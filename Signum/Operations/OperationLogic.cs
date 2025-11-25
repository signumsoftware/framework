using Signum.Engine.Maps;
using Signum.Utilities.Reflection;
using System.Collections.Immutable;
using System.Collections;
using Signum.DynamicQuery.Tokens;
using Signum.Engine.Sync;
using System.Collections.Frozen;

namespace Signum.Operations;

public static class OperationLogic
{
    [AutoExpressionField]
    public static IQueryable<OperationLogEntity> OperationLogs(this Entity e) =>
        As.Expression(() => Database.Query<OperationLogEntity>().Where(a => a.Target.Is(e)));

    [AutoExpressionField]
    public static OperationLogEntity? PreviousOperationLog(this Entity e) =>
        As.Expression(() => e.OperationLogs().Where(ol => ol.Exception == null && ol.End.HasValue && e.SystemPeriod().Contains(Schema.Current.TimeZoneMode == TimeZoneMode.Local ? ol.End.Value.ToLocalTime().ToUniversalTime() : ol.End.Value)).OrderBy(a => a.End!.Value).FirstOrDefault());

    [AutoExpressionField]
    public static OperationLogEntity? LastOperationLog(this Entity e) =>
    As.Expression(() => e.OperationLogs().DisableQueryFilter().Where(ol => ol.Exception == null).OrderByDescending(a => a.End!.Value).FirstOrDefault());


    [AutoExpressionField]
    public static IQueryable<OperationLogEntity> Logs(this OperationSymbol o) =>
        As.Expression(() => Database.Query<OperationLogEntity>().Where(a => a.Operation.Is(o)));

    static Polymorphic<Dictionary<OperationSymbol, IOperation>> operations = new Polymorphic<Dictionary<OperationSymbol, IOperation>>(PolymorphicMerger.InheritDictionaryInterfaces, typeof(IEntity));

    static ResetLazy<FrozenDictionary<OperationSymbol, List<Type>>> operationsFromKey = new ResetLazy<FrozenDictionary<OperationSymbol, List<Type>>>(() =>
    {
        return (from t in operations.OverridenTypes
                from d in operations.GetDefinition(t)!.Keys
                group t by d into g
                select KeyValuePair.Create(g.Key, g.ToList())).ToFrozenDictionaryEx();
    });


    public static HashSet<OperationSymbol> RegisteredOperations
    {
        get { return operations.OverridenValues.SelectMany(a => a.Keys).ToHashSet(); }
    }

    static readonly Variable<ImmutableStack<Type>> allowedTypes = Statics.ThreadVariable<ImmutableStack<Type>>("saveOperationsAllowedTypes");

    public static bool AllowSaveGlobally { get; set; }

    public static bool IsSaveAllowedInContext(Type type)
    {
        var stack = allowedTypes.Value;
        return (stack != null && stack.Contains(type));
    }

    public static IDisposable AllowSave<T>() where T : class, IEntity
    {
        return AllowSave(typeof(T));
    }

    public static IDisposable AllowSave(Type type)
    {
        allowedTypes.Value = (allowedTypes.Value ?? ImmutableStack<Type>.Empty).Push(type);

        return new Disposable(() => allowedTypes.Value = allowedTypes.Value.Pop());
    }

    public static IDisposable AllowSave(List<Type> types)
    {
        allowedTypes.Value = (allowedTypes.Value ?? ImmutableStack<Type>.Empty).PushRange(types);

        return new Disposable(() =>
        {
            foreach (var type in types)
                allowedTypes.Value = allowedTypes.Value.Pop();
        });
    }

    public static void AssertStarted(SchemaBuilder sb)
    {
        sb.AssertDefined(ReflectionTools.GetMethodInfo(() => Start(null!)));
    }

    public static void Start(SchemaBuilder sb)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        sb.Include<OperationLogEntity>()
            .WithQuery(() => lo => new
            {
                Entity = lo,
                lo.Id,
                lo.Target,
                lo.Operation,
                lo.User,
                lo.Start,
                lo.End,
                lo.Exception
            });

        SymbolLogic<OperationSymbol>.Start(sb, () => RegisteredOperations);

        sb.Include<OperationSymbol>()
            .WithQuery(() => os => new
            {
                Entity = os,
                os.Id,
                os.Key
            });

        QueryLogic.Expressions.Register((OperationSymbol o) => o.Logs(), OperationMessage.Logs);
        QueryLogic.Expressions.Register((Entity o) => o.OperationLogs(), () => typeof(OperationLogEntity).NicePluralName());
        QueryLogic.Expressions.Register((Entity o) => o.LastOperationLog(), OperationMessage.LastOperationLog);


        sb.Schema.EntityEventsGlobal.Saving += EntityEventsGlobal_Saving;

        sb.Schema.EntityEvents<OperationSymbol>().PreDeleteSqlSync += Operation_PreDeleteSqlSync;
        sb.Schema.EntityEvents<TypeEntity>().PreDeleteSqlSync += Type_PreDeleteSqlSync;
        sb.Schema.EntityEvents<TypeEntity>().PreDeleteSqlSync += Type_PreDeleteSqlSync_Origin;

