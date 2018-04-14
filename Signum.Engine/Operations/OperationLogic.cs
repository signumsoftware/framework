using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Entities;
using Signum.Utilities.DataStructures;
using System.Reflection;
using Signum.Engine.Maps;
using System.Threading;
using Signum.Utilities.Reflection;
using Signum.Utilities.ExpressionTrees;
using Signum.Engine.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Entities.Reflection;
using System.Linq.Expressions;
using Signum.Entities.Basics;
using System.Data.Common;
using Signum.Engine.Operations.Internal;
using Signum.Engine;

namespace Signum.Engine.Operations
{
    public static class OperationLogic
    {
        static Expression<Func<Entity, IQueryable<OperationLogEntity>>> OperationLogsEntityExpression =
            e => Database.Query<OperationLogEntity>().Where(a => a.Target.RefersTo(e));
        [ExpressionField]
        public static IQueryable<OperationLogEntity> OperationLogs(this Entity e)
        {
            return OperationLogsEntityExpression.Evaluate(e);
        }


        static Expression<Func<Entity, OperationLogEntity>> CurrentOperationLogExpression =
            e => e.OperationLogs().Where(ol => ol.End.HasValue && e.SystemPeriod().Contains(ol.End.Value)).OrderBy(a => a.End.Value).FirstOrDefault();
        [ExpressionField]
        public static OperationLogEntity PreviousOperationLog(this Entity e)
        {
            return CurrentOperationLogExpression.Evaluate(e);
        }

        static Expression<Func<OperationSymbol, IQueryable<OperationLogEntity>>> LogsExpression =
            o => Database.Query<OperationLogEntity>().Where(a => a.Operation == o);
        [ExpressionField]
        public static IQueryable<OperationLogEntity> Logs(this OperationSymbol o)
        {
            return LogsExpression.Evaluate(o);
        }

        static Polymorphic<Dictionary<OperationSymbol, IOperation>> operations = new Polymorphic<Dictionary<OperationSymbol, IOperation>>(PolymorphicMerger.InheritDictionaryInterfaces, typeof(IEntity));

