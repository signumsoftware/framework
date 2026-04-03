using System.Collections;
using Signum.Engine.Linq;
using Signum.Engine.Sync;
using Signum.Utilities.Reflection;

namespace Signum.Engine.Maps;

public class EntityEvents<T> : IEntityEvents
        where T : Entity
{
    /// <summary>
    /// Before validation
    /// </summary>
    public event PreSavingEventHandler<T>? PreSaving;

    /// <summary>
    /// After validation, before DB Save
    /// </summary>
    public event SavingEventHandler<T>? Saving;

    /// <summary>
    /// After DB Save and clean modifications, but inside transaction
    /// </summary>
    public event SavedEventHandler<T>? Saved;

    public event AlternativeRetrieveEventHandler<T>? AlternativeRetrieve;
    public event RetrievedEventHandler<T>? Retrieved;

    public CacheControllerBase<T>? CacheController { get; set; }

    public event FilterQueryEventHandler<T>? FilterQuery;

    public event PreUnsafeDeleteHandler<T>? PreUnsafeDelete;
    public event PreUnsafeMListDeleteHandler<T>? PreUnsafeMListDelete;

    public event Func<T, SqlPreCommand?> PreDeleteSqlSync;

    public event PreUnsafeUpdateHandler<T>? PreUnsafeUpdate;

    public event PreUnsafeInsertHandler<T>? PreUnsafeInsert;
    public event BulkInsetHandler<T>? PreBulkInsert;

    public Dictionary<PropertyRoute, IAdditionalBinding>? AdditionalBindings { get; private set; }

    /// <param name="valueFunction">For Caching scenarios</param>
    public void RegisterBinding<M>(PropertyRoute pr, Func<bool> shouldSet, Func<Expression<Func<T, PrimaryKey? /*rowId*/, M>>> valueExpression, Func<T, PrimaryKey? /*rowId*/, IRetriever, M>? valueFunction = null)
    {
        if (AdditionalBindings == null)
            AdditionalBindings = new Dictionary<PropertyRoute, IAdditionalBinding>();

        AdditionalBindings.Add(pr, new AdditionalBinding<T, M>(pr, shouldSet, valueExpression, valueFunction));
    }

    /// <param name="valueFunction">For Caching scenarios</param>
    public void RegisterBinding<M>(Expression<Func<T, M>> field, Func<bool> shouldSet, Func<Expression<Func<T, PrimaryKey? /*rowId*/, M>>> valueExpression, Func<T, PrimaryKey? /*rowId*/, IRetriever, M>? valueFunction = null)
    {
        var ma = (MemberExpression)field.Body;

        var pr = PropertyRoute.Construct(field);

        RegisterBinding(pr, shouldSet, valueExpression, valueFunction);
    }

    internal IEnumerable<FilterQueryResult<T>> OnFilterQuery(FilterQueryArgs args)
    {
        if (FilterQuery == null)
            return Enumerable.Empty<FilterQueryResult<T>>();

        return FilterQuery.GetInvocationListTyped().Select(f => f(args)).NotNull().ToList();
    }

    public IDisposable? OnPreUnsafeDelete(IQueryable entityQuery) => this.OnPreUnsafeDelete((IQueryable<T/*Entity*/>)entityQuery);
    internal IDisposable? OnPreUnsafeDelete(IQueryable<T> entityQuery)
    {
        IDisposable? result = null;
        if (PreUnsafeDelete != null)
            foreach (var action in PreUnsafeDelete.GetInvocationListTyped().Reverse())
                result = Disposable.Combine(result, action(entityQuery));

        return result;
    }

    internal IDisposable? OnPreUnsafeMListDelete(IQueryable mlistQuery, IQueryable<T> entityQuery)
    {
        IDisposable? result = null;
        if (PreUnsafeMListDelete != null)
            foreach (var action in PreUnsafeMListDelete.GetInvocationListTyped().Reverse())
                result = Disposable.Combine(result, action(mlistQuery, entityQuery));

        return result;
    }


    internal SqlPreCommand? OnPreDeleteSqlSync(T entity)
    {
        if (PreDeleteSqlSync == null)
            return null;

        return PreDeleteSqlSync.GetInvocationListTyped().Reverse().Select(a => a(entity)).Combine(Spacing.Simple);
    }

    IDisposable? IEntityEvents.OnPreUnsafeUpdate(IUpdateable update)
    {
        IDisposable? result = null;
        if (PreUnsafeUpdate != null)
        {
            var query = update.EntityQuery<T>();
            foreach (var action in PreUnsafeUpdate.GetInvocationListTyped().Reverse())
                result = Disposable.Combine(result, action(update, query));
        }

        return result;
    }

    LambdaExpression IEntityEvents.OnPreUnsafeInsert(IQueryable query, LambdaExpression constructor, IQueryable entityQuery)
    {
        if (PreUnsafeInsert != null)
            foreach (var action in PreUnsafeInsert.GetInvocationListTyped().Reverse())
                constructor = action(query, constructor, (IQueryable<T>)entityQuery);

        return constructor;
    }

    void IEntityEvents.OnPreBulkInsert(bool inMListTable)
    {
        if (PreBulkInsert != null)
            foreach (var action in PreBulkInsert.GetInvocationListTyped().Reverse())
                action(inMListTable);
    }

    void IEntityEvents.OnPreSaving(Entity entity, PreSavingContext ctx)
    {
        PreSaving?.Invoke((T)entity, ctx);
    }

    void IEntityEvents.OnSaving(Entity entity)
    {
        Saving?.Invoke((T)entity);

    }

    void IEntityEvents.OnSaved(Entity entity, SavedEventArgs args)
    {
        Saved?.Invoke((T)entity, args);

    }

    void IEntityEvents.OnRetrieved(Entity entity, PostRetrievingContext ctx)
    {
        Retrieved?.Invoke((T)entity, ctx);
    }

    public Entity? OnAlternativeRetrieving(PrimaryKey id)
    {
        if (AlternativeRetrieve == null)
            return null;

        var args = new AlternativeRetrieveArgs<T>();

        AlternativeRetrieve(id, args);

        if (args.SkipAlternativeRetrieve)
            return null;

        if (args.Entity == null)
            throw new EntityNotFoundException(typeof(T), id);

        if (!args.AvoidAccesVerify)
        {
            var verifyAcces = Database.Query<T>().Where(a => a.Id == id).Any();
            if (!verifyAcces)
                throw new EntityNotFoundException(typeof(T), id);
        }

        return (Entity)args.Entity;
    }


    ICacheController? IEntityEvents.CacheController
    {
        get { return CacheController; }
    }
}