        sb.Schema.SchemaCompleted += OperationLogic_Initializing;
        sb.Schema.SchemaCompleted += () => RegisterCurrentLogs(sb.Schema);

        ExceptionLogic.DeleteLogs += ExceptionLogic_DeleteLogs;

        OperationsContainerToken.GetEligibleTypeOperations = (entityType) => OperationsToken_GetEligibleTypeOperations(entityType);
        OperationToken.IsAllowedExtension = (operationSymbol, entityType) => OperationToken_IsAllowedExtension(operationSymbol, entityType);
        OperationToken.BuildExtension = (entityType, operationSymbol, parentExpression) => OperationToken_BuildExpression(entityType, operationSymbol, parentExpression);
    }

    private static IEnumerable<OperationSymbol> OperationsToken_GetEligibleTypeOperations(Type entityType)
    {
        return TypeOperations(entityType)
            .Where((o) =>
                {
                    var op = TryFindOperation(entityType, o.OperationSymbol) as IEntityOperation;

                    if (op == null)
                        return false;

                    return !op.HasCanExecute || op.CanExecuteExpression() != null;
                })
            .Select(o => o.OperationSymbol);
    }


    static MethodInfo miContainsEnumerable = ReflectionTools.GetMethodInfo((IEnumerable<int> s) => s.Contains(2)).GetGenericMethodDefinition();
    static MethodInfo miFormatWith = ReflectionTools.GetMethodInfo((string format) => format.FormatWith("hi"));
    static MethodInfo miNiceToString = ReflectionTools.GetMethodInfo((Enum a) => a.NiceToString());

    private static Expression OperationToken_BuildExpression(Type entityType, OperationSymbol operationSymbol, Expression parentExpression)
    {
        var operation = (IEntityOperation)FindOperation(entityType, operationSymbol);

        LambdaExpression? cee = operation.CanExecuteExpression();

        if (operation.HasCanExecute && cee == null)
            throw new InvalidOperationException($"Operation {operationSymbol} requires CanExecuteExpression to be used as query token");

        var entity = parentExpression.ExtractEntity(false);
        var operationKey = Expression.Constant(operationSymbol.Key);
        var canExecute = cee == null ? Expression.Constant(null, typeof(string)) : (Expression)Expression.Invoke(cee, entity);

        if (operation.StateType != null && operation.UntypedFromStates != null)
        {
            var miContains = miContainsEnumerable.MakeGenericMethod(operation.StateType!);

            var state = Expression.Invoke(operation.GetStateExpression()!, entity);

            var message = OperationMessage.StateShouldBe0InsteadOf1.NiceToString(operation.UntypedFromStates.Cast<Enum>().CommaOr(a => a.NiceToString()), "{0}");

            var stateNiceName = (Expression)Expression.Call(miNiceToString, Expression.Convert(state, typeof(Enum)));

            if (operation.StateType!.IsNullable())
                stateNiceName = Expression.Condition(
                    Expression.Equal(state, Expression.Constant(null, operation.StateType!)),
                    Expression.Constant("null"),
                    stateNiceName);

            canExecute = Expression.Condition(
                Expression.Not(Expression.Call(miContains, Expression.Constant(operation.UntypedFromStates!), state)),
                Expression.Call(miFormatWith, Expression.Constant(message), stateNiceName),
                canExecute);
        }

        var dtoConstructor = typeof(CellOperationDTO).GetConstructor(new[] { typeof(Lite<IEntity>), typeof(string), typeof(string) });

        NewExpression newExpr = Expression.New(dtoConstructor!, entity.BuildLite(), operationKey, canExecute);

        return newExpr;
    }

    private static string? OperationToken_IsAllowedExtension(OperationSymbol operationSymbol, Type entityType)
    {
        return OperationAllowedMessage(operationSymbol, entityType, true, null);
    }

    private static void RegisterCurrentLogs(Schema schema)
    {
        var s = Schema.Current;
        foreach (var t in s.Tables.Values.Where(t => t.SystemVersioned != null))
        {
            giRegisterExpression.GetInvoker(t.Type)();
        }
    }

    static GenericInvoker<Action> giRegisterExpression =
        new(() => RegisterPreviousLog<Entity>());

    public static void RegisterPreviousLog<T>()
        where T : Entity
    {
        QueryLogic.Expressions.Register(
            (T entity) => entity.PreviousOperationLog(),
            () => OperationMessage.PreviousOperationLog.NiceToString());
    }


    public static void ExceptionLogic_DeleteLogs(DeleteLogParametersEmbedded parameters, StringBuilder sb, CancellationToken token)
    {
        void Remove(DateTime? dateLimit, bool withExceptions)
        {
            if (dateLimit == null)
                return;

            var query = Database.Query<OperationLogEntity>().Where(o => o.Start < dateLimit.Value);

            if (withExceptions)
                query = query.Where(a => a.Exception != null);

            query.UnsafeDeleteChunksLog(parameters, sb, token);
        }

        Remove(parameters.GetDateLimitDelete(typeof(OperationLogEntity).ToTypeEntity()), withExceptions: false);
        Remove(parameters.GetDateLimitDeleteWithExceptions(typeof(OperationLogEntity).ToTypeEntity()), withExceptions: true);
    }

    static void OperationLogic_Initializing()
    {
        try
        {
            var types = Schema.Current.Tables.Keys
                .Where(t => EntityKindCache.GetAttribute(t) == null);

            if (types.Any())
                throw new InvalidOperationException($"{0} has not EntityTypeAttribute".FormatWith(types.Select(a => "'" + a.TypeName() + "'").CommaAnd()));

            var errors = (from t in Schema.Current.Tables.Keys
                          let attr = EntityKindCache.GetAttribute(t)
                          where attr.RequiresSaveOperation && !HasSaveLike(t)
                          select attr.IsRequiresSaveOperationOverriden ?
                            "'{0}' has '{1}' set to true, but no operation for saving has been implemented.".FormatWith(t.TypeName(), nameof(attr.RequiresSaveOperation)) :
                            "'{0}' is 'EntityKind.{1}', but no operation for saving has been implemented.".FormatWith(t.TypeName(), attr.EntityKind)).ToList();

            if (errors.Any())
                throw new InvalidOperationException(errors.ToString("\n") + @"
Consider the following options:
* Implement a save operation using sb.Include<T>().WithSave(..), or uinsg the 'save' snippet.
* Change the EntityKind to a more appropiated one.
* Exceptionally, override the property EntityTypeAttribute.RequiresSaveOperation for your particular entity.");
        }
        catch (Exception e) when (StartParameters.IgnoredCodeErrors != null)
        {
            StartParameters.IgnoredCodeErrors.Add(e);
        }
    }

    static SqlPreCommand Operation_PreDeleteSqlSync(OperationSymbol op)
    {
        return Administrator.DeleteWhereScript((OperationLogEntity ol) => ol.Operation, op);
    }

    static SqlPreCommand Type_PreDeleteSqlSync(TypeEntity type)
    {
        var table = Schema.Current.Table<OperationLogEntity>();
        var column = (IColumn)((FieldImplementedByAll)Schema.Current.Field((OperationLogEntity ol) => ol.Target)).TypeColumn;
        return Administrator.DeleteWhereScript(table, column, type.Id);
    }

    static SqlPreCommand? Type_PreDeleteSqlSync_Origin(TypeEntity type)
    {
        if (Administrator.ExistsTable<OperationLogEntity>() && Database.Query<OperationLogEntity>().Any(a => a.Origin != null && a.Origin.EntityType.ToTypeEntity().Is(type)))
        {
            var table = Schema.Current.Table<OperationLogEntity>();
            var column = (IColumn)((FieldImplementedByAll)Schema.Current.Field((OperationLogEntity ol) => ol.Origin)).TypeColumn;
            return Administrator.DeleteWhereScript(table, column, type.Id);
        }

        return null;
    }

    static void EntityEventsGlobal_Saving(Entity ident)
    {
        if (ident.IsGraphModified &&
            EntityKindCache.RequiresSaveOperation(ident.GetType()) && !AllowSaveGlobally && !IsSaveAllowedInContext(ident.GetType()))
            throw new InvalidOperationException("Saving '{0}' is controlled by the operations. Use using(OperationLogic.AllowSave<{0}>()) or execute {1}".FormatWith(
                ident.GetType().Name,
                operations.GetValue(ident.GetType()).Values
                .Where(IsSaveLike)
                .CommaOr(o => o.OperationSymbol.Key)));
    }

    #region Events

    public static event SurroundOperationHandler? SurroundOperation;
    public static event AllowOperationHandler? AllowOperation;

    internal static IDisposable? OnSuroundOperation(IOperation operation, OperationLogEntity log, IEntity? entity, object?[]? args)
    {
        return Disposable.Combine(SurroundOperation, f => f(operation, log, (Entity?)entity, args));
    }

    internal static void SetExceptionData(Exception ex, OperationSymbol operationSymbol, IEntity? entity, object?[]? args)
    {
        ex.Data["operation"] = operationSymbol;
        ex.Data["entity"] = entity;
        if (args != null)
            ex.Data["args"] = args;
    }

    public static bool OperationAllowed(OperationSymbol operationSymbol, Type entityType, bool inUserInterface, Entity? entity)
    {
        if (AllowOperation != null)
            return AllowOperation(operationSymbol, entityType, inUserInterface, entity);
        else
            return true;
    }

    public static string? OperationAllowedMessage(OperationSymbol operationSymbol, Type entityType, bool inUserInterface, Entity? entity)
    {
        if (!OperationAllowed(operationSymbol, entityType, inUserInterface, entity))
            return OperationMessage.Operation01IsNotAuthorized.NiceToString().FormatWith(operationSymbol.NiceToString(), operationSymbol.Key) +
                (inUserInterface ? " " + OperationMessage.InUserInterface.NiceToString() : "");

        return null;
    }

    public static void AssertOperationAllowed(OperationSymbol operationSymbol, Type entityType, bool inUserInterface, Entity? entity)
    {
        var allowed = OperationAllowedMessage(operationSymbol, entityType, inUserInterface, entity);

        if (allowed != null)
            throw new UnauthorizedAccessException(allowed);
    }
    #endregion

    public static void Register(this IOperation operation, bool replace = false)
    {
        if (Schema.Current.IsCompleted)
            throw new InvalidOperationException("Schema already completed");

        if (!operation.OverridenType.IsIEntity())
            throw new InvalidOperationException("Type '{0}' has to implement at least {1}".FormatWith(operation.OverridenType.Name));

        operation.AssertIsValid();

        var dic = operations.GetOrAddDefinition(operation.OverridenType);

        if (replace)
            dic[operation.OperationSymbol] = operation;
        else
            dic.AddOrThrow(operation.OperationSymbol, operation, "Operation {0} has already been registered");

        operations.ClearCache();

        operationsFromKey.Reset();
    }

    private static bool HasSaveLike(Type entityType)
    {
        return TypeOperations(entityType).Any(IsSaveLike);
    }

    private static bool IsSaveLike(IOperation operation)
    {
        return operation is IExecuteOperation && ((IEntityOperation)operation).CanBeModified == true;
    }

    public static List<OperationInfo> ServiceGetOperationInfos(Type entityType, Entity? entity)
    {
        try
        {
            return (from oper in TypeOperations(entityType)
                    where OperationAllowed(oper.OperationSymbol, entityType, true, entity)
                    select ToOperationInfo(oper)).ToList();
        }
        catch (Exception e)
        {
            e.Data["EntityType"] = entityType.TypeName();
            throw;
        }
    }

    public static bool HasConstructOperations(Type entityType)
    {
        return TypeOperations(entityType).Any(o => o.OperationType == OperationType.Constructor);
    }

    public static List<OperationInfo> GetAllOperationInfos(Type entityType)
    {
        return TypeOperations(entityType).Select(o => ToOperationInfo(o)).ToList();
    }

    public static OperationInfo GetOperationInfo(Type type, OperationSymbol operationSymbol)
    {
        return ToOperationInfo(FindOperation(type, operationSymbol));
    }

    public static IEnumerable<OperationSymbol> AllSymbols()
    {
        return operationsFromKey.Value.Keys;
    }

    private static OperationInfo ToOperationInfo(IOperation oper)
    {
        return new OperationInfo(oper.OperationSymbol, oper.OperationType)
        {
            ReturnType = oper.ReturnType,
            HasStates = (oper as IGraphHasStatesOperation)?.HasFromStates,
            HasCanExecute = (oper as IEntityOperation)?.HasCanExecute,
            HasCanExecuteExpression = (oper as IOperation)?.CanExecuteExpression() != null,
            CanBeModified = (oper as IEntityOperation)?.CanBeModified,
            CanBeNew = (oper as IEntityOperation)?.CanBeNew,
            ForReadonlyEntity = (oper as IExecuteOperation)?.ForReadonlyEntity,
            ResultIsSaved = (oper as IConstructorFromOperation)?.ResultIsSaved,
            BaseType = (oper as IEntityOperation)?.BaseType ?? (oper as IConstructorFromManyOperation)?.BaseType
        };
    }

    static readonly Variable<Dictionary<string, object>> multiCanExecuteState = Statics.ThreadVariable<Dictionary<string, object>>("multicanExecuteState");

    public static IDisposable? CreateMultiCanExecuteState()
    {
        var oldState = multiCanExecuteState.Value;
        multiCanExecuteState.Value = new Dictionary<string, object>();
        return new Disposable(() => multiCanExecuteState.Value = oldState);
    }

    public static Dictionary<string, object>? MultiCanExecuteState => multiCanExecuteState.Value;
    public static bool IsMultiCanExecute => multiCanExecuteState.Value != null;

    public static Dictionary<OperationSymbol, string?> ServiceCanExecute(Entity entity)
    {
        try
        {

            using (CreateMultiCanExecuteState())
            {
                var entityType = entity.GetType();

                var result = (from o in TypeOperations(entityType)
                              let eo = o as IEntityOperation
                              where eo != null && (eo.CanBeNew || !entity.IsNew) && OperationAllowed(o.OperationSymbol, entityType, true, entity)
                              select KeyValuePair.Create(eo.OperationSymbol, eo.CanExecute(entity))).ToDictionary();

                return result;
            }


        }
        catch (Exception e)
        {
            e.Data["entity"] = entity;
            throw;
        }
    }


    #region Execute
    public static T Execute<T>(this T entity, ExecuteSymbol<T> symbol, params object?[]? args)
        where T : class, IEntity
    {
        var op = Find<IExecuteOperation>(entity.GetType(), symbol.Symbol).AssertEntity((Entity)(IEntity)entity);
        op.Execute(entity, args);
        return (T)(IEntity)entity;
    }

    public static Entity ServiceExecute(IEntity entity, OperationSymbol operationSymbol, params object?[]? args)
    {
        var op = Find<IExecuteOperation>(entity.GetType(), operationSymbol).AssertEntity((Entity)(IEntity)entity);
        op.Execute(entity, args);
        return (Entity)(IEntity)entity;
    }

    public static T ExecuteLite<T>(this Lite<T> lite, ExecuteSymbol<T> symbol, params object?[]? args)
        where T : class, IEntity
    {
        T entity = lite.Retrieve();
        var op = Find<IExecuteOperation>(lite.EntityType, symbol.Symbol).AssertLite();
        op.Execute(entity, args);
        return entity;
    }

    public static Entity ServiceExecuteLite(Lite<IEntity> lite, OperationSymbol operationSymbol, params object?[]? args)
    {
        Entity entity = (Entity)lite.Retrieve();
        var op = Find<IExecuteOperation>(lite.EntityType, operationSymbol);
        op.Execute(entity, args);
        return entity;
    }


    public static string? CanExecute<T>(this T entity, IEntityOperationSymbolContainer<T> symbol)
        where T : class, IEntity
    {
        var op = Find<IEntityOperation>(entity.GetType(), symbol.Symbol);
        return op.CanExecute(entity);
    }

    public static string? ServiceCanExecute(Entity entity, OperationSymbol operationSymbol)
    {
        var op = Find<IEntityOperation>(entity.GetType(), operationSymbol);
        return op.CanExecute(entity);
    }
    #endregion

    #region Delete

    public static void DeleteLite<T>(this Lite<T> lite, DeleteSymbol<T> symbol, params object?[]? args)
        where T : class, IEntity
    {
        IEntity entity = lite.Retrieve();
        var op = Find<IDeleteOperation>(lite.EntityType, symbol.Symbol).AssertLite();
        op.Delete(entity, args);
    }

    public static void ServiceDelete(Lite<IEntity> lite, OperationSymbol operationSymbol, params object?[]? args)
    {
        IEntity entity = lite.Retrieve();
        var op = Find<IDeleteOperation>(lite.EntityType, operationSymbol);
        op.Delete(entity, args);
    }

    public static void Delete<T>(this T entity, DeleteSymbol<T> symbol, params object?[]? args)
        where T : class, IEntity
    {
        var op = Find<IDeleteOperation>(entity.GetType(), symbol.Symbol).AssertEntity((Entity)(IEntity)entity);
        op.Delete(entity, args);
    }

    public static void ServiceDelete(Entity entity, OperationSymbol operationSymbol, params object?[]? args)
    {
        var op = Find<IDeleteOperation>(entity.GetType(), operationSymbol).AssertEntity((Entity)(IEntity)entity);
        op.Delete(entity, args);
    }
    #endregion

    #region Construct
    public static Entity ServiceConstruct(Type type, OperationSymbol operationSymbol, params object?[]? args)
    {
        var op = Find<IConstructOperation>(type, operationSymbol);
        return (Entity)op.Construct(args);
    }

    public static T Construct<T>(ConstructSymbol<T>.Simple symbol, params object?[]? args)
        where T : class, IEntity
    {
        var op = Find<IConstructOperation>(typeof(T), symbol.Symbol);
        return (T)op.Construct(args);
    }
    #endregion

    #region ConstructFrom

    public static T ConstructFrom<F, T>(this F entity, ConstructSymbol<T>.From<F> symbol, params object?[]? args)
        where T : class, IEntity
        where F : class, IEntity
    {
        var op = Find<IConstructorFromOperation>(entity.GetType(), symbol.Symbol).AssertEntity((Entity)(object)entity);
        return (T)op.Construct(entity, args);
    }

    public static Entity ServiceConstructFrom(IEntity entity, OperationSymbol operationSymbol, params object?[]? args)
    {
        var op = Find<IConstructorFromOperation>(entity.GetType(), operationSymbol).AssertEntity((Entity)(object)entity);
        return (Entity)op.Construct(entity, args);
    }

    public static T ConstructFromLite<F, T>(this Lite<F> lite, ConstructSymbol<T>.From<F> symbol, params object?[]? args)
        where T : class, IEntity
        where F : class, IEntity
    {
        var op = Find<IConstructorFromOperation>(lite.EntityType, symbol.Symbol).AssertLite();
        return (T)op.Construct(Database.Retrieve(lite), args);
    }

    public static Entity ServiceConstructFromLite(Lite<IEntity> lite, OperationSymbol operationSymbol, params object?[]? args)
    {
        var op = Find<IConstructorFromOperation>(lite.EntityType, operationSymbol);
        return (Entity)op.Construct(Database.Retrieve(lite), args);
    }
    #endregion

    #region ConstructFromMany
    public static Entity ServiceConstructFromMany(IEnumerable<Lite<IEntity>> lites, Type type, OperationSymbol operationSymbol, params object?[]? args)
    {
        var onlyType = lites.Select(a => a.EntityType).Distinct().Only();

        return (Entity)Find<IConstructorFromManyOperation>(onlyType ?? type, operationSymbol).Construct(lites, args);
    }

    public static T ConstructFromMany<F, T>(List<Lite<F>> lites, ConstructSymbol<T>.FromMany<F> symbol, params object?[]? args)
        where T : class, IEntity
        where F : class, IEntity
    {
        var onlyType = lites.Select(a => a.EntityType).Distinct().Only();

        return (T)(IEntity)Find<IConstructorFromManyOperation>(onlyType ?? typeof(F), symbol.Symbol).Construct(lites.Cast<Lite<IEntity>>().ToList(), args);
    }
    #endregion

    public static T Find<T>(Type type, OperationSymbol operationSymbol)
        where T : IOperation
    {
        IOperation result = FindOperation(type, operationSymbol);

        if (!(result is T))
            throw new InvalidOperationException("Operation '{0}' is a {1} not a {2} use {3} instead".FormatWith(operationSymbol, result.GetType().TypeName(), typeof(T).TypeName(),
                result is IExecuteOperation ? "Execute" :
                result is IDeleteOperation ? "Delete" :
                result is IConstructOperation ? "Construct" :
                result is IConstructorFromOperation ? "ConstructFrom" :
                result is IConstructorFromManyOperation ? "ConstructFromMany" : null));

        return (T)result;
    }

    public static IOperation FindOperation(Type type, OperationSymbol operationSymbol)
    {
        IOperation? result = TryFindOperation(type, operationSymbol);
        if (result == null)
            throw new InvalidOperationException("Operation '{0}' not found for type {1}".FormatWith(operationSymbol, type));
        return result;
    }

    public static IOperation? TryFindOperation(Type type, OperationSymbol operationSymbol)
    {
        return operations.TryGetValue(type.CleanType())?.TryGetC(operationSymbol);
    }

    public static Graph<T>.Construct FindConstruct<T>(ConstructSymbol<T>.Simple symbol)
        where T : class, IEntity
    {
        return (Graph<T>.Construct)FindOperation(typeof(T), symbol.Symbol);
    }

    public static Graph<T>.ConstructFrom<F> FindConstructFrom<F, T>(ConstructSymbol<T>.From<F> symbol)
        where T : class, IEntity
        where F : class, IEntity
    {
        return (Graph<T>.ConstructFrom<F>)FindOperation(typeof(F), symbol.Symbol);
    }

    public static Graph<T>.ConstructFromMany<F> FindConstructFromMany<F, T>(ConstructSymbol<T>.FromMany<F> symbol)
        where T : class, IEntity
        where F : class, IEntity
    {
        return (Graph<T>.ConstructFromMany<F>)FindOperation(typeof(F), symbol.Symbol);
    }

    public static Graph<T>.Execute FindExecute<T>(ExecuteSymbol<T> symbol)
        where T : class, IEntity
    {
        return (Graph<T>.Execute)FindOperation(typeof(T), symbol.Symbol);
    }

    public static Graph<T>.Delete FindDelete<T>(DeleteSymbol<T> symbol)
        where T : class, IEntity
    {
        return (Graph<T>.Delete)FindOperation(typeof(T), symbol.Symbol);
    }

    static T AssertLite<T>(this T result)
         where T : IEntityOperation
    {
        if (result.CanBeModified)
            throw new InvalidOperationException("Operation {0} is not allowed for Lites".FormatWith(result.OperationSymbol));

        return result;
    }

    static T AssertEntity<T>(this T result, Entity entity)
        where T : IEntityOperation
    {
        if (!result.CanBeModified)
        {
            var list = GraphExplorer.FromRoot(entity).Where(a => a.Modified == ModifiedState.SelfModified);
            if (list.Any())
                throw new InvalidOperationException("Operation {0} needs a Lite or a clean entity, but the entity has changes:\n {1}"
                    .FormatWith(result.OperationSymbol, list.ToString("\n")));
        }

        return result;
    }

    public static IEnumerable<IOperation> TypeOperations(Type type)
    {
        var dic = operations.TryGetValue(type);

        if (dic == null)
            return Enumerable.Empty<IOperation>();

        return dic.Values;
    }

    public static IEnumerable<IOperation> TypeOperationsAndConstructors(Type type)
    {
        var typeOperations = from t in TypeOperations(type)
                             where t.OperationType != Operations.OperationType.ConstructorFrom &&
                             t.OperationType != Operations.OperationType.ConstructorFromMany
                             select t;

        var returnTypeOperations = from kvp in operationsFromKey.Value
                                   select FindOperation(kvp.Value.FirstEx(), kvp.Key) into op
                                   where op.OperationType == Operations.OperationType.ConstructorFrom &&
                                   op.OperationType == OperationType.ConstructorFromMany
                                   where op.ReturnType == type
                                   select op;

        return typeOperations.Concat(returnTypeOperations);
    }

    public static string? InState<T>(this T state, params T[] fromStates) where T : struct, Enum
    {
        if (!fromStates.Contains(state))
            return OperationMessage.StateShouldBe0InsteadOf1.NiceToString().FormatWith(
                fromStates.CommaOr(v => ((Enum)(object)v).NiceToString()),
                ((Enum)(object)state).NiceToString());

        return null;
    }

    public static string? InStateProp<T>(this T state, string propName, params T[] fromStates) where T : struct, Enum
    {
        if (!fromStates.Contains(state))
            return OperationMessage.TheStateOf0ShouldBe1InsteadOf2.NiceToString().FormatWith(
                propName,
                fromStates.CommaOr(v => v.NiceToString()),
                state.NiceToString());

        return null;
    }

    public static string? ShouldBe<T>(this T state, string propName, params T[] fromStates)
         where T : struct, Enum
    {
        if (!fromStates.Contains(state))
            return ValidationMessage._0ShouldBe1InsteadOf2.NiceToString().FormatWith(
                propName,
                fromStates.CommaOr(v => v.NiceToString()),
                state.NiceToString());

        return null;
    }



    public static List<Type> FindTypes(OperationSymbol operation)
    {
        return operationsFromKey.Value
            .TryGetC(operation)
            .EmptyIfNull()
            .ToList();
    }

    internal static IEnumerable<Graph<E, S>.IGraphOperation> GraphOperations<E, S>()
        where E : Entity
    {
        return operations.OverridenValues.SelectMany(d => d.Values).OfType<Graph<E, S>.IGraphOperation>();
    }

    public static bool IsDefined(Type type, OperationSymbol operation)
    {
        return operations.TryGetValue(type)?.TryGetC(operation) != null;
    }

    public static Dictionary<OperationSymbol, string> GetContextualCanExecute(IEnumerable<Lite<IEntity>> lites, List<OperationSymbol> operationSymbols)
    {
        Dictionary<OperationSymbol, string> result = new Dictionary<OperationSymbol, string>();
        foreach (var grLites in lites.GroupBy(a => a.EntityType))
        {
            var operations = operationSymbols.Select(opKey => FindOperation(grLites.Key, opKey)).ToList();

            foreach (var grOperations in operations.Where(a => a.StateType != null).GroupBy(a => a.StateType!))
            {
                if (grOperations.Key != null)
                {
                    var dic = giGetContextualGraphCanExecute.GetInvoker(grLites.Key, grLites.Key, grOperations.Key)(grLites, grOperations);
                    if (result.IsEmpty())
                        result.AddRange(dic);
                    else
                    {
                        foreach (var kvp in dic)
                        {
                            result[kvp.Key] = "\n".Combine(result.TryGetC(kvp.Key), kvp.Value);
                        }
                    }
                }
            }

            var operationsWithCanExecute = operations.Where(a => a.CanExecuteExpression() != null && !result.ContainsKey(a.OperationSymbol));
            var dic2 = giGetCanExecuteExpression.GetInvoker(grLites.Key)(grLites, operationsWithCanExecute);
            if (result.IsEmpty())
                result.AddRange(dic2);
            else
            {
                foreach (var kvp in dic2)
                {
                    result[kvp.Key] = "\n".Combine(result.TryGetC(kvp.Key), kvp.Value);
                }
            }
        }

        return result;
    }

    internal static GenericInvoker<Func<IEnumerable<Lite<IEntity>>, IEnumerable<IOperation>, Dictionary<OperationSymbol, string>>> giGetContextualGraphCanExecute =
        new((lites, operations) => GetContextualGraphCanExecute<Entity, Entity, DayOfWeek>(lites, operations));
    internal static Dictionary<OperationSymbol, string> GetContextualGraphCanExecute<T, E, S>(IEnumerable<Lite<IEntity>> lites, IEnumerable<IOperation> operations)
        where E : Entity
        //where S : struct (nullable enums)
        where T : E
    {
        var getState = Graph<E, S>.GetState;

        var states = lites.Chunk(200).SelectMany(list =>
            Database.Query<T>().Where(e => list.Contains(e.ToLite())).Select(getState).Distinct()).Distinct().ToList();

        return (from o in operations.OfType<Graph<E, S>.IGraphFromStatesOperation>()
                let invalid = states.Where(s => !o.FromStates.Contains(s)).ToList()
                where invalid.Any()
                select KeyValuePair.Create(o.OperationSymbol,
                    OperationMessage.StateShouldBe0InsteadOf1.NiceToString().FormatWith(
                    o.FromStates.CommaOr(v => ((Enum)(object)v).NiceToString()),
                    invalid.CommaOr(v => ((Enum)(object)v).NiceToString())))).ToDictionary();
    }

    internal static GenericInvoker<Func<IEnumerable<Lite<IEntity>>, IEnumerable<IOperation>, Dictionary<OperationSymbol, string>>> giGetCanExecuteExpression =
        new((lites, operation) => GetCanExecuteExpression<Entity>(lites, operation));
    internal static Dictionary<OperationSymbol, string> GetCanExecuteExpression<E>(IEnumerable<Lite<IEntity>> lites, IEnumerable<IOperation> operations)
        where E : Entity
    {
        var eParam = Expression.Parameter(typeof(E));

        var arrayInit = Expression.NewArrayInit(typeof(string), operations.Select(o => Expression.Invoke(o.CanExecuteExpression()!, eParam)));

        var canExecutes = Expression.Lambda<Func<E, string[]>>(arrayInit, eParam);

        var states = lites.Chunk(200).SelectMany(list =>
            Database.Query<E>().Where(e => list.Contains(e.ToLite())).Select(canExecutes).Distinct()).ToList();

        return operations.Select((o, i) => KeyValuePair.Create(o.OperationSymbol, states.Select(s => s[i]).Distinct().NotNull().ToString("\n").VerticalEtc(4)))
            .Where(a => a.Value.HasText())
            .ToDictionaryEx();
    }

    internal static GenericInvoker<Func<IOperation, IEnumerable<Lite<IEntity>>, Dictionary<PrimaryKey, string>>> giGetCanExecute =
        new GenericInvoker<Func<IOperation, IEnumerable<Lite<IEntity>>, Dictionary<PrimaryKey, string>>>((op, lites) => GetCanExecute<Entity>(op, lites));
    static Dictionary<PrimaryKey, string> GetCanExecute<FF>(IOperation operation, IEnumerable<Lite<IEntity>> lites)
        where FF : Entity
    {
        var casted = lites.Cast<Lite<FF>>();
        var eParam = Expression.Parameter(typeof(FF));
        var canExecutes = Expression.Lambda<Func<FF, string?>>(Expression.Invoke(operation.CanExecuteExpression()!, eParam), eParam);
        return Database.Query<FF>().Where(a => lites.Contains(a.ToLite()))
            .Select(e => KeyValuePair.Create(e.Id, canExecutes.Evaluate(e)))
            .ToList()
            .Where(kvp => kvp.Value != null)
            .ToDictionary(a => a.Key, a => a.Value!);
    }


    public static Func<OperationLogEntity, bool> LogOperation = (request) => true;

    public static void SaveLog(this OperationLogEntity log)
    {
        if (!LogOperation(log))
            return;

        using (ExecutionMode.Global())
            log.Save();
    }
}

