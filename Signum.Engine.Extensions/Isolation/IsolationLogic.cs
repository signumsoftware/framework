using Signum.Entities.Basics;
using Signum.Entities.Isolation;
using Signum.Utilities.Reflection;
using Signum.Engine.Processes;
using Signum.Entities.Processes;
using Signum.Entities.Authorization;

namespace Signum.Engine.Isolation;

public static class IsolationLogic
{
    public static bool IsStarted;

    public static ResetLazy<List<Lite<IsolationEntity>>> Isolations = null!;

    internal static Dictionary<Type, IsolationStrategy> strategies = new Dictionary<Type, IsolationStrategy>();

    public static void Start(SchemaBuilder sb)
    {
        if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
        {
            sb.Include<IsolationEntity>()
                .WithSave(IsolationOperation.Save)
                .WithQuery(() => iso => new
                {
                    Entity = iso,
                    iso.Id,
                    iso.Name
                });


            UserWithClaims.FillClaims += (userWithClaims, user) =>
            {
                userWithClaims.Claims["Isolation"] = ((UserEntity)user).TryIsolation();
            };

            Schema.Current.AttachToUniqueFilter += entity =>
            {
                var type = entity.GetType();
                var hasIsolationMixin = MixinDeclarations.IsDeclared(type, typeof(IsolationMixin));

                return hasIsolationMixin == false ? null :
                    e => e.Mixin<IsolationMixin>().Isolation.Is(entity.Mixin<IsolationMixin>().Isolation);
            };

            sb.Schema.EntityEventsGlobal.PreSaving += EntityEventsGlobal_PreSaving;
            sb.Schema.SchemaCompleted += AssertIsolationStrategies;
            OperationLogic.SurroundOperation += OperationLogic_SurroundOperation;

            Isolations = sb.GlobalLazy(() => Database.RetrieveAllLite<IsolationEntity>(),
                new InvalidateWith(typeof(IsolationEntity)));

            ProcessLogic.ApplySession += ProcessLogic_ApplySession;

            Validator.OverridePropertyValidator((IsolationMixin m) => m.Isolation).StaticPropertyValidation += (mi, pi) =>
            {
                if (strategies.GetOrThrow(mi.MainEntity.GetType()) == IsolationStrategy.Isolated && mi.Isolation == null)
                    return ValidationMessage._0IsNotSet.NiceToString(pi.NiceName());

                return null;
            };
            IsStarted = true;
        }
    }


    static IDisposable? ProcessLogic_ApplySession(ProcessEntity process)
    {
        return IsolationEntity.Override(process.Data!.TryIsolation());
    }

    static IDisposable? OperationLogic_SurroundOperation(IOperation operation, OperationLogEntity log, Entity? entity, object?[]? args)
    {
        return IsolationEntity.Override(entity?.TryIsolation() ?? args.TryGetArgC<Lite<IsolationEntity>>());
    }

    static void EntityEventsGlobal_PreSaving(Entity ident, PreSavingContext ctx)
    {
        if (strategies.TryGet(ident.GetType(), IsolationStrategy.None) != IsolationStrategy.None && IsolationEntity.Current != null)
        {
            if (ident.Mixin<IsolationMixin>().Isolation == null)
            {
                if (ident.IsNew)
                {
                    ident.Mixin<IsolationMixin>().Isolation = IsolationEntity.Current;
                    ctx.InvalidateGraph();
                }
                else
                {
                    if (ident.IsGraphModified)
                        throw new ApplicationException(IsolationMessage.Entity0HasIsolation1ButCurrentIsolationIs2.NiceToString(ident, "null", IsolationEntity.Current));
                }
            }
            else if (!ident.Mixin<IsolationMixin>().Isolation.Is(IsolationEntity.Current))
                throw new ApplicationException(IsolationMessage.Entity0HasIsolation1ButCurrentIsolationIs2.NiceToString(ident, ident.Mixin<IsolationMixin>().Isolation, IsolationEntity.Current));
        }
    }

    static void AssertIsolationStrategies()
    {
        var result = EnumerableExtensions.JoinStrict(
            strategies.Keys,
            Schema.Current.Tables.Keys.Where(a => !a.IsEnumEntityOrSymbol() && !typeof(SemiSymbol).IsAssignableFrom(a) && a != typeof(IsolationEntity)),
            a => a,
            a => a,
            (a, b) => 0);

        var extra = result.Extra.OrderBy(a => a.Namespace).ThenBy(a => a.Name).ToString(t => "  IsolationLogic.Register<{0}>(IsolationStrategy.XXX);".FormatWith(t.Name), "\r\n");

        var lacking = result.Missing.GroupBy(a => a.Namespace).OrderBy(gr => gr.Key).ToString(gr => "  //{0}\r\n".FormatWith(gr.Key) +
            gr.ToString(t => "  IsolationLogic.Register<{0}>(IsolationStrategy.XXX);".FormatWith(t.Name), "\r\n"), "\r\n\r\n");

        if (extra.HasText() || lacking.HasText())
            throw new InvalidOperationException("IsolationLogic's strategies are not synchronized with the Schema.\r\n" +
                    (extra.HasText() ? ("Remove something like:\r\n" + extra + "\r\n\r\n") : null) +
                    (lacking.HasText() ? ("Add something like:\r\n" + lacking + "\r\n\r\n") : null));

        foreach (var item in strategies.Where(kvp => kvp.Value == IsolationStrategy.Isolated || kvp.Value == IsolationStrategy.Optional))
        {
            giRegisterFilterQuery.GetInvoker(item.Key)(item.Value);
        }

        Schema.Current.EntityEvents<IsolationEntity>().FilterQuery += () =>
        {
            if (IsolationEntity.Current == null || ExecutionMode.InGlobal)
                return null;

            return new FilterQueryResult<IsolationEntity>(
                a => a.ToLite().Is(IsolationEntity.Current),
                a => a.ToLite().Is(IsolationEntity.Current));
        };
    }