public interface IAdditionalBinding
{
    PropertyRoute PropertyRoute { get; }
    Func<bool> ShouldSet { get; }
    LambdaExpression GetValueExpression();
    void SetInMemory(Entity entity, IRetriever retriever);
}

public class AdditionalBinding<T, V> : IAdditionalBinding
    where T : Entity
{
    public PropertyRoute PropertyRoute { get; set; }
    public Func<bool> ShouldSet { get; set; }
    public Func<Expression<Func<T, PrimaryKey? /*rowId*/, V>>> ValueExpression { get; set; }
    public Func<T, PrimaryKey? /*rowId*/, IRetriever, V>? ValueFunction { get; set; }
    LambdaExpression IAdditionalBinding.GetValueExpression() => ValueExpression();

    Action<T, IRetriever>? _setter;

    public AdditionalBinding(PropertyRoute propertyRoute, Func<bool> shouldSet, 
        Func<Expression<Func<T, PrimaryKey? /*rowId*/, V>>> valueExpression, 
        Func<T, PrimaryKey? /*rowId*/, IRetriever, V>? valueFunction)
    {
        PropertyRoute = propertyRoute;
        ShouldSet = shouldSet;
        ValueExpression = valueExpression;
        ValueFunction = valueFunction;
    }

    public void SetInMemory(Entity entity, IRetriever retriever) => SetInMemory((T)entity, retriever);
    void SetInMemory(T entity, IRetriever retriever)
    {
        if (!ShouldSet())
            return;

        if (ValueFunction == null)
            throw new InvalidOperationException($"ValueFunction should be set in AdditionalBinding {PropertyRoute} because {PropertyRoute.Type} is Cached");

        var setValue = _setter ??= CreateSetter();

        setValue(entity, retriever);
    }

    Action<T, IRetriever> CreateSetter()
    {

        if (PropertyRoute.Type.IsMList())
        {
            var partGetter = PropertyRoute.GetLambdaExpression<T, V>(true).Compile();

            return (e, retriever) =>
            {
                var mlist = partGetter(e);

                if (mlist == null)
                    return;

                var value = ValueFunction!(e, null, retriever);

                ((IMListPrivate)mlist).AssignAndPostRetrieving((IMListPrivate)value!, null!);

                retriever.ModifiablePostRetrieving((Modifiable)(object)mlist);
            };
        }
        else if (PropertyRoute.Parent!.PropertyRouteType == PropertyRouteType.Root)
        {
            var setter = ReflectionTools.CreateSetter<T, V>(PropertyRoute.PropertyInfo!);

            return (e, retriever) => 
            {
                var value = ValueFunction!(e, null, retriever);
                setter!(e, value);
            };
        }
        else
        {
            var mlistRoute = PropertyRoute.GetMListItemsRoute();
            if (mlistRoute != null)
            {
                var mlistGetter = mlistRoute.Parent!.GetLambdaExpression<T, IMListPrivate>(true).Compile();
                var partGetter = PropertyRoute.Parent!.GetLambdaExpression<ModifiableEntity, ModifiableEntity>(true, mlistRoute).Compile();

                var setter = ReflectionTools.CreateSetter<ModifiableEntity, V>(PropertyRoute.FieldInfo!);

                return (e, retriever) =>
                {
                    var mlist = mlistGetter(e);
                    if (mlist == null)
                        return;

                    var list = (IList)mlist;
                    for (int i = 0; i < list.Count; i++)
                    {
                        var rowId = mlist.GetRowId(i);
                        var part = partGetter((ModifiableEntity)list[i]!);
                        var value = ValueFunction!(e, rowId, retriever);
                        setter!(part, value);
                    }
                };
            }
            else
            {
                var partGetter = PropertyRoute.Parent!.GetLambdaExpression<T, ModifiableEntity>(true).Compile();

                var setter = ReflectionTools.CreateSetter<ModifiableEntity, V>(PropertyRoute.FieldInfo!);

                return (e, retriever) =>
                {
                    var part = partGetter(e);
                    if (part == null)
                        return;

                    var value = ValueFunction!(e, null, retriever);
                    setter!(part, value);
                };
            }
        }
    }
}

