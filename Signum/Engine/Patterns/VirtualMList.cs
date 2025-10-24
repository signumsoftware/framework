using Signum.Engine.Maps;
using Signum.Utilities.DataStructures;
using Signum.Utilities.Reflection;
using System.Collections.Immutable;

namespace Signum.Engine;

public static class VirtualMList
{
    //Order, OrderLine, Order.Lines
    public static Dictionary<Type, Dictionary<PropertyRoute, VirtualMListInfo>> RegisteredVirtualMLists = new Dictionary<Type, Dictionary<PropertyRoute, VirtualMListInfo>>();

    static readonly Variable<ImmutableStack<Type>> avoidTypes = Statics.ThreadVariable<ImmutableStack<Type>>("avoidVirtualMList");

    public static bool ShouldAvoidMListType(Type elementType)
    {
        var stack = avoidTypes.Value;
        return (stack != null && (stack.Contains(elementType) || stack.Contains(typeof(object))));
    }

    public static bool IsVirtualMList(this PropertyRoute pr)
    {
        return pr.Type.IsMList() && (RegisteredVirtualMLists.TryGetC(pr.RootType)?.ContainsKey(pr) ?? false);
    }

    /// <param name="elementType">Use null for every type</param>
    public static IDisposable AvoidMListType(Type elementType)
    {
        avoidTypes.Value = (avoidTypes.Value ?? ImmutableStack<Type>.Empty).Push(elementType);

        return new Disposable(() => avoidTypes.Value = avoidTypes.Value.Pop());
    }

    static readonly Variable<ImmutableStack<Type>> considerNewTypes = Statics.ThreadVariable<ImmutableStack<Type>>("considerNewTypes");

    public static bool ShouldConsiderNew(Type parentType)
    {
        var stack = considerNewTypes.Value;
        return (stack != null && (stack.Contains(parentType) || stack.Contains(typeof(object))));
    }

    /// <param name="parentType">Use null for every type</param>
    public static IDisposable ConsiderNewType(Type parentType)
    {
        considerNewTypes.Value = (considerNewTypes.Value ?? ImmutableStack<Type>.Empty).Push(parentType);

        return new Disposable(() => considerNewTypes.Value = considerNewTypes.Value.Pop());
    }

    public static FluentInclude<T> WithVirtualMList<T, L>(this FluentInclude<T> fi,
        Expression<Func<T, MList<L>>> mListField,
        Expression<Func<L, Lite<T>?>> backReference,
        ExecuteSymbol<L> saveOperation,
        DeleteSymbol<L> deleteOperation)
        where T : Entity
        where L : Entity
    {

        return fi.WithVirtualMList(mListField, backReference,
            onSave: saveOperation == null ? null : new Action<L, T>((line, e) =>
            {
                line.Execute(saveOperation);
            }),
            onRemove: deleteOperation == null ? null : new Action<L, T>((line, e) =>
            {
                line.Delete(deleteOperation);
            }));
    }