public static class FluentOperationInclude
{
    public static FluentInclude<T> WithSave<T>(this FluentInclude<T> fi, ExecuteSymbol<T> saveOperation, Action<T, object?[]?>? execute = null)
        where T : Entity
    {
        new Graph<T>.Execute(saveOperation)
        {
            CanBeNew = true,
            CanBeModified = true,
            Execute = execute ?? ((e, _) => { })
        }.Register();

        return fi;
    }

    public static FluentInclude<T> WithDelete<T>(this FluentInclude<T> fi, DeleteSymbol<T> deleteOperation, Action<T, object?[]?>? delete = null)
           where T : Entity
    {
        new Graph<T>.Delete(deleteOperation)
        {
            Delete = delete ?? ((e, _) => e.Delete())
        }.Register();
        return fi;
    }

    public static FluentInclude<T> WithConstruct<T>(this FluentInclude<T> fi, ConstructSymbol<T>.Simple construct, Func<object?[]?, T> constructFunction)
           where T : Entity
    {
        new Graph<T>.Construct(construct)
        {
            Construct = constructFunction
        }.Register();
        return fi;
    }

    public static FluentInclude<T> WithConstruct<T>(this FluentInclude<T> fi, ConstructSymbol<T>.Simple construct)
           where T : Entity, new()
    {
        new Graph<T>.Construct(construct)
        {
            Construct = (_) => new T()
        }.Register();
        return fi;
    }
}

public interface IOperation
{
    OperationSymbol OperationSymbol { get; }
    Type OverridenType { get; }
    OperationType OperationType { get; }
    Type? ReturnType { get; }
    void AssertIsValid();

    IList? UntypedFromStates { get; }
    IList? UntypedToStates { get; }

    Type? StateType { get; }
    LambdaExpression? GetStateExpression();

    LambdaExpression? CanExecuteExpression();
}

public interface IEntityOperation : IOperation
{
    bool CanBeModified { get; }
    bool CanBeNew { get; }
    string? CanExecute(IEntity entity);

    bool HasCanExecute { get; }
    Type BaseType { get; }
}


public delegate IDisposable? SurroundOperationHandler(IOperation operation, OperationLogEntity log, Entity? entity, object?[]? args);
public delegate bool AllowOperationHandler(OperationSymbol operationSymbol, Type entityType, bool inUserInterface, Entity? entity);