public delegate void PreSavingEventHandler<T>(T ident, PreSavingContext ctx) where T : Entity;
public delegate void RetrievedEventHandler<T>(T ident, PostRetrievingContext ctx) where T : Entity;
public delegate void SavingEventHandler<T>(T ident) where T : Entity;
public delegate void SavedEventHandler<T>(T ident, SavedEventArgs args) where T : Entity;
public delegate FilterQueryResult<T>? FilterQueryEventHandler<T>(FilterQueryArgs args) where T : Entity;
public delegate void AlternativeRetrieveEventHandler<T>(PrimaryKey id, AlternativeRetrieveArgs<T> args) where T : Entity;

public delegate IDisposable? PreUnsafeDeleteHandler<T>(IQueryable<T> entityQuery);
public delegate IDisposable? PreUnsafeMListDeleteHandler<T>(IQueryable mlistQuery, IQueryable<T> entityQuery);
public delegate IDisposable? PreUnsafeUpdateHandler<T>(IUpdateable update, IQueryable<T> entityQuery);
public delegate LambdaExpression PreUnsafeInsertHandler<T>(IQueryable query, LambdaExpression constructor, IQueryable<T> entityQuery);
public delegate void BulkInsetHandler<T>(bool inMListTable);


public class FilterQueryArgs
{
    internal FilterQueryArgs(Expression fullQuery, ConstantExpression baseQuery)
    {
        FullQuery = fullQuery;
        BaseQuery = baseQuery;
    }

    /// Database.Query<OperationLogEntity>().Where(ol => ol.Target == myInvoice)
    /// |<--------------------------- FullQuery------------------------------->|
    /// |<----------- BaseQuery----------->|
    //                                     ^ <- Here will be inserted the new where, if any

    public Expression FullQuery { get; set; }

    //The 
    public ConstantExpression BaseQuery { get; set; }

