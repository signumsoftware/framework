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

namespace Signum.Engine.Operations
{
    public static class OperationLogic
    {

        static Expression<Func<IdentifiableEntity, IQueryable<OperationLogDN>>> OperationLogsEntityExpression =
            e => Database.Query<OperationLogDN>().Where(a => a.Target.RefersTo(e));
        [ExpressionField("OperationLogsEntityExpression")]
        public static IQueryable<OperationLogDN> OperationLogs(this IdentifiableEntity e)
        {
            return OperationLogsEntityExpression.Evaluate(e);
        }

        static Expression<Func<OperationSymbol, IQueryable<OperationLogDN>>> LogsExpression =
            o => Database.Query<OperationLogDN>().Where(a => a.Operation == o);
        public static IQueryable<OperationLogDN> Logs(this OperationSymbol o)
        {
            return LogsExpression.Evaluate(o);
        }

        static Polymorphic<Dictionary<OperationSymbol, IOperation>> operations = new Polymorphic<Dictionary<OperationSymbol, IOperation>>(PolymorphicMerger.InheritDictionaryInterfaces, typeof(IIdentifiable));

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


        static readonly Polymorphic<Tuple<bool>> saveProtectedTypes = new Polymorphic<Tuple<bool>>(PolymorphicMerger.InheritanceAndInterfaces, typeof(IIdentifiable));

        static readonly Variable<ImmutableStack<Type>> allowedTypes = Statics.ThreadVariable<ImmutableStack<Type>>("saveOperationsAllowedTypes");

        public static bool AllowSaveGlobally { get; set; }

        public static bool IsSaveProtectedAllowed(Type type)
        {
            if (AllowSaveGlobally)
                return true;

            var stack = allowedTypes.Value;
            return (stack != null && stack.Contains(type));
        }

        public static bool IsSaveProtected(Type type)
        {
            if (!typeof(IIdentifiable).IsAssignableFrom(type))
                return false;

            var tuple = saveProtectedTypes.TryGetValue(type);

            return tuple != null && tuple.Item1;
        }

        public static HashSet<Type> GetSaveProtectedTypes()
        {
            return TypeLogic.TypeToDN.Keys.Where(IsSaveProtected).ToHashSet();
        }
        
        public static void SetProtectedSave<T>(bool? isProtected) where T : IIdentifiable
        {
            SetProtectedSave(typeof(T), isProtected);
        }

        public static void SetProtectedSave(Type type, bool? isProtected)
        {
            saveProtectedTypes.SetDefinition(type, isProtected == null ? null : Tuple.Create(isProtected.Value));
        }

        public static IDisposable AllowSave<T>() where T : class, IIdentifiable
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
                sb.Include<OperationLogDN>();

                SymbolLogic<OperationSymbol>.Start(sb, () => RegisteredOperations);

                dqm.RegisterQuery(typeof(OperationSymbol), () =>
                    from os in Database.Query<OperationSymbol>()
                    select new
                    {
                        Entity = os,
                        os.Id,
                        os.Key
                    });

                dqm.RegisterQuery(typeof(OperationLogDN), () =>
                    from lo in Database.Query<OperationLogDN>()
                    select new
                    {
                        Entity = lo,
                        lo.Id,
                        Target = lo.Target,
                        lo.Operation,
                        User = lo.User,
                        lo.Start,
                        lo.End,
                        lo.Exception
                    });

                dqm.RegisterExpression((OperationSymbol o) => o.Logs(), () => OperationMessage.Logs.NiceToString());
                dqm.RegisterExpression((IdentifiableEntity o) => o.OperationLogs(), () => typeof(OperationLogDN).NicePluralName());

                sb.Schema.EntityEventsGlobal.Saving += EntityEventsGlobal_Saving;

                sb.Schema.Table<OperationSymbol>().PreDeleteSqlSync += new Func<IdentifiableEntity, SqlPreCommand>(Operation_PreDeleteSqlSync);
                sb.Schema.Table<TypeDN>().PreDeleteSqlSync += new Func<IdentifiableEntity, SqlPreCommand>(Type_PreDeleteSqlSync);

