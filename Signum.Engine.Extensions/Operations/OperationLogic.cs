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
                                                   Target = lo.Target,
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
                throw new UnauthorizedAccessException(Resources.Operation0IsNotAuthorized.Formato(operationKey));
        }
        #endregion


        public static void Register(this IOperation operation)
        {
            if (!typeof(IIdentifiable).IsAssignableFrom(operation.Type))
                throw new InvalidOperationException(Resources.Type0HasToImplement1AtLeast.Formato(operation.Type));

            operation.AssertIsValid(); 

            operations.GetOrCreate(operation.Type)[operation.Key] = operation;
        }

        static OperationInfo ToOperationInfo(IOperation operation, string canExecute)
        {
            return new OperationInfo
            {
                Key = operation.Key,
                Lite = (operation as IEntityOperation).TryCS(eo=>eo.Lite),
                Returns = operation.Returns,
                OperationType = operation.OperationType,
                CanExecute = canExecute,
                ReturnType = operation.ReturnType
            };
        }

        public static List<OperationInfo> ServiceGetConstructorOperationInfos(Type entityType)
        {
            return (from o in TypeOperations(entityType)
                    where o is IConstructorOperation && OperationAllowed(o.Key)
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

        #region Delete
        public static IdentifiableEntity ServiceDelete(Lite lite, Enum operationKey, params object[] args)
        {
            IdentifiableEntity entity = Database.RetrieveAndForget(lite);
            Find<IDeleteOperation>(lite.RuntimeType, operationKey).Delete(entity, args);
            return entity;
        }

        public static void Delete<T>(this Lite<T> lite, Enum operationKey, params object[] args)
            where T : class, IIdentifiable
        {
            T entity = lite.RetrieveAndForget();
            Find<IDeleteOperation>(lite.RuntimeType, operationKey).Delete(entity, args);
        }

        public static void DeleteBase<T>(this Lite<T> lite, Type baseType, Enum operationKey, params object[] args)
            where T : class, IIdentifiable
        {
            T entity = lite.RetrieveAndForget();
            Find<IDeleteOperation>(baseType, operationKey).Delete(entity, args);
        }
        #endregion

        #region Construct
        public static IIdentifiable ServiceConstruct(Type type, Enum operationKey, params object[] args)
        {
            return Find<IConstructorOperation>(type, operationKey).Construct(args);
        }

        public static T Construct<T>(Enum operationKey, params object[] args)
            where T : class, IIdentifiable
        {
            return (T)Find<IConstructorOperation>(typeof(T), operationKey).Construct(args);
        }
        #endregion

        #region ConstructFrom
        public static IIdentifiable ServiceConstructFrom(IIdentifiable entity, Enum operationKey, params object[] args)
        {
            return Find<IConstructorFromOperation>(entity.GetType(), operationKey).AssertLite(false).Construct(entity, args);
        }

        public static IIdentifiable ServiceConstructFromLite(Lite lite, Enum operationKey, params object[] args)
        {
            return Find<IConstructorFromOperation>(lite.RuntimeType, operationKey).AssertLite(true).Construct(Database.RetrieveAndForget(lite), args);
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
                throw new ApplicationException(Resources.Operation0NotFoundForType1.Formato(operationKey, type));

            if (!(result is T))
                throw new ApplicationException(Resources.Operation0IsA1NotA2Use3Instead.Formato(operationKey, result.GetType().TypeName(), typeof(T).TypeName(),
                    result is IExecuteOperation ? "Execute" :
                    result is IDeleteOperation ? "Delete" :
                    result is IConstructorOperation ? "Construct" :
                    result is IConstructorFromOperation ? "ConstructFrom" :
                    result is IConstructorFromManyOperation ? "ConstructFromMany" : null));

            return (T)result;
        }

        static T AssertLite<T>(this T result, bool isLite)
             where T : IEntityOperation
        {
            if (isLite && !result.Lite)
                throw new ApplicationException(Resources.Operation0IsNotAllowedForLites.Formato(result.Key));

            if (!isLite && result.Lite)
                throw new ApplicationException(Resources.Operation0NeedsALite.Formato(result.Key));

            return result; 
        }

        static IOperation TryFind(Type type, Enum operationKey)
        {
            if (!typeof(IIdentifiable).IsAssignableFrom(type))
                throw new ApplicationException(Resources.Type0HasToImplement1AtLeast.Formato(type, typeof(IIdentifiable)));

            IOperation result = type.FollowC(t => t.BaseType)
                .TakeWhile(t => typeof(IdentifiableEntity).IsAssignableFrom(t))
                .Select(t => operations.TryGetC(t).TryGetC(operationKey)).NotNull().FirstOrDefault();

            if (result != null)
                return result;

            List<Type> interfaces = type.GetInterfaces()
                .Where(t => typeof(IIdentifiable).IsAssignableFrom(t) && operations.TryGetC(t).TryGetC(operationKey) != null)
                .ToList();

            if (interfaces.Count > 1)
                throw new ApplicationException(Resources.AmbiguityBetweenInterfaces0.Formato(interfaces.ToString(", ")));

            if (interfaces.Count < 1)
                return null;

            return operations[interfaces.Single()][operationKey];
        }

        static List<IOperation> TypeOperations(Type type)
        {
            if (!typeof(IIdentifiable).IsAssignableFrom(type))
                throw new ApplicationException(Resources.Type0HasToImplement1AtLeast.Formato(type, typeof(IIdentifiable)));

            HashSet<Enum> result = type.FollowC(t => t.BaseType)
                    .TakeWhile(t => typeof(IdentifiableEntity).IsAssignableFrom(t))
                    .Select(t => operations.TryGetC(t)).NotNull().SelectMany(d=>d.Keys).ToHashSet();

            result.UnionWith(type.GetInterfaces()
                .Where(t => typeof(IIdentifiable).IsAssignableFrom(t))
                .Select(t => operations.TryGetC(t))
                .NotNull().SelectMany(d => d.Keys));

            return result.Select(a => TryFind(type, a)).ToList();
        }

        public static T GetArg<T>(this object[] args, int pos)
        {
            if (pos < 0)
                throw new ArgumentException("pos");

            bool acceptsNulls = typeof(T).IsByRef || Nullable.GetUnderlyingType(typeof(T)) != null;

            if (args == null || args.Length <= pos || (args[pos] == null ? !acceptsNulls : !(args[pos] is T)))
                throw new ApplicationException(Resources.TheOperationNeedsA0InTheArgumentNumber1.Formato(typeof(T), pos));

            return (T)args[pos];
        }

        public static T TryGetArgC<T>(this object[] args, int pos) where T:class
        {
            if (pos < 0)
                throw new ArgumentException("pos");

            if (args == null || args.Length <= pos || args[pos] == null)
                 return null;

            if(!(args[pos] is T))
                throw new ApplicationException(Resources.TheOperationNeedsA0InTheArgumentNumber1.Formato(typeof(T), pos));

            return (T)args[pos];
        }

        public static T? TryGetArgS<T>(this object[] args, int pos) where T : struct
        {
            if (pos < 0)
                throw new ArgumentException("pos");

            if (args == null || args.Length <= pos || args[pos] == null)
                return null;

            if (!(args[pos] is T))
                throw new ApplicationException(Resources.TheOperationNeedsA0InTheArgumentNumber1.Formato(typeof(T), pos));

            return (T)args[pos];
        }


        public static Type FindType(Enum operation)
        {
            return OperationLogic.operations.Where(o => o.Value.ContainsKey(operation)).Single(
                "Operation '{0}' not registered".Formato(operation),
                "Operation '{0}' registered for more than one element".Formato(operation)).Key;
        }
    }

    public delegate void OperationHandler(IOperation operation, IIdentifiable entity);
    public delegate void ErrorOperationHandler(IOperation operation, IIdentifiable entity, Exception ex);
    public delegate bool AllowOperationHandler(Enum operationKey);
}