    public static IsolationStrategy GetStrategy(Type type)
    {
        return strategies[type];
    }

    static readonly GenericInvoker<Action<IsolationStrategy>> giRegisterFilterQuery = new(strategy => Register_FilterQuery<Entity>(strategy));
    static void Register_FilterQuery<T>(IsolationStrategy stragegy) where T : Entity
    {
        Schema.Current.EntityEvents<T>().FilterQuery += () =>
        {
            if (ExecutionMode.InGlobal || IsolationEntity.Current == null)
                return null;

            if (stragegy == IsolationStrategy.Isolated)
                return new FilterQueryResult<T>(
                    a => a.Mixin<IsolationMixin>().Isolation.Is(IsolationEntity.Current),
                    a => a.Mixin<IsolationMixin>().Isolation.Is(IsolationEntity.Current));
            
            if(stragegy == IsolationStrategy.Optional)
                return new FilterQueryResult<T>(
                    a => a.Mixin<IsolationMixin>().Isolation.Is(IsolationEntity.Current) || a.Mixin<IsolationMixin>().Isolation == null,
                    a => a.Mixin<IsolationMixin>().Isolation.Is(IsolationEntity.Current) || a.Mixin<IsolationMixin>().Isolation == null);

            throw new UnexpectedValueException(stragegy);
        };


        Schema.Current.EntityEvents<T>().PreUnsafeInsert += (IQueryable query, LambdaExpression constructor, IQueryable<T> entityQuery) =>
        {
            if (ExecutionMode.InGlobal || IsolationEntity.Current == null)
                return constructor;

            if (constructor.Body.Type == typeof(T))
            {
                var newBody = Expression.Call(
                  miSetMixin.MakeGenericMethod(typeof(T), typeof(IsolationMixin), typeof(Lite<IsolationEntity>)),
                  constructor.Body,
                  Expression.Quote(IsolationLambda),
                  Expression.Constant(IsolationEntity.Current));

                return Expression.Lambda(newBody, constructor.Parameters);
            }

            return constructor; //MListTable
        };
    }

    static MethodInfo miSetMixin = ReflectionTools.GetMethodInfo((Entity a) => a.SetMixin((IsolationMixin m) => m.Isolation, null)).GetGenericMethodDefinition();
    static Expression<Func<IsolationMixin, Lite<IsolationEntity>?>> IsolationLambda = (IsolationMixin m) => m.Isolation;

    public static void Register<T>(IsolationStrategy strategy) where T : Entity
    {
        strategies.Add(typeof(T), strategy);

        if (strategy == IsolationStrategy.Isolated || strategy == IsolationStrategy.Optional)
            MixinDeclarations.Register(typeof(T), typeof(IsolationMixin));

        if (strategy == IsolationStrategy.Optional)
        {
            Schema.Current.Settings.FieldAttributes((T e) => e.Mixin<IsolationMixin>().Isolation).Remove<ForceNotNullableAttribute>(); //Remove non-null 
        }
    }
    
    public static IEnumerable<T> WhereCurrentIsolationInMemory<T>(this IEnumerable<T> collection) where T : Entity
    {
        var curr = IsolationEntity.Current;

        if (curr == null || strategies[typeof(T)] == IsolationStrategy.None)
            return collection;

        return collection.Where(a => a.Isolation().Is(curr));
    }

    public static Lite<IsolationEntity>? GetOnlyIsolation(List<Lite<Entity>> selectedEntities)
    {
        return selectedEntities.GroupBy(a => a.EntityType)
            .Select(gr => strategies[gr.Key] == IsolationStrategy.None ? null : giGetOnlyIsolation.GetInvoker(gr.Key)(gr))
            .NotNull()
            .Only();
    }


    static GenericInvoker<Func<IEnumerable<Lite<Entity>>, Lite<IsolationEntity>?>> giGetOnlyIsolation =
        new(list => GetOnlyIsolation<Entity>(list));


    public static Lite<IsolationEntity>? GetOnlyIsolation<T>(IEnumerable<Lite<Entity>> selectedEntities) where T : Entity
    {
        return selectedEntities.Cast<Lite<T>>().Chunk(100).Select(gr =>
            Database.Query<T>().Where(e => gr.Contains(e.ToLite())).Select(e => e.Isolation()).Only()
            ).NotNull().Only();
    }

    public static Dictionary<Type, IsolationStrategy> GetIsolationStrategies()
    {
        return strategies.ToDictionary();
    }

}
