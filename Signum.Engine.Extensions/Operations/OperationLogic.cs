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
        bool Returns { get; }

        void AssertIsValid();
    }

    public interface IEntityOperation : IOperation
    {
        bool Lite { get; }
        bool AllowsNew { get; }
        bool CanExecute(IIdentifiable entity); 
    }

    public static class OperationLogic
    {
        static Dictionary<Type, Dictionary<Enum, IOperation>> operations = new Dictionary<Type, Dictionary<Enum, IOperation>>();

        public static void AssertIsStarted(SchemaBuilder sb)
        {
            sb.AssertDefined(ReflectionTools.GetMethodInfo(()=>Start(null,null))); 
        }

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<OperationDN>();
                sb.Include<LogOperationDN>();

                EnumLogic<OperationDN>.Start(sb, () => operations.Values.SelectMany(b => b.Keys).ToHashSet());

                dqm[typeof(LogOperationDN)] = (from lo in Database.Query<LogOperationDN>()
                                               select new
                                               {
                                                   Entity = lo.ToLite(),
                                                   lo.Id,
                                                   Target_nf_ = lo.Target,
                                                   Operation = lo.Operation.ToLite(),
                                                   User = lo.User.ToLite(),
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
                Lite = (operation as IEntityOperation).TryCS(eo=>eo.Lite),
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
            Find<IExecuteOperation>(entity.GetType(), operationKey).AssertLite(false).Execute(entity, args);
            return (IdentifiableEntity)entity;
        }

        public static IdentifiableEntity ServiceExecuteLite(Lite lite, Enum operationKey, params object[] args)
        {
            IdentifiableEntity entity = Database.RetrieveAndForget(lite);
            Find<IExecuteOperation>(lite.RuntimeType, operationKey).AssertLite(true).Execute(entity, args);
            return entity;
        }

        public static T Execute<T>(this T entity, Enum operationKey, params object[] args)
           where T : class, IIdentifiable
        {
            Find<IExecuteOperation>(entity.GetType(), operationKey).AssertLite(false).Execute(entity, args);
            return (T)(IIdentifiable)entity;
        }

        public static T ExecuteLite<T>(this Lite<T> lite, Enum operationKey, params object[] args)
            where T : class, IIdentifiable
        {
            T  entity = lite.RetrieveAndForget();
            Find<IExecuteOperation>(lite.RuntimeType, operationKey).AssertLite(true).Execute(entity, args);
            return entity;
        }

        public static T ExecuteBase<T>(this T entity, Type baseType, Enum operationKey, params object[] args)
             where T : class, IIdentifiable
        {
            Find<IExecuteOperation>(baseType, operationKey).AssertLite(false).Execute(entity, args);
            return entity;
        }

        public static T ExecuteLiteBase<T>(this Lite<T> lite, Type baseType, Enum operationKey, params object[] args)
            where T:class, IIdentifiable
        {
            T entity = lite.RetrieveAndForget();
            Find<IExecuteOperation>(baseType, operationKey).AssertLite(true).Execute(entity, args);
            return entity;
        }
        #endregion

        #region Construct
        public static IdentifiableEntity ServiceConstruct(Type type, Enum operationKey, params object[] args)
        {
            return Find<IConstructorOperation>(type, operationKey).Construct(args);
        }

        public static T Construct<T>(Enum operationKey, params object[] args)
            where T : class, IIdentifiable
        {
            return (T)(IIdentifiable)Find<IConstructorOperation>(typeof(T), operationKey).Construct(args);
        }
        #endregion

        #region ConstructFrom
        public static IdentifiableEntity ServiceConstructFrom(IIdentifiable entity, Enum operationKey, params object[] args)
        {
            return (IdentifiableEntity)Find<IConstructorFromOperation>(entity.GetType(), operationKey).AssertLite(false).Construct(entity, args);
        }

        public static IdentifiableEntity ServiceConstructFromLite(Lite lite, Enum operationKey, params object[] args)
        {
            return (IdentifiableEntity)Find<IConstructorFromOperation>(lite.RuntimeType, operationKey).AssertLite(true).Construct(Database.RetrieveAndForget(lite), args);
        }


        public static T ConstructFrom<T>(this IIdentifiable entity, Enum operationKey, params object[] args)
              where T : class, IIdentifiable
        {
            return (T)Find<IConstructorFromOperation>(entity.GetType(), operationKey).AssertLite(false).Construct(entity, args);
        }

        public static T ConstructFromLite<T>(this Lite lite, Enum operationKey, params object[] args)
           where T : class, IIdentifiable
        {
            return (T)Find<IConstructorFromOperation>(lite.RuntimeType, operationKey).AssertLite(true).Construct(Database.RetrieveAndForget(lite), args);
        }


        public static T ConstructFromBase<T>(this IIdentifiable entity, Type baseType, Enum operationKey, params object[] args)
            where T : class, IIdentifiable
        {
            return (T)Find<IConstructorFromOperation>(baseType, operationKey).AssertLite(false).Construct(entity, args);
        }

        public static T ConstructFromLiteBase<T>(this Lite lite, Type baseType, Enum operationKey, params object[] args)
               where T : class, IIdentifiable
        {
            return (T)Find<IConstructorFromOperation>(baseType, operationKey).AssertLite(true).Construct(Database.RetrieveAndForget(lite), args);
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

        static T Find<T>(Type type, Enum operationKey)
            where T : IOperation
        {
            IOperation result = TryFind(type, operationKey);
            if (result == null)
                throw new ApplicationException("Operation {0} not found for Type {1}".Formato(operationKey, type));

            if (!(result is T))
                throw new ApplicationException("Operation {0} is a {1} not a {2}, use '{3}' instead".Formato(operationKey, result.GetType().TypeName(), typeof(T).TypeName(),
                    result is IExecuteOperation ? "Execute" :
                    result is IConstructorOperation ? "Construct" :
                    result is IConstructorFromOperation ? "ConstructFrom" :
                    result is IConstructorFromManyOperation ? "ConstructFromMany" : null));

            return (T)result;
        }

        static T AssertLite<T>(this T result, bool isLite)
             where T : IEntityOperation
        {
            if (isLite && !result.Lite)
                throw new ApplicationException("Operation {0} is not allowed for lites".Formato(result.Key));

            if (!isLite && result.Lite)
                throw new ApplicationException("Operation {0} needs a Lite".Formato(result.Key));

            return result; 
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