    public static FluentInclude<T> WithVirtualMList<T, L>(this FluentInclude<T> fi,
        Expression<Func<T, MList<L>>> mlistProperty,
        Expression<Func<L, Lite<T>?>> backReference,
        Action<L, T>? onSave = null,
        Action<L, T>? onRemove = null,
        bool? lazyRetrieve = null,
        bool? lazyDelete = null,  //To avoid StackOverflows
        bool forceSeek = false)
        where T : Entity
        where L : Entity
    {
        fi.SchemaBuilder.Include<L>();

        var mListRoute = PropertyRoute.Construct(mlistProperty);
        var backReferenceRoute = PropertyRoute.Construct(backReference, avoidLastCasting: true);

        if (fi.SchemaBuilder.Settings.FieldAttribute<IgnoreAttribute>(mListRoute) == null ||
            mListRoute.PropertyInfo!.GetCustomAttribute<QueryablePropertyAttribute>() == null)
            throw new InvalidOperationException($"The property {mListRoute} should have the attributes [Ignore, QueryableProperty] to be used as VirtualMList");

        var nn = Validator.TryGetPropertyValidator(backReferenceRoute)?.Validators.OfType<NotNullValidatorAttribute>().SingleOrDefaultEx();
        if (nn != null && !nn.Disabled)
            throw new InvalidOperationException($"The property {backReferenceRoute} should have an [NotNullValidator(Disabled = true)] to be used as back reference in a VirtualMList");

        Func<T, MList<L>> getMList = GetAccessor(mlistProperty);

        RegisteredVirtualMLists.GetOrCreate(typeof(T)).Add(mListRoute, new VirtualMListInfo
        {
            MListRoute = mListRoute,
            MListExpression = mlistProperty,
            BackReferenceExpression = backReference,
            BackReferenceRoute = backReferenceRoute,
            GetMList = e => getMList((T)e)
        });

        var defLazyRetrieve = lazyRetrieve ?? (typeof(L) == typeof(T));
        var defLazyDelete = lazyDelete ?? (typeof(L) == typeof(T));

        Action<L, Lite<T>>? setter = null;
        bool preserveOrder = fi.SchemaBuilder.Settings.FieldAttributes(mListRoute)!
            .OfType<PreserveOrderAttribute>()
            .Any();

        if (preserveOrder && !typeof(ICanBeOrdered).IsAssignableFrom(typeof(L)))
            throw new InvalidOperationException($"'{typeof(L).Name}' should implement '{nameof(ICanBeOrdered)}' because '{ReflectionTools.GetPropertyInfo(mlistProperty).Name}' contains '[{nameof(PreserveOrderAttribute)}]'");

        var sb = fi.SchemaBuilder;

        if (defLazyRetrieve)
        {
            sb.Schema.EntityEvents<T>().Retrieved += (T e, PostRetrievingContext ctx) =>
            {
                if (ShouldAvoidMListType(typeof(L)))
                    return;

                var mlist = getMList(e);

                if (mlist == null)
                    return;

                var query = Database.Query<L>()
                    .Where(line => backReference.Evaluate(line).Is(e.ToLite()));

                MList<L> newList = preserveOrder ?
                    query.ToVirtualMListWithOrder() :
                    query.ToVirtualMList();

                ((IMListPrivate<L>)mlist).AssignAndPostRetrieving(newList, ctx);
            };
        }

        var baseQuery = Connector.Current is SqlServerConnector && forceSeek ?
            Database.Query<L>().WithHint("FORCESEEK").DisableQueryFilter() :
            Database.Query<L>().DisableQueryFilter();

        if (preserveOrder)
        {
            sb.Schema.EntityEvents<T>().RegisterBinding<MList<L>>(mlistProperty,
                 shouldSet: () => !defLazyRetrieve && !VirtualMList.ShouldAvoidMListType(typeof(L)),
                 valueExpression: () => (e, rowId) => baseQuery.Where(line => backReference.Evaluate(line).Is(e.ToLite())).ExpandLite(line => backReference.Evaluate(line), ExpandLite.ModelLazy).ToVirtualMListWithOrder(),
                 valueFunction: (e, rowId, retriever) => Schema.Current.CacheController<L>()!.Enabled ?
                 Schema.Current.CacheController<L>()!.RequestByBackReference<T>(retriever, backReference, e.ToLite()).ToVirtualMListWithOrder():
                 Database.Query<L>().DisableQueryFilter().Where(line => backReference.Evaluate(line).Is(e.ToLite())).ExpandLite(line => backReference.Evaluate(line), ExpandLite.ModelLazy).ToVirtualMListWithOrder()

            );
        }
        else
        {
            sb.Schema.EntityEvents<T>().RegisterBinding(mlistProperty,
                shouldSet: () => !defLazyRetrieve && !VirtualMList.ShouldAvoidMListType(typeof(L)),
                valueExpression: () => (e, rowId) => baseQuery.Where(line => backReference.Evaluate(line).Is(e.ToLite())).ExpandLite(line => backReference.Evaluate(line), ExpandLite.ModelLazy).ToVirtualMList(),
                valueFunction: (e, rowId, retriever) => Schema.Current.CacheController<L>()!.Enabled ?
                Schema.Current.CacheController<L>()!.RequestByBackReference<T>(retriever, backReference, e.ToLite()).ToVirtualMList() :
                Database.Query<L>().DisableQueryFilter().Where(line => backReference.Evaluate(line).Is(e.ToLite())).ExpandLite(line => backReference.Evaluate(line), ExpandLite.ModelLazy).ToVirtualMList()
            );
        }

        sb.Schema.EntityEvents<T>().PreSaving += (T e, PreSavingContext ctx) =>
        {
            if (VirtualMList.ShouldAvoidMListType(typeof(L)))
                return;

            var mlist = getMList(e);
            if (mlist == null)
                return;

            if (mlist.Count > 0)
            {
                var graph = Saver.PreSaving(() => GraphExplorer.FromRoot(mlist).RemoveAllNodes(ctx.Graph));
                GraphExplorer.PropagateModifications(graph.Inverse());
                var errors = GraphExplorer.FullIntegrityCheck(graph);
                if (errors != null)
                {
#if DEBUG
                    var withEntites = errors.WithEntities(graph);
                    throw new IntegrityCheckException(withEntites);
#else
                    throw new IntegrityCheckException(errors);
#endif
                }
            }

            if (mlist.IsGraphModified)
                e.SetSelfModified();
        };

        sb.Schema.EntityEvents<T>().Saving += (T e) =>
        {
            if (VirtualMList.ShouldAvoidMListType(typeof(L)))
                return;

            var mlist = getMList(e);
            if (mlist == null)
                return;

            if (preserveOrder)
            {
                mlist.ForEach((o,i) => ((ICanBeOrdered) o).Order = i);
            }

            if (GraphExplorer.IsGraphModified(mlist))
                e.SetModified();
        };
        sb.Schema.EntityEvents<T>().Saved += (T e, SavedEventArgs args) =>
        {
            if (VirtualMList.ShouldAvoidMListType(typeof(L)))
                return;

            var mlist = getMList(e);

            if (mlist != null && !GraphExplorer.IsGraphModified(mlist))
                return;

            if (!(args.WasNew || ShouldConsiderNew(typeof(T))))
            {
                var oldElements = mlist.EmptyIfNull().Where(line => !line.IsNew);
                var query = Database.Query<L>().DisableQueryFilter()
                .Where(p => backReference.Evaluate(p).Is(e.ToLite()));

                if(onRemove == null)
                    query.Where(p => !oldElements.Contains(p)).UnsafeDelete();
                else
                    query.Where(p => !oldElements.Contains(p)).ToList().ForEach(line => onRemove!(line, e));
            }

            if (mlist != null)
            {
                if (mlist.Any())
                {
                    if (setter == null)
                        setter = CreateBackreferenceSetter(backReference);

                    mlist.ForEach(line => setter!(line, e.ToLite()));
                    if (onSave == null)
                        mlist.SaveList();
                    else
                        mlist.ForEach(line => { if (GraphExplorer.IsGraphModified(line)) onSave!(line, e); });

                    var priv = (IMListPrivate)mlist;
                    for (int i = 0; i < mlist.Count; i++)
                    {
                        if (priv.GetRowId(i) == null)
                            priv.SetRowId(i, mlist[i].Id);
                    }
                }
                mlist.SetCleanModified(false);
            }
        };


        sb.Schema.EntityEvents<T>().PreUnsafeDelete += query =>
        {
            if (VirtualMList.ShouldAvoidMListType(typeof(L)))
                return null;

            //You can do a VirtualMList to itself at the table level, but there should not be cycles inside the instances
            var toDelete = Database.Query<L>().DisableQueryFilter().Where(se => query.Any(e => backReference.Evaluate(se).Is(e)));
            if (defLazyDelete)
            {
                if (toDelete.Any())
                    toDelete.UnsafeDelete();
            }
            else
            {
                toDelete.UnsafeDelete();
            }
            return null;
        };

        return fi;
    }