    public static FilterQueryArgs FromLite(Lite<IEntity> lite)
    {
        return giFromLitePrivate.GetInvoker(lite.EntityType)((Lite<Entity>)lite);
    }

    static GenericInvoker<Func<Lite<Entity>, FilterQueryArgs>> giFromLitePrivate = new(lite => FromLitePrivate<Entity>(lite));
    static FilterQueryArgs FromLitePrivate<T>(Lite<T> lite) where T : Entity
    {
        return FromFilter<T>(e => e.Is(lite));
    }

    public static FilterQueryArgs FromEntity(Entity entity)
    {
        return giFromEntityPrivate.GetInvoker(entity.GetType())(entity);
    }

    static GenericInvoker<Func<Entity, FilterQueryArgs>> giFromEntityPrivate = new(entity => FromEntityPrivate((Entity)entity));
    static FilterQueryArgs FromEntityPrivate<T>(T entity) where T : Entity
    {
        return FromFilter<T>(e => e.Is(entity));
    }

    static GenericInvoker<Func<LambdaExpression, FilterQueryArgs>> giFromFilter = 
        new (lambda => FromFilter((Expression<Func<Entity, bool>>)lambda));
    public static FilterQueryArgs FromFilter<T>(Expression<Func<T, bool>> filter) where T: Entity
    {
        var query = filter == null ? Database.Query<T>() : Database.Query<T>().Where(filter);
        return FilterQueryArgs.FromQuery(query);
    }   

    public static FilterQueryArgs FromQuery<T>(IQueryable<T> similarQuery) where T : Entity
    {
        var fullQuery = DbQueryProvider.Clean(similarQuery.Expression, false, null)!;
        var baseQuey = FindBaseQueryVisitor.FindBase(fullQuery!);

        return new FilterQueryArgs(fullQuery, baseQuey);
    }

    class FindBaseQueryVisitor : ExpressionVisitor
    {
        ConstantExpression? result;

        public static ConstantExpression FindBase(Expression query)
        {
            var finder = new FindBaseQueryVisitor();
            finder.Visit(query);

            if (finder.result == null)
                throw new InvalidOperationException("No base query found");

            return finder.result;
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (node.Value is IQueryable q && q.IsBase())
            {
                if (result != null)
                    throw new InvalidOperationException("More than one result base query found");

                this.result = (ConstantExpression)q.Expression;
            }

            return base.VisitConstant(node);
        }
    }
}



public class AlternativeRetrieveArgs<T> where T : Entity
{
    public bool AvoidAccesVerify { get; set; }
    public bool SkipAlternativeRetrieve { get; set; }
    public T? Entity { get; set; }
}

public class SavedEventArgs
{
    public bool IsRoot { get; set; }
    public bool WasNew { get; set; }
    public bool WasModified { get; set; }
}

public interface IFilterQueryResult
{
    LambdaExpression InDatabaseExpression { get; }
}

public class FilterQueryResult<T> : IFilterQueryResult where T : Entity
{
    public FilterQueryResult(Expression<Func<T, bool>> inDatabaseExpression, Func<T, bool>? inMemoryFunction)
    {
        this.InDatabaseExpresson = inDatabaseExpression;
        this.InMemoryFunction = inMemoryFunction;
    }

    public readonly Expression<Func<T, bool>> InDatabaseExpresson;
    public readonly Func<T, bool>? InMemoryFunction;

    LambdaExpression IFilterQueryResult.InDatabaseExpression { get { return this.InDatabaseExpresson; } }
}

internal interface IEntityEvents
{
    Entity? OnAlternativeRetrieving(PrimaryKey id);
    void OnPreSaving(Entity entity, PreSavingContext ctx);
    void OnSaving(Entity entity);
    void OnSaved(Entity entity, SavedEventArgs args);

    void OnRetrieved(Entity entity, PostRetrievingContext ctx);

    IDisposable? OnPreUnsafeDelete(IQueryable entityQuery);
    IDisposable? OnPreUnsafeUpdate(IUpdateable update);
    LambdaExpression OnPreUnsafeInsert(IQueryable query, LambdaExpression constructor, IQueryable entityQuery);
    void OnPreBulkInsert(bool inMListTable);

    ICacheController? CacheController { get; }

    Dictionary<PropertyRoute, IAdditionalBinding>? AdditionalBindings { get; }
}
