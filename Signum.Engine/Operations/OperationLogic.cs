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
using Signum.Engine.Properties;

namespace Signum.Engine.Operations
{
    public static class OperationLogic
    {
        static Expression<Func<OperationDN, IQueryable<OperationLogDN>>> LogOperationsExpression =
            o => Database.Query<OperationLogDN>().Where(a => a.Operation == o);
        public static IQueryable<OperationLogDN> LogOperations(this OperationDN o)
        {
            return LogOperationsExpression.Evaluate(o);
        }

        static Polymorphic<Dictionary<Enum, IOperation>> operations = new Polymorphic<Dictionary<Enum, IOperation>>(PolymorphicMerger.InheritDictionaryInterfaces, typeof(IIdentifiable));

        public static HashSet<Enum> RegisteredOperations
        {
            get { return operations.OverridenValues.SelectMany(a => a.Keys).ToHashSet(); }
        }


        static readonly Polymorphic<Tuple<bool>> protectedSaveTypes = new Polymorphic<Tuple<bool>>(PolymorphicMerger.InheritanceAndInterfaces, typeof(IIdentifiable));

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

            var tuple = protectedSaveTypes.TryGetValue(type);

            return tuple != null && tuple.Item1;
        }
        
        public static void SetProtectedSave<T>(bool? isProtected) where T : IIdentifiable
        {
            SetProtectedSave(typeof(T), isProtected);
        }

        public static void SetProtectedSave(Type type, bool? isProtected)
        {
            protectedSaveTypes.SetDefinition(type, isProtected == null ? null : Tuple.Create(isProtected.Value));
        }

        public static IDisposable AllowSave<T>() where T : class, IIdentifiable
        {
            return AllowSave(typeof(T));
        }

        private static IDisposable AllowSave(Type type)
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
                sb.Include<OperationDN>();
                sb.Include<OperationLogDN>();

                MultiEnumLogic<OperationDN>.Start(sb, () => RegisteredOperations);