        static ResetLazy<Dictionary<OperationSymbol, List<Type>>> operationsFromKey = new ResetLazy<Dictionary<OperationSymbol, List<Type>>>(() =>
        {
            return (from t in operations.OverridenTypes
                    from d in operations.GetDefinition(t).Keys
                    group t by d into g
                    select KVP.Create(g.Key, g.ToList())).ToDictionary();
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

        public static void AssertStarted(SchemaBuilder sb)
        {
            sb.AssertDefined(ReflectionTools.GetMethodInfo(() => Start(null, null)));
        }

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<OperationLogEntity>()
                    .WithQuery(dqm, () => lo => new
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

                SymbolLogic<OperationSymbol>.Start(sb, dqm, () => RegisteredOperations);

                sb.Include<OperationSymbol>()
                    .WithQuery(dqm, () => os => new
                    {
                        Entity = os,
                        os.Id,
                        os.Key
                    });

                dqm.RegisterExpression((OperationSymbol o) => o.Logs(), () => OperationMessage.Logs.NiceToString());
                dqm.RegisterExpression((Entity o) => o.OperationLogs(), () => typeof(OperationLogEntity).NicePluralName());
             

                sb.Schema.EntityEventsGlobal.Saving += EntityEventsGlobal_Saving;

                sb.Schema.Table<OperationSymbol>().PreDeleteSqlSync += new Func<Entity, SqlPreCommand>(Operation_PreDeleteSqlSync);
                sb.Schema.Table<TypeEntity>().PreDeleteSqlSync += new Func<Entity, SqlPreCommand>(Type_PreDeleteSqlSync);

                sb.Schema.SchemaCompleted += OperationLogic_Initializing;
                sb.Schema.SchemaCompleted += () => RegisterCurrentLogs(sb.Schema, dqm);
                
                ExceptionLogic.DeleteLogs += ExceptionLogic_DeleteLogs;
            }
        }

        private static void RegisterCurrentLogs(Schema schema, DynamicQueryManager dqm)
        {
            var s = Schema.Current;
            foreach (var t in s.Tables.Values.Where(t => t.SystemVersioned != null))
            {
                giRegisterExpression.GetInvoker(t.Type)(dqm);
            }
        }

        static GenericInvoker<Action<DynamicQueryManager>> giRegisterExpression =
            new GenericInvoker<Action<DynamicQueryManager>>((dqm) => RegisterPreviousLog<Entity>(dqm));

        public static void RegisterPreviousLog<T>(DynamicQueryManager dqm)
            where T : Entity
        {
            dqm.RegisterExpression(
                (T entity) => entity.PreviousOperationLog(),
                () => OperationMessage.PreviousOperationLog.NiceToString());
        }


        public static void ExceptionLogic_DeleteLogs(DeleteLogParametersEmbedded parameters, StringBuilder sb, CancellationToken token)
        {
            var dateLimit = parameters.GetDateLimitDelete(typeof(OperationLogEntity).ToTypeEntity());

            if (dateLimit == null)
                return;

            Database.Query<OperationLogEntity>().Where(o => o.Start < dateLimit.Value).UnsafeDeleteChunksLog(parameters, sb, token);
        }

        static void OperationLogic_Initializing()
        {
            try
            {
                var types = Schema.Current.Tables.Keys
                    .Where(t => EntityKindCache.GetAttribute(t) == null)
                    .Select(a => "'" + a.TypeName() + "'")
                    .CommaAnd();

                if (types.HasText())
                    throw new InvalidOperationException($"{0} has not EntityTypeAttribute".FormatWith(types));

                var errors = (from t in Schema.Current.Tables.Keys
                              let attr = EntityKindCache.GetAttribute(t)
                              where attr.RequiresSaveOperation && !HasExecuteNoLite(t)
                              select attr.IsRequiresSaveOperationOverriden ?
                                "'{0}' has '{1}' set to true, but no operation for saving has been implemented.".FormatWith(t.TypeName(), nameof(attr.RequiresSaveOperation)) :
                                "'{0}' is 'EntityKind.{1}', but no operation for saving has been implemented.".FormatWith(t.TypeName(), attr.EntityKind)).ToList();

                if (errors.Any())
                    throw new InvalidOperationException(errors.ToString("\r\n") + @"
Consider the following options:
    * Implement an operation for saving using 'save' snippet.
    * Change the EntityKind to a more appropiated one. 
    * Exceptionally, override the property EntityTypeAttribute.RequiresSaveOperation for your particular entity.");
            }
            catch (Exception e) when (StartParameters.IgnoredCodeErrors != null)
            {
                StartParameters.IgnoredCodeErrors.Add(e);
            }
        }

        static SqlPreCommand Operation_PreDeleteSqlSync(Entity arg)
        {
            return Administrator.DeleteWhereScript((OperationLogEntity ol) => ol.Operation, (OperationSymbol)arg);
        }

        static SqlPreCommand Type_PreDeleteSqlSync(Entity arg)
        {
            var table = Schema.Current.Table<OperationLogEntity>();
            var column = (IColumn)((FieldImplementedByAll)Schema.Current.Field((OperationLogEntity ol) => ol.Target)).ColumnType;
            return Administrator.DeleteWhereScript(table, column, ((TypeEntity)arg).Id);
        }

        static void EntityEventsGlobal_Saving(Entity ident)
        {
            if (ident.IsGraphModified && 
                EntityKindCache.RequiresSaveOperation(ident.GetType()) && !AllowSaveGlobally && !IsSaveAllowedInContext(ident.GetType()))
                throw new InvalidOperationException("Saving '{0}' is controlled by the operations. Use OperationLogic.AllowSave<{0}>() or execute {1}".FormatWith(
                    ident.GetType().Name,
                    operations.GetValue(ident.GetType()).Values
                    .Where(IsExecuteNoLite)
                    .CommaOr(o => o.OperationSymbol.Key)));
        }

        #region Events

        public static event SurroundOperationHandler SurroundOperation;
        public static event AllowOperationHandler AllowOperation;

        internal static IDisposable OnSuroundOperation(IOperation operation, OperationLogEntity log, IEntity entity, object[] args)
        {
            return Disposable.Combine(SurroundOperation, f => f(operation, log, (Entity)entity, args));
        }

        internal static void SetExceptionData(Exception ex, OperationSymbol operationSymbol, IEntity entity, object[] args)
        {
            ex.Data["operation"] = operationSymbol;
            ex.Data["entity"] = entity;
            if (args != null)
                ex.Data["args"] = args;
        }

        public static bool OperationAllowed(OperationSymbol operationSymbol, Type entityType, bool inUserInterface)
        {
            if (AllowOperation != null)
                return AllowOperation(operationSymbol, entityType, inUserInterface);
            else
                return true;
        }

        public static void AssertOperationAllowed(OperationSymbol operationSymbol, Type entityType, bool inUserInterface)
        {
            if (!OperationAllowed(operationSymbol, entityType, inUserInterface))
                throw new UnauthorizedAccessException(OperationMessage.Operation01IsNotAuthorized.NiceToString().FormatWith(operationSymbol.NiceToString(), operationSymbol.Key) +
                    (inUserInterface ? " " + OperationMessage.InUserInterface.NiceToString() : ""));
        }
        #endregion

        public static void Register(this IOperation operation, bool replace = false)
        {
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

        private static bool HasExecuteNoLite(Type entityType)
        {
            return TypeOperations(entityType).Any(IsExecuteNoLite);
        }

        private static bool IsExecuteNoLite(IOperation operation)
        {
            return operation is IExecuteOperation && ((IEntityOperation)operation).Lite == false;
        }

        public static List<OperationInfo> ServiceGetOperationInfos(Type entityType)
        {
            try
            {
                return (from oper in TypeOperations(entityType)
                        where OperationAllowed(oper.OperationSymbol, entityType, true)
                        select ToOperationInfo(oper)).ToList();
            }
            catch(Exception e)
            {
                e.Data["EntityType"] = entityType.TypeName();
                throw;
            }
        }

        public static bool HasConstructOperations(Type entityType)
        {
            return TypeOperations(entityType).Any(o => o.OperationType == Entities.OperationType.Constructor);
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
            return new OperationInfo
            {
                OperationSymbol = oper.OperationSymbol,
                Lite = (oper as IEntityOperation)?.Lite,
                Returns = oper.Returns,
                OperationType = oper.OperationType,
                ReturnType = oper.ReturnType,
                HasStates = (oper as IGraphHasFromStatesOperation)?.HasFromStates,
                HasCanExecute = (oper as IEntityOperation)?.HasCanExecute,
                AllowsNew = (oper as IEntityOperation)?.AllowsNew,
                BaseType = (oper as IEntityOperation)?.BaseType ?? (oper as IConstructorFromManyOperation)?.BaseType
            };
        }

        public static Dictionary<OperationSymbol, string> ServiceCanExecute(Entity entity)
        {
            try
            {
                var entityType = entity.GetType();

                return (from o in TypeOperations(entityType)
                        let eo = o as IEntityOperation
                        where eo != null && (eo.AllowsNew || !entity.IsNew) && OperationAllowed(o.OperationSymbol, entityType, true)
                        select KVP.Create(eo.OperationSymbol, eo.CanExecute(entity))).ToDictionary();
            }
            catch(Exception e)
            {
                e.Data["entity"] = entity.BaseToString();
                throw;
            }
        }


        #region Execute
        public static T Execute<T>(this T entity, ExecuteSymbol<T> symbol, params object[] args)
            where T : class, IEntity
        {
            var op = Find<IExecuteOperation>(entity.GetType(), symbol.Symbol).AssertEntity((Entity)(IEntity)entity);
            op.Execute(entity, args);
            return (T)(IEntity)entity;
        }

        public static Entity ServiceExecute(IEntity entity, OperationSymbol operationSymbol, params object[] args)
        {
            var op = Find<IExecuteOperation>(entity.GetType(), operationSymbol).AssertEntity((Entity)(IEntity)entity);
            op.Execute(entity, args);
            return (Entity)(IEntity)entity;
        }

        public static T ExecuteLite<T>(this Lite<T> lite, ExecuteSymbol<T> symbol, params object[] args)
            where T : class, IEntity
        {
            T entity = lite.RetrieveAndForget();
            var op = Find<IExecuteOperation>(lite.EntityType, symbol.Symbol).AssertLite();
            op.Execute(entity, args);
            return entity;
        }

        public static Entity ServiceExecuteLite(Lite<IEntity> lite, OperationSymbol operationSymbol, params object[] args)
        {
            Entity entity = (Entity)lite.RetrieveAndForget();
            var op = Find<IExecuteOperation>(lite.EntityType, operationSymbol);
            op.Execute(entity, args);
            return entity;
        }


        public static string CanExecute<T>(this T entity, IEntityOperationSymbolContainer<T> symbol)
            where T : class, IEntity
        {
            var op = Find<IEntityOperation>(entity.GetType(), symbol.Symbol);
            return op.CanExecute(entity);
        }

        public static string ServiceCanExecute(Entity entity, OperationSymbol operationSymbol)
        {
            var op = Find<IEntityOperation>(entity.GetType(), operationSymbol);
            return op.CanExecute(entity);
        }
        #endregion

        #region Delete

        public static void DeleteLite<T>(this Lite<T> lite, DeleteSymbol<T> symbol, params object[] args)
            where T : class, IEntity
        {
            IEntity entity = lite.RetrieveAndForget();
            var op = Find<IDeleteOperation>(lite.EntityType, symbol.Symbol).AssertLite();
            op.Delete(entity, args);
        }

        public static void ServiceDelete(Lite<IEntity> lite, OperationSymbol operationSymbol, params object[] args)
        {
            IEntity entity = lite.RetrieveAndForget();
            var op = Find<IDeleteOperation>(lite.EntityType, operationSymbol);
            op.Delete(entity, args);
        }

        public static void Delete<T>(this T entity, DeleteSymbol<T> symbol, params object[] args)
            where T : class, IEntity
        {
            var op = Find<IDeleteOperation>(entity.GetType(), symbol.Symbol).AssertEntity((Entity)(IEntity)entity);
            op.Delete(entity, args);
        }

        public static void ServiceDelete(Entity entity, OperationSymbol operationSymbol, params object[] args)
        {
            var op = Find<IDeleteOperation>(entity.GetType(), operationSymbol).AssertEntity((Entity)(IEntity)entity);
            op.Delete(entity, args);
        }
        #endregion

        #region Construct
        public static Entity ServiceConstruct(Type type, OperationSymbol operationSymbol, params object[] args)
        {
            var op = Find<IConstructOperation>(type, operationSymbol);
            return (Entity)op.Construct(args);
        }

        public static T Construct<T>(ConstructSymbol<T>.Simple symbol, params object[] args)
            where T : class, IEntity
        {
            var op = Find<IConstructOperation>(typeof(T), symbol.Symbol);
            return (T)op.Construct(args);
        }
        #endregion

        #region ConstructFrom

        public static T ConstructFrom<F, T>(this F entity, ConstructSymbol<T>.From<F> symbol, params object[] args)
            where T : class, IEntity
            where F : class, IEntity
        {
            var op = Find<IConstructorFromOperation>(entity.GetType(), symbol.Symbol).AssertEntity((Entity)(object)entity);
            return (T)op.Construct(entity, args);
        }

        public static Entity ServiceConstructFrom(IEntity entity, OperationSymbol operationSymbol, params object[] args)
        {
            var op = Find<IConstructorFromOperation>(entity.GetType(), operationSymbol).AssertEntity((Entity)(object)entity);
            return (Entity)op.Construct(entity, args);
        }

        public static T ConstructFromLite<F, T>(this Lite<F> lite, ConstructSymbol<T>.From<F> symbol, params object[] args)
            where T : class, IEntity
            where F : class, IEntity
        {
            var op = Find<IConstructorFromOperation>(lite.EntityType, symbol.Symbol).AssertLite();
            return (T)op.Construct(Database.RetrieveAndForget(lite), args);
        }

        public static Entity ServiceConstructFromLite(Lite<IEntity> lite, OperationSymbol operationSymbol, params object[] args)
        {
            var op = Find<IConstructorFromOperation>(lite.EntityType, operationSymbol);
            return (Entity)op.Construct(Database.RetrieveAndForget(lite), args);
        }
        #endregion

        #region ConstructFromMany
        public static Entity ServiceConstructFromMany(IEnumerable<Lite<IEntity>> lites, Type type, OperationSymbol operationSymbol, params object[] args)
        {
            var onlyType = lites.Select(a => a.EntityType).Distinct().Only();

            return (Entity)Find<IConstructorFromManyOperation>(onlyType ?? type, operationSymbol).Construct(lites, args);
        }

        public static T ConstructFromMany<F, T>(List<Lite<F>> lites, ConstructSymbol<T>.FromMany<F> symbol, params object[] args)
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
            IOperation result = TryFindOperation(type, operationSymbol);
            if (result == null)
                throw new InvalidOperationException("Operation '{0}' not found for type {1}".FormatWith(operationSymbol, type));
            return result;
        }

        public static IOperation TryFindOperation(Type type, OperationSymbol operationSymbol)
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
            if (!result.Lite)
                throw new InvalidOperationException("Operation {0} is not allowed for Lites".FormatWith(result.OperationSymbol));

            return result;
        }

        static T AssertEntity<T>(this T result, Entity entity)
            where T : IEntityOperation
        {
            if (result.Lite)
            {
                var list = GraphExplorer.FromRoot(entity).Where(a => a.Modified == ModifiedState.SelfModified);
                if (list.Any())
                    throw new InvalidOperationException("Operation {0} needs a Lite or a clean entity, but the entity has changes:\r\n {1}"
                        .FormatWith(result.OperationSymbol, list.ToString("\r\n")));
            }

            return result;
        }

        public static bool IsLite(OperationSymbol operationSymbol)
        {
            return operationsFromKey.Value.TryGetC(operationSymbol)
                .EmptyIfNull()
                .Select(t => FindOperation(t, operationSymbol))
                .OfType<IEntityOperation>()
                .Select(a => a.Lite)
                .FirstOrDefault();
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
                                 where t.OperationType != Entities.OperationType.ConstructorFrom &&
                                 t.OperationType != Entities.OperationType.ConstructorFromMany
                                 select t;

            var returnTypeOperations = from kvp in operationsFromKey.Value
                                       select FindOperation(kvp.Value.FirstEx(), kvp.Key) into op
                                       where op.OperationType == Entities.OperationType.ConstructorFrom && 
                                       op.OperationType == Entities.OperationType.ConstructorFromMany
                                       where op.ReturnType == type
                                       select op;

            return typeOperations.Concat(returnTypeOperations);
        }

        public static string InState<T>(this T state, params T[] fromStates) where T : struct
        {
            if (!fromStates.Contains(state))
                return OperationMessage.StateShouldBe0InsteadOf1.NiceToString().FormatWith(
                    fromStates.CommaOr(v => ((Enum)(object)v).NiceToString()),
                    ((Enum)(object)state).NiceToString());

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

        public static OperationType OperationType(Type type, OperationSymbol operationSymbol)
        {
            return FindOperation(type, operationSymbol).OperationType;
        }

        public static Dictionary<OperationSymbol, string> GetContextualCanExecute(IEnumerable<Lite<IEntity>> lites, List<OperationSymbol> operationSymbols)
        {
            Dictionary<OperationSymbol, string> result = null;
            using (ExecutionMode.Global())
            {
                foreach (var grLites in lites.GroupBy(a => a.EntityType))
                {
                    var operations = operationSymbols.Select(opKey => FindOperation(grLites.Key, opKey)).ToList();

                    foreach (var grOperations in operations.GroupBy(a => a.GetType().GetGenericArguments().Let(arr=>Tuple.Create(arr[0], arr[1]))))
                    {
                        var dic = giGetContextualGraphCanExecute.GetInvoker(grLites.Key, grOperations.Key.Item1, grOperations.Key.Item2)(grLites, grOperations);
                        if (result == null)
                            result = dic;
                        else
                        {
                            foreach (var kvp in dic)
                            {
                                result[kvp.Key] = "\r\n".Combine(result.TryGetC(kvp.Key), kvp.Value);
                            }
                        }
                    }
                }
            }

            return result;
        }

        internal static GenericInvoker<Func<IEnumerable<Lite<IEntity>>, IEnumerable<IOperation>, Dictionary<OperationSymbol, string>>> giGetContextualGraphCanExecute =
            new GenericInvoker<Func<IEnumerable<Lite<IEntity>>, IEnumerable<IOperation>, Dictionary<OperationSymbol, string>>>((lites, operations) => GetContextualGraphCanExecute<Entity, Entity, DayOfWeek>(lites, operations));
        internal static Dictionary<OperationSymbol, string> GetContextualGraphCanExecute<T, E, S>(IEnumerable<Lite<IEntity>> lites, IEnumerable<IOperation> operations)
            where E : Entity
            //where S : struct (nullable enums)
            where T : E
        {
            var getState = Graph<E, S>.GetState;
            
            var states = lites.GroupsOf(200).SelectMany(list =>
                Database.Query<T>().Where(e => list.Contains(e.ToLite())).Select(getState).Distinct()).Distinct().ToList();

            return (from o in operations.Cast<Graph<E, S>.IGraphFromStatesOperation>()
                    let invalid = states.Where(s => !o.FromStates.Contains(s)).ToList()
                    where invalid.Any()
                    select KVP.Create(o.OperationSymbol,
                        OperationMessage.StateShouldBe0InsteadOf1.NiceToString().FormatWith(
                        o.FromStates.CommaOr(v => ((Enum)(object)v).NiceToString()),
                        invalid.CommaOr(v => ((Enum)(object)v).NiceToString())))).ToDictionary();
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
        public static FluentInclude<T> WithSave<T>(this FluentInclude<T> fi, ExecuteSymbol<T> saveOperation)
            where T : Entity
        {
            new Graph<T>.Execute(saveOperation)
            {
                AllowsNew = true,
                Lite = false,
                Execute = (e, _) => { }
            }.Register();
            return fi;
        }

        public static FluentInclude<T> WithDelete<T>(this FluentInclude<T> fi, DeleteSymbol<T> delete)
               where T : Entity
        {
            new Graph<T>.Delete(delete)
            {
                Delete = (e, _) => e.Delete()
            }.Register();
            return fi;
        }

        public static FluentInclude<T> WithConstruct<T>(this FluentInclude<T> fi, ConstructSymbol<T>.Simple construct, Func<object[], T> constructFunction)
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
        bool Returns { get; }
        Type ReturnType { get; }
        void AssertIsValid();

        IEnumerable<Enum> UntypedFromStates { get; }
        IEnumerable<Enum> UntypedToStates { get; }
        Type StateType { get; }
    }

    public interface IEntityOperation : IOperation
    {
        bool Lite { get; }
        bool AllowsNew { get; }
        string CanExecute(IEntity entity);
        bool HasCanExecute { get; }
        Type BaseType { get; }
    }


    public delegate IDisposable SurroundOperationHandler(IOperation operation, OperationLogEntity log, Entity entity, object[] args);
    public delegate void OperationHandler(IOperation operation, Entity entity);
    public delegate void ErrorOperationHandler(IOperation operation, Entity entity, Exception ex);
    public delegate bool AllowOperationHandler(OperationSymbol operationSymbol, Type entityType, bool inUserInterface);


}