    public static FluentInclude<T> WithVirtualMListInitializeOnly<T, L>(this FluentInclude<T> fi,
        Expression<Func<T, MList<L>>> mListField,
        Expression<Func<L, Lite<T>?>> backReference,
        Action<L, T>? onSave = null)
        where T : Entity
        where L : Entity
    {
        fi.SchemaBuilder.Include<L>();

        Func<T, MList<L>> getMList = GetAccessor(mListField);
        Action<L, Lite<T>>? setter = null;
        var sb = fi.SchemaBuilder;

        sb.Schema.EntityEvents<T>().RegisterBinding(mListField,
            shouldSet: () => false,
            valueExpression: () => (e, rowId) => Database.Query<L>().DisableQueryFilter().Where(line => backReference.Evaluate(line).Is(e.ToLite())).ExpandLite(line => backReference.Evaluate(line), ExpandLite.ModelLazy).ToVirtualMListWithOrder()
            );

        sb.Schema.EntityEvents<T>().Saving += (T e) =>
        {
            if (VirtualMList.ShouldAvoidMListType(typeof(L)))
                return;

            var mlist = getMList(e);
            if (mlist == null)
                return;

            if (GraphExplorer.IsGraphModified(getMList(e)))
                e.SetModified();
        };

        sb.Schema.EntityEvents<T>().Saved += (T e, SavedEventArgs args) =>
        {
            if (VirtualMList.ShouldAvoidMListType(typeof(L)))
                return;

            var mlist = getMList(e);
            if (mlist == null)
                return;

            if (!GraphExplorer.IsGraphModified(mlist))
                return;

            if (setter == null)
                setter = CreateBackreferenceSetter(backReference);

            mlist.ForEach(line => setter!(line, e.ToLite()));
            if (onSave == null)
                mlist.SaveList();
            else
                mlist.ForEach(line =>
                {
                    if (GraphExplorer.IsGraphModified(line))
                        onSave!(line, e);
                });
            var priv = (IMListPrivate)mlist;
            for (int i = 0; i < mlist.Count; i++)
            {
                if (priv.GetRowId(i) == null)
                    priv.SetRowId(i, mlist[i].Id);
            }
            mlist.SetCleanModified(false);
        };
        return fi;
    }