                dqm[typeof(OperationLogDN)] = (from lo in Database.Query<OperationLogDN>()
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
                                               }).ToDynamic();

                dqm[typeof(OperationDN)] = (from lo in Database.Query<OperationDN>()
                                            select new
                                            {
                                                Entity = lo,
                                                lo.Id,
                                                lo.Name,
                                                lo.Key,
                                            }).ToDynamic();

                dqm.RegisterExpression((OperationDN o) => o.LogOperations());

                sb.Schema.EntityEventsGlobal.Saving += EntityEventsGlobal_Saving;

                sb.Schema.Table<OperationDN>().PreDeleteSqlSync += new Func<IdentifiableEntity, SqlPreCommand>(Operation_PreDeleteSqlSync);
                sb.Schema.Table<TypeDN>().PreDeleteSqlSync += new Func<IdentifiableEntity, SqlPreCommand>(Type_PreDeleteSqlSync);
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
            var f = ((FieldImplementedByAll)t.Fields["target"].Field).ColumnTypes;

            var param = Connector.Current.ParameterBuilder.CreateReferenceParameter("@id", false, arg.Id);

            return new SqlPreCommandSimple("DELETE FROM {0} WHERE {1} = {2}".Formato(t.Name, f.Name, param.ParameterName), new List<DbParameter> { param });
        }

        static void EntityEventsGlobal_Saving(IdentifiableEntity ident)
        {
            if (ident.Modified == true && 
                IsSaveProtected(ident.GetType()) && 
                !IsSaveProtectedAllowed(ident.GetType()))
                throw new InvalidOperationException("Saving '{0}' is controlled by the operations. Use OperationLogic.AllowSave<{0}>() or execute {1}".Formato(
                    ident.GetType().Name,
                    operations.GetValue(ident.GetType()).Values
                    .Where(IsExecuteNoLite)
                    .CommaOr(o => MultiEnumDN.UniqueKey(o.Key))));
        }

        #region Events

        public static event OperationHandler BeginOperation;
        public static event OperationHandler EndOperation;
        public static event ErrorOperationHandler ErrorOperation;
        public static event AllowOperationHandler AllowOperation;

        internal static void OnBeginOperation(IOperation operation, IIdentifiable entity)
        {
            if (BeginOperation != null)
                BeginOperation(operation, entity);
        }

        internal static void OnEndOperation(IOperation operation, IIdentifiable entity)
        {
            if (EndOperation != null)
                EndOperation(operation, entity);
        }

        internal static void OnErrorOperation(IOperation operation, IIdentifiable entity, Exception ex)
        {
            ex.Data["entity"] = entity;

            if (ErrorOperation != null)
                ErrorOperation(operation, entity, ex);
        }

        public static bool OperationAllowed(Enum operationKey, bool inUserInterface)
        {
            if (AllowOperation != null)
                return AllowOperation(operationKey, inUserInterface);
            else
                return true;
        }

        public static void AssertOperationAllowed(Enum operationKey, bool inUserInterface)
        {
            if (!OperationAllowed(operationKey, inUserInterface))
                throw new UnauthorizedAccessException(Resources.Operation01IsNotAuthorized.Formato(operationKey.NiceToString(), MultiEnumDN.UniqueKey(operationKey)) +
                    (inUserInterface ? " " + Resources.InUserInterface : ""));
        }
        #endregion



        public static void Register(this IOperation operation)
        {
            if (!operation.Type.IsIIdentifiable())
                throw new InvalidOperationException("Type '{0}' has to implement at least {1}".Formato(operation.Type.Name));

            operation.AssertIsValid();

            operations.GetOrAdd(operation.Type).AddOrThrow(operation.Key, operation, "Operation {0} has already been registered");

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

            operations.GetOrAdd(operation.Type)[operation.Key] = operation;
        }

        static OperationInfo ToOperationInfo(IOperation operation, string canExecute)
        {
            return new OperationInfo
            {
                Key = operation.Key,
                Lite = (operation as IEntityOperation).TryCS(eo => eo.Lite),
                Returns = operation.Returns,
                OperationType = operation.OperationType,
                CanExecute = canExecute,
                ReturnType = operation.ReturnType,
                HasStates = (operation as IGraphHasFromStatesOperation).TryCS(eo => eo.HasFromStates) ?? false,
            };
        }

        public static List<OperationInfo> ServiceGetOperationInfos(Type entityType)
        {
            return (from o in TypeOperations(entityType)
                    where OperationAllowed(o.Key, true)
                    select ToOperationInfo(o, null)).ToList();
        }

        public static List<OperationInfo> ServiceGetEntityOperationInfos(IdentifiableEntity entity)
        {
            return (from o in TypeOperations(entity.GetType())
                    let eo = o as IEntityOperation
                    where eo != null && (eo.AllowsNew || !entity.IsNew) && OperationAllowed(o.Key, true)
                    select ToOperationInfo(eo, eo.CanExecute(entity))).ToList();
        }

        public static List<OperationInfo> GetAllOperationInfos(Type entityType)
        {
            return TypeOperations(entityType).Select(o => ToOperationInfo(o, null)).ToList();
        }

        #region Execute
        public static IdentifiableEntity ServiceExecute(IIdentifiable entity, Enum operationKey, params object[] args)
        {
            var op = Find<IExecuteOperation>(entity.GetType(), operationKey).AssertEntity((IdentifiableEntity)entity);
            op.Execute(entity, args);
            return (IdentifiableEntity)entity;
        }

        public static IdentifiableEntity ServiceExecuteLite(Lite lite, Enum operationKey, params object[] args)
        {
            IdentifiableEntity entity = Database.RetrieveAndForget(lite);
            var op = Find<IExecuteOperation>(lite.RuntimeType, operationKey).AssertLite();
            op.Execute(entity, args);
            return entity;
        }

        public static T Execute<T>(this T entity, Enum operationKey, params object[] args)
           where T : class, IIdentifiable
        {
            var op = Find<IExecuteOperation>(entity.GetType(), operationKey).AssertEntity((IdentifiableEntity)(IIdentifiable)entity);
            op.Execute(entity, args);
            return (T)(IIdentifiable)entity;
        }

        public static T ExecuteLite<T>(this Lite<T> lite, Enum operationKey, params object[] args)
            where T : class, IIdentifiable
        {
            T entity = lite.RetrieveAndForget();
            var op = Find<IExecuteOperation>(lite.RuntimeType, operationKey).AssertLite();
            op.Execute(entity, args);
            return entity;
        }

        public static string CanExecute<T>(this T entity, Enum operationKey)
           where T : class, IIdentifiable
        {
            var op = Find<IEntityOperation>(entity.GetType(), operationKey);
            return op.CanExecute(entity);
        }
        #endregion

        #region Delete
        public static IdentifiableEntity ServiceDelete(Lite lite, Enum operationKey, params object[] args)
        {
            IdentifiableEntity entity = Database.RetrieveAndForget(lite);
            var op = Find<IDeleteOperation>(lite.RuntimeType, operationKey);
            op.Delete(entity, args);
            return entity;
        }

        public static void Delete<T>(this Lite<T> lite, Enum operationKey, params object[] args)
            where T : class, IIdentifiable
        {
            T entity = lite.RetrieveAndForget();
            var op = Find<IDeleteOperation>(lite.RuntimeType, operationKey);
            op.Delete(entity, args);
        }

        public static void Delete<T>(this T entity, Enum operationKey, params object[] args)
            where T : class, IIdentifiable
        {
            var op = Find<IDeleteOperation>(entity.GetType(), operationKey).AssertEntity((IdentifiableEntity)(IIdentifiable)entity);
            op.Delete(entity, args);
        }
        #endregion

        #region Construct
        public static IdentifiableEntity ServiceConstruct(Type type, Enum operationKey, params object[] args)
        {
            var op = Find<IConstructOperation>(type, operationKey);
            return (IdentifiableEntity)op.Construct(args);
        }

        public static T Construct<T>(Enum operationKey, params object[] args)
            where T : class, IIdentifiable
        {
            var op = Find<IConstructOperation>(typeof(T), operationKey);
            return (T)op.Construct(args);
        }
        #endregion

        #region ConstructFrom
        public static IdentifiableEntity ServiceConstructFrom(IIdentifiable entity, Enum operationKey, params object[] args)
        {
            var op = Find<IConstructorFromOperation>(entity.GetType(), operationKey).AssertEntity((IdentifiableEntity)entity);
            return (IdentifiableEntity)op.Construct(entity, args);
        }

        public static IdentifiableEntity ServiceConstructFromLite(Lite lite, Enum operationKey, params object[] args)
        {
            var op = Find<IConstructorFromOperation>(lite.RuntimeType, operationKey).AssertLite();
            return (IdentifiableEntity)op.Construct(Database.RetrieveAndForget(lite), args);
        }

        public static T ConstructFrom<T>(this IIdentifiable entity, Enum operationKey, params object[] args)
              where T : class, IIdentifiable
        {
            var op = Find<IConstructorFromOperation>(entity.GetType(), operationKey).AssertEntity((IdentifiableEntity)entity);
            return (T)op.Construct(entity, args);
        }

        public static T ConstructFromLite<T>(this Lite lite, Enum operationKey, params object[] args)
           where T : class, IIdentifiable
        {
            var op = Find<IConstructorFromOperation>(lite.RuntimeType, operationKey).AssertLite();
            return (T)op.Construct(Database.RetrieveAndForget(lite), args);
        }
        #endregion

        #region ConstructFromMany
        public static IdentifiableEntity ServiceConstructFromMany(List<Lite> lites, Type type, Enum operationKey, params object[] args)
        {
            return (IdentifiableEntity)Find<IConstructorFromManyOperation>(type, operationKey).Construct(lites, args);
        }

        public static T ConstructFromMany<F, T>(List<Lite<F>> lites, Enum operationKey, params object[] args)
            where T : class, IIdentifiable
            where F : class, IIdentifiable
        {
            return (T)(IIdentifiable)Find<IConstructorFromManyOperation>(typeof(F), operationKey).Construct(lites.Cast<Lite>().ToList(), args);
        }
        #endregion

        public static T Find<T>(Type type, Enum operationKey)
            where T : IOperation
        {
            IOperation result = FindOperation(type, operationKey);

            if (!(result is T))
                throw new InvalidOperationException("Operation {0} is a {1} not a {2} use {3} instead".Formato(operationKey, result.GetType().TypeName(), typeof(T).TypeName(),
                    result is IExecuteOperation ? "Execute" :
                    result is IDeleteOperation ? "Delete" :
                    result is IConstructOperation ? "Construct" :
                    result is IConstructorFromOperation ? "ConstructFrom" :
                    result is IConstructorFromManyOperation ? "ConstructFromMany" : null));

            return (T)result;
        }

        private static IOperation FindOperation(Type type, Enum operationKey)
        {
            IOperation result = operations.TryGetValue(type).TryGetC(operationKey);
            if (result == null)
                throw new InvalidOperationException("Operation {0} not found for type {1}".Formato(operationKey, type));
            return result;
        }

        static T AssertLite<T>(this T result)
             where T : IEntityOperation
        {
            if (!result.Lite)
                throw new InvalidOperationException("Operation {0} is not allowed for Lites".Formato(result.Key));

            return result;
        }

        static T AssertEntity<T>(this T result, IdentifiableEntity entity)
            where T : IEntityOperation
        {
            if (result.Lite)
            {
                var list = GraphExplorer.FromRoot(entity).Where(a => a.SelfModified);
                if (list.Any())
                    throw new InvalidOperationException("Operation {0} needs a Lite or a clean entity, but the entity has changes:\r\n {1}".Formato(result.Key, list.ToString("\r\n")));
            }

            return result;
        }

        public static bool IsLite(Enum operationKey)
        {
            return operations.OverridenTypes.Select(t => operations.GetDefinition(t)).NotNull()
                .Select(d => d.TryGetC(operationKey)).NotNull().OfType<IEntityOperation>().Select(a => a.Lite).FirstOrDefault();
        }


        static IEnumerable<IOperation> TypeOperations(Type type)
        {
            var dic = operations.TryGetValue(type);

            if (dic == null)
                return Enumerable.Empty<IOperation>();

            return dic.Values;
        }

        public static T GetArg<T>(this object[] args)
        {
            return args.OfTypeOrEmpty<T>().SingleEx(
                () => "The operation needs a {0} in the arguments".Formato(typeof(T)),
                () => "There are more than one {0} in the arguments in the argument list".Formato(typeof(T)));
        }

        public static T TryGetArgC<T>(this object[] args) where T : class
        {
            return args.OfTypeOrEmpty<T>().SingleOrDefaultEx(
                () => "There are more than one {0} in the arguments in the argument list".Formato(typeof(T)));
        }

        public static T? TryGetArgS<T>(this object[] args) where T : struct
        {
            var casted = args.OfTypeOrEmpty<T>();

            if (casted.IsEmpty())
                return null;

            return casted.SingleEx(() => "There are more than one {0} in the arguments in the argument list".Formato(typeof(T)));
        }

        static IEnumerable<T> OfTypeOrEmpty<T>(this object[] args)
        {
            if (args == null)
                return Enumerable.Empty<T>();

            return args.OfType<T>(); 
        }

        public static string InState<T>(this T state, Enum operationKey, params T[] validStates) where T : struct
        {
            if (validStates.Contains(state))
                return null;

            return Resources.ImpossibleToExecute0FromState1.Formato(operationKey.NiceToString(), ((Enum)(object)state).NiceToString());
        }

        public static Type[] FindTypes(Enum operation)
        {
            return TypeLogic.DnToType.Values.Where(t => operations.TryGetValue(t).TryGetC(operation) != null).ToArray();
        }

        internal static IEnumerable<Graph<E, S>.IGraphOperation> GraphOperations<E, S>()
            where E : IdentifiableEntity
            where S : struct
        {
            return operations.OverridenValues.SelectMany(d => d.Values).OfType<Graph<E, S>.IGraphOperation>();
        }

        public static bool IsDefined(Type type, Enum operation)
        {
            return operations.TryGetValue(type).TryGetC(operation) != null;
        }

        public static OperationType OperationType(Type type, Enum operationKey)
        {
            return FindOperation(type, operationKey).OperationType;
        }

        public static Dictionary<Enum, string> GetContextualCanExecute(Lite[] lites, List<Enum> cleanKeys)
        {
            Dictionary<Enum, string> result = null;
            using (ExecutionMode.Global())
            {
                foreach (var grLites in lites.GroupBy(a => a.RuntimeType))
                {
                    var operations = cleanKeys.Select(k => FindOperation(grLites.Key, k)).ToList();

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

        internal static GenericInvoker<Func<IEnumerable<Lite>, IEnumerable<IOperation>, Dictionary<Enum, string>>> giGetContextualGraphCanExecute =
            new GenericInvoker<Func<IEnumerable<Lite>, IEnumerable<IOperation>, Dictionary<Enum, string>>>((lites, operations) => GetContextualGraphCanExecute<IdentifiableEntity, IdentifiableEntity, DayOfWeek>(lites, operations));
        internal static Dictionary<Enum, string> GetContextualGraphCanExecute<T, E, S>(IEnumerable<Lite> lites, IEnumerable<IOperation> operations)
            where E : IdentifiableEntity
            where S : struct
            where T : E
        {
            var getState = Graph<E, S>.GetState;

            var states = lites.GroupsOf(200).SelectMany(list =>
                Database.Query<T>().Where(e => list.Contains(e.ToLite())).Select(getState).Distinct()).Distinct().ToList();

            return (from o in operations.Cast<Graph<E, S>.IGraphFromStatesOperation>()
                    let list = states.Where(s => !o.FromStates.Contains(s)).ToList()
                    where list.Any()
                    select KVP.Create(o.Key,
                        Resources.ImpossibleToExecute0FromState1.Formato(o.Key.NiceToString(), list.CommaOr(s => ((Enum)(object)s).NiceToString())))).ToDictionary();
        }
    }

    public interface IOperation
    {
        Enum Key { get; }
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
    }


    public delegate void OperationHandler(IOperation operation, IIdentifiable entity);
    public delegate void ErrorOperationHandler(IOperation operation, IIdentifiable entity, Exception ex);
    public delegate bool AllowOperationHandler(Enum operationKey, bool inUserInterface);


}