                sb.Schema.Initializing[InitLevel.Level0SyncEntities] += OperationLogic_Initializing;
            }
        }

        static void OperationLogic_Initializing()
        {
            var errors = (from t in Schema.Current.Tables.Keys
                          let et = EntityKindCache.TryGetAttribute(t)
                          let sp = IsSaveProtected(t)
                          select et == null ? "{0} has no EntityTypeAttribute set".Formato(t.FullName) :
                          sp != RequiresSaveProtected(et.EntityKind) ? "\t{0} is {1} but is {2}'save protected'".Formato(t.TypeName(), et.EntityKind, sp ? "" : "NOT ") :
                          null).NotNull().OrderBy().ToString("\r\n");

            if (errors.HasText())
                throw new InvalidOperationException("EntityKind - OperationLogic inconsistencies: \r\n" + errors + "\r\nNote: An entity type becomes 'save protected' when a Save operation is defined for it");
        }

        private static bool RequiresSaveProtected(EntityKind entityType)
        {
            switch (entityType)
            {
                case EntityKind.SystemString:
                case EntityKind.System:
                case EntityKind.Relational:
                    return false;

                case EntityKind.String:
                case EntityKind.Shared:
                case EntityKind.Main:
                    return true;

                case EntityKind.Part:
                case EntityKind.SharedPart:                
                    return false;

                default:
                    throw new InvalidOperationException("Unexpected {0}".Formato(entityType)); 
            }
        }
        static SqlPreCommand Operation_PreDeleteSqlSync(IdentifiableEntity arg)
        {
            var t = Schema.Current.Table<OperationLogDN>();
            var f = (FieldReference)t.Fields["operation"].Field;

            var param = Connector.Current.ParameterBuilder.CreateReferenceParameter("@id", false, arg.Id);

            return new SqlPreCommandSimple("DELETE FROM {0} WHERE {1} = {2}".Formato(t.Name, f.Name, param.ParameterName), new List<DbParameter> { param });
        }

        static SqlPreCommand Type_PreDeleteSqlSync(IdentifiableEntity arg)
        {
            var t = Schema.Current.Table<OperationLogDN>();
            var f = ((FieldImplementedByAll)t.Fields["target"].Field).ColumnType;

            var param = Connector.Current.ParameterBuilder.CreateReferenceParameter("@id", false, arg.Id);

            return new SqlPreCommandSimple("DELETE FROM {0} WHERE {1} = {2}".Formato(t.Name, f.Name, param.ParameterName), new List<DbParameter> { param });
        }

        static void EntityEventsGlobal_Saving(IdentifiableEntity ident)
        {
            if (ident.IsGraphModified && 
                IsSaveProtected(ident.GetType()) && 
                !IsSaveProtectedAllowed(ident.GetType()))
                throw new InvalidOperationException("Saving '{0}' is controlled by the operations. Use OperationLogic.AllowSave<{0}>() or execute {1}".Formato(
                    ident.GetType().Name,
                    operations.GetValue(ident.GetType()).Values
                    .Where(IsExecuteNoLite)
                    .CommaOr(o => o.OperationSymbol.Key)));
        }

        #region Events

        public static event SurroundOperationHandler SurroundOperation;
        public static event OperationHandler BeginOperation;
        public static event OperationHandler EndOperation;
        public static event ErrorOperationHandler ErrorOperation;
        public static event AllowOperationHandler AllowOperation;

        internal static IDisposable OnSuroundOperation(IOperation operation, IIdentifiable entity, object[] args)
        {
            if (SurroundOperation == null)
                return null;

            IDisposable result = null;
            foreach (SurroundOperationHandler surround in SurroundOperation.GetInvocationList())
            {
                result = Disposable.Combine(result, surround(operation, (IdentifiableEntity)entity, args));
            }

            return result;
        }

        internal static void OnBeginOperation(IOperation operation, IIdentifiable entity)
        {
            if (BeginOperation != null)
                BeginOperation(operation, (IdentifiableEntity)entity);
        }

        internal static void OnEndOperation(IOperation operation, IIdentifiable entity)
        {
            if (EndOperation != null)
                EndOperation(operation, (IdentifiableEntity)entity);
        }

        internal static void OnErrorOperation(IOperation operation, IIdentifiable entity, object[] args, Exception ex)
        {
            ex.Data["entity"] = entity;
            if (args != null)
                ex.Data["args"] = args;

            if (ErrorOperation != null)
                ErrorOperation(operation, (IdentifiableEntity)entity, ex);
        }

        public static bool OperationAllowed(OperationSymbol operationSymbol, bool inUserInterface)
        {
            if (AllowOperation != null)
                return AllowOperation(operationSymbol, inUserInterface);
            else
                return true;
        }

        public static void AssertOperationAllowed(OperationSymbol operationSymbol, bool inUserInterface)
        {
            if (!OperationAllowed(operationSymbol, inUserInterface))
                throw new UnauthorizedAccessException(OperationMessage.Operation01IsNotAuthorized.NiceToString().Formato(operationSymbol.NiceToString(), operationSymbol.Key) +
                    (inUserInterface ? " " + OperationMessage.InUserInterface.NiceToString() : ""));
        }
        #endregion



        public static void Register(this IOperation operation)
        {
            if (!operation.Type.IsIIdentifiable())
                throw new InvalidOperationException("Type '{0}' has to implement at least {1}".Formato(operation.Type.Name));

            operation.AssertIsValid();

            operations.GetOrAddDefinition(operation.Type).AddOrThrow(operation.OperationSymbol, operation, "Operation {0} has already been registered");

            operationsFromKey.Reset();

            if (IsExecuteNoLite(operation))
            {
                SetProtectedSave(operation.Type, true);
            }
        }

        private static bool IsExecuteNoLite(IOperation operation)
        {
            return operation is IExecuteOperation && ((IEntityOperation)operation).Lite == false;
        }

        public static void RegisterReplace(this IOperation operation)
        {
            if (!operation.Type.IsIIdentifiable())
                throw new InvalidOperationException("Type {0} has to implement at least {1}".Formato(operation.Type));

            operation.AssertIsValid();

            operations.GetOrAddDefinition(operation.Type)[operation.OperationSymbol] = operation;

            operationsFromKey.Reset(); //unnecesarry?
        }

        public static List<OperationInfo> ServiceGetOperationInfos(Type entityType)
        {
            try
            {
                return (from oper in TypeOperations(entityType)
                        where OperationAllowed(oper.OperationSymbol, true)
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
                Lite = (oper as IEntityOperation).Try(eo => eo.Lite),
                Returns = oper.Returns,
                OperationType = oper.OperationType,
                ReturnType = oper.ReturnType,
                HasStates = (oper as IGraphHasFromStatesOperation).Try(eo => eo.HasFromStates) ?? false,
                HasCanExecute = (oper as IEntityOperation).Try(eo => eo.HasCanExecute) ?? false,
                AllowsNew = (oper as IEntityOperation).Try(eo => eo.AllowsNew) ?? false,
            };
        }

        public static Dictionary<OperationSymbol, string> ServiceCanExecute(IdentifiableEntity entity)
        {
            try
            {
                return (from o in TypeOperations(entity.GetType())
                        let eo = o as IEntityOperation
                        where eo != null && (eo.AllowsNew || !entity.IsNew) && OperationAllowed(o.OperationSymbol, true)
                        select KVP.Create(eo.OperationSymbol, eo.CanExecute(entity))).ToDictionary();
            }
            catch(Exception e)
            {
                e.Data["entity"] = entity.BaseToString();
                throw;
            }
        }


        #region Execute
        public static T Execute<T, B>(this T entity, ExecuteSymbol<B> symbol, params object[] args)
            where T : class, IIdentifiable, B
            where B : class, IIdentifiable
        {
            var op = Find<IExecuteOperation>(entity.GetType(), symbol.Operation).AssertEntity((IdentifiableEntity)(IIdentifiable)entity);
            op.Execute(entity, args);
            return (T)(IIdentifiable)entity;
        }

        public static IdentifiableEntity ServiceExecute(IIdentifiable entity, OperationSymbol operationSymbol, params object[] args)
        {
            var op = Find<IExecuteOperation>(entity.GetType(), operationSymbol).AssertEntity((IdentifiableEntity)(IIdentifiable)entity);
            op.Execute(entity, args);
            return (IdentifiableEntity)(IIdentifiable)entity;
        }

        public static T ExecuteLite<T, B>(this Lite<T> lite, ExecuteSymbol<B> symbol, params object[] args)
            where T : class, IIdentifiable, B
            where B : class, IIdentifiable
        {
            T entity = lite.RetrieveAndForget();
            var op = Find<IExecuteOperation>(lite.EntityType, symbol.Operation).AssertLite();
            op.Execute(entity, args);
            return entity;
        }

        public static IdentifiableEntity ServiceExecuteLite(Lite<IIdentifiable> lite, OperationSymbol operationSymbol, params object[] args)
        {
            IdentifiableEntity entity = (IdentifiableEntity)lite.RetrieveAndForget();
            var op = Find<IExecuteOperation>(lite.EntityType, operationSymbol).AssertLite();
            op.Execute(entity, args);
            return entity;
        }


        public static string CanExecute<T, B>(this T entity, IEntityOperationSymbolContainer<B> symbol)
            where T : class, IIdentifiable, B
            where B : class, IIdentifiable
        {
            var op = Find<IEntityOperation>(entity.GetType(), symbol.Operation);
            return op.CanExecute(entity);
        }

        public static string ServiceCanExecute(IdentifiableEntity entity, OperationSymbol operationSymbol)
        {
            var op = Find<IEntityOperation>(entity.GetType(), operationSymbol);
            return op.CanExecute(entity);
        }
        #endregion

        #region Delete

        public static void Delete<T, B>(this Lite<T> lite, DeleteSymbol<B> symbol, params object[] args)
            where T : class, IIdentifiable, B
            where B : class, IIdentifiable
        {
            IIdentifiable entity = lite.RetrieveAndForget();
            var op = Find<IDeleteOperation>(lite.EntityType, symbol.Operation);
            op.Delete(entity, args);
        }

        public static void ServiceDelete(Lite<IIdentifiable> lite, OperationSymbol operationSymbol, params object[] args)
        {
            IIdentifiable entity = lite.RetrieveAndForget();
            var op = Find<IDeleteOperation>(lite.EntityType, operationSymbol);
            op.Delete(entity, args);
        }

        public static void Delete<T, B>(this T entity, DeleteSymbol<B> symbol, params object[] args)
            where T : class, IIdentifiable, B
            where B : class, IIdentifiable
        {
            var op = Find<IDeleteOperation>(entity.GetType(), symbol.Operation).AssertEntity((IdentifiableEntity)(IIdentifiable)entity);
            op.Delete(entity, args);
        }

        public static void ServiceDelete(IdentifiableEntity entity, OperationSymbol operationSymbol, params object[] args)
        {
            var op = Find<IDeleteOperation>(entity.GetType(), operationSymbol).AssertEntity((IdentifiableEntity)(IIdentifiable)entity);
            op.Delete(entity, args);
        }
        #endregion

        #region Construct
        public static IdentifiableEntity ServiceConstruct(Type type, OperationSymbol operationSymbol, params object[] args)
        {
            var op = Find<IConstructOperation>(type, operationSymbol);
            return (IdentifiableEntity)op.Construct(args);
        }

        public static T Construct<T>(ConstructSymbol<T>.Simple symbol, params object[] args)
            where T : class, IIdentifiable
        {
            var op = Find<IConstructOperation>(typeof(T), symbol.Operation);
            return (T)op.Construct(args);
        }
        #endregion

        #region ConstructFrom

        public static T ConstructFrom<F, FB, T>(this F entity, ConstructSymbol<T>.From<FB> symbol, params object[] args)
            where T : class, IIdentifiable
            where FB : class, IIdentifiable
            where F : class, IIdentifiable, FB
        {
            var op = Find<IConstructorFromOperation>(entity.GetType(), symbol.Operation).AssertEntity((IdentifiableEntity)(object)entity);
            return (T)op.Construct(entity, args);
        }

        public static IdentifiableEntity ServiceConstructFrom(IIdentifiable entity, OperationSymbol operationSymbol, params object[] args)
        {
            var op = Find<IConstructorFromOperation>(entity.GetType(), operationSymbol).AssertEntity((IdentifiableEntity)(object)entity);
            return (IdentifiableEntity)op.Construct(entity, args);
        }

        public static T ConstructFromLite<F, FB, T>(this Lite<F> lite, ConstructSymbol<T>.From<FB> symbol, params object[] args)
            where T : class, IIdentifiable
            where FB : class, IIdentifiable
            where F : class, IIdentifiable, FB
        {
            var op = Find<IConstructorFromOperation>(lite.EntityType, symbol.Operation).AssertLite();
            return (T)op.Construct(Database.RetrieveAndForget(lite), args);
        }

        public static IdentifiableEntity ServiceConstructFromLite(Lite<IIdentifiable> lite, OperationSymbol operationSymbol, params object[] args)
        {
            var op = Find<IConstructorFromOperation>(lite.EntityType, operationSymbol).AssertLite();
            return (IdentifiableEntity)op.Construct(Database.RetrieveAndForget(lite), args);
        }
        #endregion

        #region ConstructFromMany
        public static IdentifiableEntity ServiceConstructFromMany(IEnumerable<Lite<IIdentifiable>> lites, Type type, OperationSymbol operationSymbol, params object[] args)
        {
            return (IdentifiableEntity)Find<IConstructorFromManyOperation>(type, operationSymbol).Construct(lites, args);
        }

        public static T ConstructFromMany<F, FB, T>(List<Lite<F>> lites, ConstructSymbol<T>.FromMany<FB> symbol, params object[] args)
            where T : class, IIdentifiable
            where FB : class, IIdentifiable
            where F : class, IIdentifiable, FB
        {
            return (T)(IIdentifiable)Find<IConstructorFromManyOperation>(typeof(F), symbol.Operation).Construct(lites.Cast<Lite<IIdentifiable>>().ToList(), args);
        }
        #endregion

        public static T Find<T>(Type type, OperationSymbol operationSymbol)
            where T : IOperation
        {
            IOperation result = FindOperation(type, operationSymbol);

            if (!(result is T))
                throw new InvalidOperationException("Operation '{0}' is a {1} not a {2} use {3} instead".Formato(operationSymbol, result.GetType().TypeName(), typeof(T).TypeName(),
                    result is IExecuteOperation ? "Execute" :
                    result is IDeleteOperation ? "Delete" :
                    result is IConstructOperation ? "Construct" :
                    result is IConstructorFromOperation ? "ConstructFrom" :
                    result is IConstructorFromManyOperation ? "ConstructFromMany" : null));

            return (T)result;
        }

        public static IOperation FindOperation(Type type, OperationSymbol operationSymbol)
        {
            IOperation result = operations.TryGetValue(type).TryGetC(operationSymbol);
            if (result == null)
                throw new InvalidOperationException("Operation '{0}' not found for type {1}".Formato(operationSymbol, type));
            return result;
        }

        public static Graph<T>.Construct FindConstruct<T>(ConstructSymbol<T>.Simple symbol) 
            where T : class, IIdentifiable
        {
            return (Graph<T>.Construct)FindOperation(typeof(T), symbol.Operation);
        }

        public static Graph<T>.ConstructFrom<F> FindConstructFrom<F, T>(ConstructSymbol<T>.From<F> symbol) 
            where T : class, IIdentifiable
            where F : class, IIdentifiable
        {
            return (Graph<T>.ConstructFrom<F>)FindOperation(typeof(F), symbol.Operation);
        }

        public static Graph<T>.ConstructFromMany<F> FindConstructFromMany<F, T>(ConstructSymbol<T>.FromMany<F> symbol)
            where T : class, IIdentifiable
            where F : class, IIdentifiable
        {
            return (Graph<T>.ConstructFromMany<F>)FindOperation(typeof(F), symbol.Operation);
        }

        public static Graph<T>.Execute FindExecute<T>(ExecuteSymbol<T> symbol)
            where T : class, IIdentifiable
        {
            return (Graph<T>.Execute)FindOperation(typeof(T), symbol.Operation);
        }

        public static Graph<T>.Delete FindDelete<T>(DeleteSymbol<T> symbol)
            where T : class, IIdentifiable
        {
            return (Graph<T>.Delete)FindOperation(typeof(T), symbol.Operation);
        }

        static T AssertLite<T>(this T result)
             where T : IEntityOperation
        {
            if (!result.Lite)
                throw new InvalidOperationException("Operation {0} is not allowed for Lites".Formato(result.OperationSymbol));

            return result;
        }

        static T AssertEntity<T>(this T result, IdentifiableEntity entity)
            where T : IEntityOperation
        {
            if (result.Lite)
            {
                var list = GraphExplorer.FromRoot(entity).Where(a => a.Modified == ModifiedState.SelfModified);
                if (list.Any())
                    throw new InvalidOperationException("Operation {0} needs a Lite or a clean entity, but the entity has changes:\r\n {1}"
                        .Formato(result.OperationSymbol, list.ToString("\r\n")));
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

        static IEnumerable<IOperation> TypeOperations(Type type)
        {
            var dic = operations.TryGetValue(type);

            if (dic == null)
                return Enumerable.Empty<IOperation>();

            return dic.Values;
        }

        public static string InState<T>(this T state, params T[] fromStates) where T : struct
        {
            if (!fromStates.Contains(state))
                return OperationMessage.StateShouldBe0InsteadOf1.NiceToString().Formato(
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
            where E : IdentifiableEntity
            where S : struct
        {
            return operations.OverridenValues.SelectMany(d => d.Values).OfType<Graph<E, S>.IGraphOperation>();
        }

        public static bool IsDefined(Type type, OperationSymbol operation)
        {
            return operations.TryGetValue(type).TryGetC(operation) != null;
        }

        public static OperationType OperationType(Type type, OperationSymbol operationSymbol)
        {
            return FindOperation(type, operationSymbol).OperationType;
        }

        public static Dictionary<OperationSymbol, string> GetContextualCanExecute(Lite<IIdentifiable>[] lites, List<OperationSymbol> operationSymbols)
        {
            Dictionary<OperationSymbol, string> result = null;
            using (ExecutionMode.Global())
            {
                foreach (var grLites in lites.GroupBy(a => a.EntityType))
                {
                    var operations = operationSymbols.Select(k => FindOperation(grLites.Key, k)).ToList();

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

        internal static GenericInvoker<Func<IEnumerable<Lite<IIdentifiable>>, IEnumerable<IOperation>, Dictionary<OperationSymbol, string>>> giGetContextualGraphCanExecute =
            new GenericInvoker<Func<IEnumerable<Lite<IIdentifiable>>, IEnumerable<IOperation>, Dictionary<OperationSymbol, string>>>((lites, operations) => GetContextualGraphCanExecute<IdentifiableEntity, IdentifiableEntity, DayOfWeek>(lites, operations));
        internal static Dictionary<OperationSymbol, string> GetContextualGraphCanExecute<T, E, S>(IEnumerable<Lite<IIdentifiable>> lites, IEnumerable<IOperation> operations)
            where E : IdentifiableEntity
            where S : struct
            where T : E
        {
            var getState = Graph<E, S>.GetState;

            var states = lites.GroupsOf(200).SelectMany(list =>
                Database.Query<T>().Where(e => list.Contains(e.ToLite())).Select(getState).Distinct()).Distinct().ToList();

            return (from o in operations.Cast<Graph<E, S>.IGraphFromStatesOperation>()
                    let invalid = states.Where(s => !o.FromStates.Contains(s)).ToList()
                    where invalid.Any()
                    select KVP.Create(o.OperationSymbol,
                        OperationMessage.StateShouldBe0InsteadOf1.NiceToString().Formato(
                        o.FromStates.CommaOr(v => ((Enum)(object)v).NiceToString()),
                        invalid.CommaOr(v => ((Enum)(object)v).NiceToString())))).ToDictionary();
        }
    }

    public interface IOperation
    {
        OperationSymbol OperationSymbol { get; }
        Type Type { get; }
        OperationType OperationType { get; }
        bool Returns { get; }
        Type ReturnType { get; }

        void AssertIsValid();
    }

    public interface IEntityOperation : IOperation
    {
        bool Lite { get; }
        bool AllowsNew { get; }
        string CanExecute(IIdentifiable entity);
        bool HasCanExecute { get; }
    }


    public delegate IDisposable SurroundOperationHandler(IOperation operation, IdentifiableEntity entity, object[] args);
    public delegate void OperationHandler(IOperation operation, IdentifiableEntity entity);
    public delegate void ErrorOperationHandler(IOperation operation, IdentifiableEntity entity, Exception ex);
    public delegate bool AllowOperationHandler(OperationSymbol operationSymbol, bool inUserInterface);


}
