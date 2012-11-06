using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Entities.Operations;
using Signum.Entities;
using Signum.Utilities.DataStructures;
using System.Reflection;
using Signum.Engine.Maps;
using Signum.Entities.Authorization;
using System.Threading;
using Signum.Engine.Authorization;
using Signum.Utilities.Reflection;
using Signum.Utilities.ExpressionTrees;
using Signum.Engine.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Extensions.Properties;
using Signum.Entities.Reflection;
using System.Linq.Expressions;
using Signum.Entities.Basics;

namespace Signum.Engine.Operations
{
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


        public static readonly HashSet<Type> ProtectedSaveTypes = new HashSet<Type>();

        static readonly Variable<ImmutableStack<Type>> allowedTypes = Statics.ThreadVariable<ImmutableStack<Type>>("saveOperationsAllowedTypes");
        public static bool AllowSaveGlobally { get; set; }

        public static bool IsSaveProtected(Type type)
        {
            if (AllowSaveGlobally)
                return false;

            var it = allowedTypes.Value;

            return ProtectedSaveTypes.Contains(type) && (it == null || !it.Contains(type));
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

                EnumLogic<OperationDN>.Start(sb, () => RegisteredOperations);

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
            }
        }

        static void EntityEventsGlobal_Saving(IdentifiableEntity ident)
        {
            if (ident.Modified == true && IsSaveProtected(ident.GetType()))
                throw new InvalidOperationException("Saving '{0}' is controlled by the operations. Use OperationLogic.AllowSave() or execute {1}".Formato(
                    ident.GetType().Name,
                    operations.GetValue(ident.GetType()).Keys.CommaOr(k => EnumDN.UniqueKey(k))));
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

        public static bool OperationAllowed(Enum operationKey)
        {
            if (AllowOperation != null)
                return AllowOperation(operationKey);
            else
                return true;
        }

        public static void AssertOperationAllowed(Enum operationKey)
        {
            if (!OperationAllowed(operationKey))
                throw new UnauthorizedAccessException(Resources.Operation01IsNotAuthorized.Formato(operationKey.NiceToString(), EnumDN.UniqueKey(operationKey)));
        }
        #endregion



        public static void Register(this IOperation operation)
        {
            if (!operation.Type.IsIIdentifiable())
                throw new InvalidOperationException("Type '{0}' has to implement at least {1}".Formato(operation.Type.Name));

            operation.AssertIsValid();

            operations.GetOrAdd(operation.Type).AddOrThrow(operation.Key, operation, "Operation {0} has already been registered");

            if (operation is IExecuteOperation && ((IEntityOperation)operation).Lite == false)
            {
                ProtectedSaveTypes.Add(operation.Type);
            }
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
                ReturnType = operation.ReturnType
            };
        }

        public static List<OperationInfo> ServiceGetConstructorOperationInfos(Type entityType)
        {
            return (from o in TypeOperations(entityType)
                    where o is IConstructOperation && OperationAllowed(o.Key)
                    select ToOperationInfo(o, null)).ToList();
        }

        public static List<OperationInfo> ServiceGetQueryOperationInfos(Type entityType)
        {
            return (from o in TypeOperations(entityType)
                    where o is IConstructorFromManyOperation && OperationAllowed(o.Key)
                    select ToOperationInfo(o, null)).ToList();
        }

        public static List<OperationInfo> ServiceGetEntityOperationInfos(IdentifiableEntity entity)
        {
            return (from o in TypeOperations(entity.GetType())
                    let eo = o as IEntityOperation
                    where eo != null && (eo.AllowsNew || !entity.IsNew) && OperationAllowed(o.Key)
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
        public static IIdentifiable ServiceConstruct(Type type, Enum operationKey, params object[] args)
        {
            var op = Find<IConstructOperation>(type, operationKey);
            return op.Construct(args);
        }

        public static T Construct<T>(Enum operationKey, params object[] args)
            where T : class, IIdentifiable
        {
            var op = Find<IConstructOperation>(typeof(T), operationKey);
            return (T)op.Construct(args);
        }
        #endregion

        #region ConstructFrom
        public static IIdentifiable ServiceConstructFrom(IIdentifiable entity, Enum operationKey, params object[] args)
        {
            var op = Find<IConstructorFromOperation>(entity.GetType(), operationKey).AssertEntity((IdentifiableEntity)entity);
            return op.Construct(entity, args);
        }

        public static IIdentifiable ServiceConstructFromLite(Lite lite, Enum operationKey, params object[] args)
        {
            var op = Find<IConstructorFromOperation>(lite.RuntimeType, operationKey).AssertLite();
            return op.Construct(Database.RetrieveAndForget(lite), args);
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
            IOperation result = operations.TryGetValue(type).TryGetC(operationKey);
            if (result == null)
                throw new InvalidOperationException("Operation {0} not found for type {1}".Formato(operationKey, type));

            if (!(result is T))
                throw new InvalidOperationException("Operation {0} is a {1} not a {2} use {3} instead".Formato(operationKey, result.GetType().TypeName(), typeof(T).TypeName(),
                    result is IExecuteOperation ? "Execute" :
                    result is IDeleteOperation ? "Delete" :
                    result is IConstructOperation ? "Construct" :
                    result is IConstructorFromOperation ? "ConstructFrom" :
                    result is IConstructorFromManyOperation ? "ConstructFromMany" : null));

            return (T)result;
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

        public static T GetArg<T>(this object[] args, int pos)
        {
            if (pos < 0)
                throw new ArgumentException("pos");

            bool acceptsNulls = typeof(T).IsByRef || Nullable.GetUnderlyingType(typeof(T)) != null;

            if (args == null || args.Length <= pos || (args[pos] == null ? !acceptsNulls : !(args[pos] is T)))
                throw new ArgumentException("The operation needs a {0} in the argument {1}".Formato(typeof(T), pos));

            return (T)args[pos];
        }

        public static T TryGetArgC<T>(this object[] args, int pos) where T : class
        {
            if (pos < 0)
                throw new ArgumentException("pos");

            if (args == null || args.Length <= pos || args[pos] == null)
                return null;

            if (!(args[pos] is T))
                throw new ArgumentException("The operation needs a {0} in the argument {1}".Formato(typeof(T), pos));

            return (T)args[pos];
        }

        public static T? TryGetArgS<T>(this object[] args, int pos) where T : struct
        {
            if (pos < 0)
                throw new ArgumentException("pos");

            if (args == null || args.Length <= pos || args[pos] == null)
                return null;

            if (!(args[pos] is T))
                throw new ApplicationException("The operation needs a {0} in the argument {1}".Formato(typeof(T), pos));

            return (T)args[pos];
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


    }

    public delegate void OperationHandler(IOperation operation, IIdentifiable entity);
    public delegate void ErrorOperationHandler(IOperation operation, IIdentifiable entity, Exception ex);
    public delegate bool AllowOperationHandler(Enum operationKey);


}
