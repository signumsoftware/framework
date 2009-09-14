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

namespace Signum.Engine.Operations
{
    public interface IOperation
    {
        Enum Key { get; }
        Type Type { get; }
        OperationType OperationType { get; }
        bool Lazy { get; }
        bool Returns { get; }

        void AssertIsValid();
    }

    public interface IEntityOperation : IOperation
    {
        bool AllowsNew { get; }
        bool CanExecute(IIdentifiable entity); 
    }

    public static class OperationLogic
    {
        static Dictionary<Type, Dictionary<Enum, IOperation>> operations = new Dictionary<Type, Dictionary<Enum, IOperation>>();

        public static void AssertIsStarted(SchemaBuilder sb)
        {
            if (!sb.ContainsDefinition<OperationDN>())
                throw new ApplicationException("Call OperationLogic.Start first"); 
        }

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined<OperationDN>())
            {
                sb.Include<OperationDN>();
                sb.Include<LogOperationDN>();

                EnumLogic<OperationDN>.Start(sb, () => operations.Values.SelectMany(b => b.Keys).ToHashSet());

                dqm[typeof(LogOperationDN)] = (from lo in Database.Query<LogOperationDN>()
                                               select new
                                               {
                                                   Entity = lo.ToLazy(),
                                                   lo.Id,
                                                   lo.Target,
                                                   Operation = lo.Operation.ToLazy(),
                                                   User = lo.User.ToLazy(),
                                                   lo.Start,
                                                   lo.End,
                                                   lo.Exception
                                               }).ToDynamic();
            }
        }

        #region Events
        public static event OperationHandler BeginOperation;
        public static event OperationHandler EndOperation;
        public static event ErrorOperationHandler ErrorOperation;
        public static event AllowOperationHandler AllowOperation; 

        internal static void OnBeginOperation(IOperation operation, IdentifiableEntity entity)
        {
            if (BeginOperation != null)
                BeginOperation(operation, entity);
        }

        internal static void OnEndOperation(IOperation operation, IdentifiableEntity entity)
        {
            if (EndOperation != null)
                EndOperation(operation, entity);
        }

        internal static void OnErrorOperation(IOperation operation, IdentifiableEntity entity, Exception ex)
        {
            if (ErrorOperation != null)
                ErrorOperation(operation, entity, ex);
        }

        internal static bool OnAllowOperation(Enum operationKey)
        {
            if (AllowOperation != null)
                return AllowOperation(operationKey);
            else 
                return true; 
        }
        #endregion


        public static void Register(IOperation operation)
        {
            operation.AssertIsValid(); 

            operations.GetOrCreate(operation.Type)[operation.Key] = operation;
        }

        static OperationInfo ToOperationInfo(IOperation operation, bool canExecute)
        {
            return new OperationInfo
            {
                Key = operation.Key,
                Lazy = operation.Lazy,
                Returns = operation.Returns,
                OperationType = operation.OperationType,
                CanExecute = canExecute,
            };
        }

        public static List<OperationInfo> ServiceGetConstructorOperationInfos(Type entityType)
        {
            return (from k in EnumLogic<OperationDN>.Keys
                    let o = TryFind(entityType, k) as IConstructorOperation
                    where o != null && OnAllowOperation(k)
                    select ToOperationInfo(o, true)).ToList();
        }

        public static List<OperationInfo> ServiceGetQueryOperationInfos(Type entityType)
        {
            return (from k in EnumLogic<OperationDN>.Keys
                    let cfm = TryFind(entityType, k) as IConstructorFromManyOperation
                    where cfm != null && OnAllowOperation(k)
                    select ToOperationInfo(cfm, true)).ToList();
        }

        public static List<OperationInfo> ServiceGetEntityOperationInfos(IdentifiableEntity entity)
        {
            return (from k in EnumLogic<OperationDN>.Keys
                    let eo = TryFind(entity.GetType(), k) as IEntityOperation
                    where eo != null && (eo.AllowsNew || !entity.IsNew) && OnAllowOperation(k)
                    select ToOperationInfo(eo, eo.CanExecute(entity))).ToList();
        }

        #region Execute
        public static IdentifiableEntity ServiceExecute(IIdentifiable entity, Enum operationKey, params object[] args)
        {
            Find<IExecuteOperation>(entity.GetType(), operationKey, false).Execute(entity, args);
            return (IdentifiableEntity)entity;
        }

        public static IdentifiableEntity ServiceExecuteLazy(Lazy lazy, Enum operationKey, params object[] args)
        {
            IdentifiableEntity entity = Database.RetrieveAndForget(lazy);
            Find<IExecuteOperation>(lazy.RuntimeType, operationKey, true).Execute(entity, args);
            return entity;
        }

        public static T Execute<T>(this T entity, Enum operationKey, params object[] args)
           where T : class, IIdentifiable
        {
            Find<IExecuteOperation>(entity.GetType(), operationKey, false).Execute(entity, args);
            return (T)(IIdentifiable)entity;
        }

        public static T ExecuteLazy<T>(this Lazy<T> lazy, Enum operationKey, params object[] args)
            where T : class, IIdentifiable
        {
            T  entity = lazy.RetrieveAndForget(); 
            Find<IExecuteOperation>(lazy.RuntimeType, operationKey, true).Execute(entity, args);
            return entity;
        }

        public static T ExecuteBase<T>(this T entity, Type baseType, Enum operationKey, params object[] args)
             where T : class, IIdentifiable
        {
             Find<IExecuteOperation>(baseType, operationKey, false).Execute(entity, args);
             return entity;
        }

        public static T ExecuteLazyBase<T>(this Lazy<T> lazy, Type baseType, Enum operationKey, params object[] args)
            where T:class, IIdentifiable
        {
            T entity = lazy.RetrieveAndForget(); 
            Find<IExecuteOperation>(baseType, operationKey, true).Execute(entity, args);
            return entity;
        }
        #endregion

        #region Construct
        public static IdentifiableEntity ServiceConstruct(Type type, Enum operationKey, params object[] args)
        {
            return Find<IConstructorOperation>(type, operationKey, false).Construct(args);
        }

        public static T Construct<T>(Enum operationKey, params object[] args)
            where T : class, IIdentifiable
        {
            return (T)(IIdentifiable)Find<IConstructorOperation>(typeof(T), operationKey, false).Construct(args);
        }
        #endregion

        #region ConstructFrom
        public static IdentifiableEntity ServiceConstructFrom(IIdentifiable entity, Enum operationKey, params object[] args)
        {
            return (IdentifiableEntity)Find<IConstructorFromOperation>(entity.GetType(), operationKey, false).Construct(entity, args);
        }

        public static IdentifiableEntity ServiceConstructFromLazy(Lazy lazy, Enum operationKey, params object[] args)
        {
            return (IdentifiableEntity)Find<IConstructorFromOperation>(lazy.RuntimeType, operationKey, true).Construct(Database.RetrieveAndForget(lazy), args);
        }


        public static T ConstructFrom<T>(this IIdentifiable entity, Enum operationKey, params object[] args)
              where T : class, IIdentifiable
        {
            return (T)Find<IConstructorFromOperation>(entity.GetType(), operationKey, false).Construct(entity, args);
        }

        public static T ConstructFromLazy<T>(this Lazy lazy, Enum operationKey, params object[] args)
           where T : class, IIdentifiable
        {
            return (T)Find<IConstructorFromOperation>(lazy.RuntimeType, operationKey, true).Construct(Database.RetrieveAndForget(lazy), args);
        }


        public static T ConstructFromBase<T>(this IIdentifiable entity, Type baseType, Enum operationKey, params object[] args)
            where T : class, IIdentifiable
        {
            return (T)Find<IConstructorFromOperation>(baseType, operationKey, false).Construct(entity, args);
        }

        public static T ConstructFromLazyBase<T>(this Lazy lazy, Type baseType, Enum operationKey, params object[] args)
               where T : class, IIdentifiable
        {
            return (T)Find<IConstructorFromOperation>(baseType, operationKey, true).Construct(Database.RetrieveAndForget(lazy), args);
        }
        #endregion

        #region ConstructFromMany
        public static IdentifiableEntity ServiceConstructFromMany(List<Lazy> lazies, Type type, Enum operationKey, params object[] args)
        {
            return (IdentifiableEntity)Find<IConstructorFromManyOperation>(type, operationKey, true).Construct(lazies, args);
        }

        public static T ConstructFromMany<F, T>(List<Lazy<F>> lazies, Enum operationKey, params object[] args)
            where T : class, IIdentifiable
            where F : class, IIdentifiable
        {
            return (T)(IIdentifiable)Find<IConstructorFromManyOperation>(typeof(F), operationKey, true).Construct(lazies.Cast<Lazy>().ToList(), args);
        }
        #endregion

        static T Find<T>(Type type, Enum operationKey, bool isLazy)
            where T : IOperation
        {
            IOperation result = TryFind(type, operationKey);
            if (result == null)
                throw new ApplicationException("Operation {0} not found for Type {1}".Formato(operationKey, type));

            if (isLazy && !result.Lazy)
                throw new ApplicationException("Operation {0} is not allowed for lazies".Formato(operationKey));

            if (!isLazy && result.Lazy)
                throw new ApplicationException("Operation {0} needs a Lazy".Formato(operationKey));

            if (!(result is T))
                throw new ApplicationException("Operation {0} is a {1} not a {2}, use '{3}' instead".Formato(operationKey, result.GetType().TypeName(), typeof(T).TypeName(),
                    result is IExecuteOperation ? "Execute" :
                    result is IConstructorOperation ? "Construct" :
                    result is IConstructorFromOperation ? "ConstructFrom" :
                    result is IConstructorFromManyOperation ? "ConstructFromMany" : null));

            return (T)result;
        }

        static IOperation TryFind(Type type, Enum operationKey)
        {
            if (!typeof(IIdentifiable).IsAssignableFrom(type))
                throw new ApplicationException("type is a {0} but to implement {1} at least".Formato(type, typeof(IIdentifiable)));

            IOperation result = type.FollowC(t => t.BaseType)
                .TakeWhile(t => typeof(IdentifiableEntity).IsAssignableFrom(t))
                .Select(t => operations.TryGetC(t).TryGetC(operationKey)).FirstOrDefault();

            if (result != null)
                return result;

            List<Type> interfaces = type.GetInterfaces()
                .Where(t => typeof(IdentifiableEntity).IsAssignableFrom(t) && operations.TryGetC(t).TryGetC(operationKey) != null)
                .ToList();

            if (interfaces.Count > 1)
                throw new ApplicationException("Ambiguity between interfaces: {0}".Formato(interfaces.ToString(", ")));

            if (interfaces.Count < 1)
                return null;

            return operations[interfaces.Single()][operationKey];
        }

        public static T GetArg<T>(this object[] args, int pos)
        {
            if (pos < 0)
                throw new ArgumentException("pos");

            if (args == null || args.Length <= pos || !(args[pos] is T))
                throw new ApplicationException("The operation needs a {0} in the argument number {1}".Formato(typeof(T), pos));

            return (T)args[pos];
        }
    }

    public delegate void OperationHandler(IOperation operation, IdentifiableEntity entity);
    public delegate void ErrorOperationHandler(IOperation operation, IdentifiableEntity entity, Exception ex);
    public delegate bool AllowOperationHandler(Enum operationKey);
}