    public static Func<T, MList<L>> GetAccessor<T, L>(Expression<Func<T, MList<L>>> mListField)
        where T : Entity
        where L : Entity
    {
        var body = mListField.Body;

        var param = mListField.Parameters.SingleEx();

        var newBody = SafeAccess(param, (MemberExpression)body, body);

        return Expression.Lambda<Func<T, MList<L>>>(newBody, mListField.Parameters.Single()).Compile();
    }

    private static Expression SafeAccess(ParameterExpression param, MemberExpression member, Expression acum)
    {
        if (member.Expression == param)
            return acum;

        return SafeAccess(param,
            member: (MemberExpression)member.Expression!,
            acum: Expression.Condition(Expression.Equal(member.Expression!, Expression.Constant(null, member.Expression!.Type)), Expression.Constant(null, acum.Type), acum)
            );
    }

    public static Action<L, Lite<T>>? CreateBackreferenceSetter<T, L>(Expression<Func<L, Lite<T>?>> getBackReference)
        where T : Entity
        where L : Entity
    {
        var body = getBackReference.Body;
        if (body.NodeType == ExpressionType.Convert)
            body = ((UnaryExpression)body).Operand;

        using (HeavyProfiler.LogNoStackTrace("CreateSetter"))
        {
            ParameterExpression t = getBackReference.Parameters.SingleEx();
            ParameterExpression p = Expression.Parameter(typeof(Lite<T>), "p");

            var me = ((MemberExpression)body);

            var p2 = p.TryConvert(me.Member.ReturningType());

            var exp = Expression.Lambda(typeof(Action<L, Lite<T>>), Expression.Assign(me, p2), t, p);
            return (Action<L, Lite<T>>)exp.Compile();
        }
    }

    public static MList<T> ToVirtualMListWithOrder<T>(this IEnumerable<T> elements)
        where T : Entity
    {
        return new MList<T>(elements.Select(line => new MList<T>.RowIdElement(line, line.Id, ((ICanBeOrdered)line).Order)));
    }

    public static MList<T> ToVirtualMList<T>(this IEnumerable<T> elements)
        where T : Entity
    {
        return new MList<T>(elements.Select(line => new MList<T>.RowIdElement(line, line.Id, null)));
    }

    public static MList<Lite<T>> ToVirtualMList<T>(this IEnumerable<Lite<T>> elements)
        where T : Entity
    {
        return new MList<Lite<T>>(elements.Select(line => new MList<Lite<T>>.RowIdElement(line, line.Id, null)));
    }
}

public class VirtualMListInfo
{
    public PropertyRoute MListRoute { get; init; }
    public LambdaExpression MListExpression { get; init; }
    public PropertyRoute BackReferenceRoute { get; init; }
    public LambdaExpression BackReferenceExpression { get; init; }
    public Func<Entity, IMListPrivate?> GetMList { get; init; }
}
