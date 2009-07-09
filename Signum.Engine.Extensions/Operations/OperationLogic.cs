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

        internal static HashSet<Enum> OperationKeys; 
        internal static Dictionary<Enum, OperationDN> ToOperation;
        internal static Dictionary<string, Enum> ToEnum;

        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined<OperationDN>())
            {
                sb.Include<OperationDN>();
                sb.Include<LogOperationDN>();

                sb.Schema.Initializing += Schema_Initializing;
                sb.Schema.Synchronizing += Schema_Synchronizing;
                sb.Schema.Generating += Schema_Generating;
            }
        }

        static void Schema_Initializing(Schema sender)
        {
            using (AuthLogic.Disable())
            using (new EntityCache(true))
            {
                OperationKeys = operations.Values.SelectMany(b=>b.Keys).ToHashSet();

                ToOperation = EnumerableExtensions.JoinStrict(
                     Database.RetrieveAll<OperationDN>(),
                     OperationKeys,
                     a => a.Key,
                     k => OperationDN.UniqueKey(k),
                     (a, k) => new { a, k }, "Caching ActionDN").ToDictionary(p => p.k, p => p.a);

                ToEnum = ToOperation.Keys.ToDictionary(k => OperationDN.UniqueKey(k));
            }
        }

        static SqlPreCommand Schema_Generating()
        {
            Table table = Schema.Current.Table<OperationDN>();

            return GetOperations().Select(a => table.InsertSqlSync(a)).Combine(Spacing.Simple);
        }

        const string OperationsKey = "Operations";
        static SqlPreCommand Schema_Synchronizing(Replacements replacements)
        {
            Table table = Schema.Current.Table<OperationDN>();

            List<OperationDN> current = Administrator.TryRetrieveAll<OperationDN>(replacements);

            return Synchronizer.SyncronizeReplacing(replacements, OperationsKey,
                current.ToDictionary(c => c.Key),
                GetOperations().ToDictionary(s => s.Key),
                (k, c) => table.DeleteSqlSync(c),
                (k, s) => table.InsertSqlSync(s),
                (k, c, s) =>
                {
                    c.Name = s.Name;
                    c.Key = s.Key;
                    return table.UpdateSqlSync(c);
                }, Spacing.Double);
        }

        #region Events
        public static event OperationHandler BeginOperation;
        public static event OperationHandler EndOperation;
        public static event ErrorOperationHandler ErrorOperation;
        public static event AllowOperationHandler AllowOperation; 

        private static void OnBeginOperation(IOperation operation, IdentifiableEntity entity)
        {
            if (BeginOperation != null)
                BeginOperation(operation, entity);
        }

        private static void OnEndOperation(IOperation operation, IdentifiableEntity entity)
        {
            if (EndOperation != null)
                EndOperation(operation, entity);
        }

        private static void OnErrorOperation(IOperation operation, IdentifiableEntity entity, Exception ex)
        {
            if (ErrorOperation != null)
                ErrorOperation(operation, entity, ex);
        }

        private static bool OnAllowOperation(Enum operationKey)
        {
            if (AllowOperation != null)
                return AllowOperation(operationKey);
            else 
                return true; 
        }
        #endregion

        static List<OperationDN> GetOperations()
        {
            return operations.Values.SelectMany(b => b.Keys).ToHashSet().Select(k => OperationDN.FromEnum(k)).ToList();
        }

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

        public static List<OperationInfo> GetConstructorOperationInfos(Type entityType)
        {
            return (from k in OperationKeys
                    let o = TryFind(entityType, k) as IConstructorOperation
                    where o != null && OnAllowOperation(k)
                    select ToOperationInfo(o, true)).ToList();
        }

        public static List<OperationInfo> GetQueryOperationInfos(Type entityType)
        {
            return (from k in OperationKeys
                    let cfm = TryFind(entityType, k) as IConstructorFromManyOperation
                    where cfm != null && OnAllowOperation(k)
                    select ToOperationInfo(cfm, true)).ToList();
        }

        public static List<OperationInfo> GetEntityOperationInfos(IdentifiableEntity entity)
        {
            return (from k in OperationKeys
                    let eo = TryFind(entity.GetType(), k) as IEntityOperation
                    where eo != null && OnAllowOperation(k)
                    select ToOperationInfo(eo, eo.CanExecute(entity))).ToList();
        }

        #region Execute
        public static IdentifiableEntity ExecuteLazy(this Lazy lazy, Enum operationKey, params object[] args)
        {
            return ExecutePrivate(Find<IExecuteOperation>(lazy.RuntimeType, operationKey, true), Database.RetrieveAndForget(lazy), args);
        }

        public static IdentifiableEntity ExecuteLazy(this Lazy lazy, Type entityType, Enum operationKey, params object[] args)
        {
            return ExecutePrivate(Find<IExecuteOperation>(entityType, operationKey, true), Database.RetrieveAndForget(lazy), args);
        }

        public static IdentifiableEntity Execute(this IIdentifiable entity, Enum operationKey, params object[] args)
        {
            return ExecutePrivate(Find<IExecuteOperation>(entity.GetType(), operationKey, false), (IdentifiableEntity)entity, args);
        }

        public static IdentifiableEntity Execute(this IIdentifiable entity, Type entityType, Enum operationKey, params object[] args)
        {
            return ExecutePrivate(Find<IExecuteOperation>(entityType, operationKey, false), (IdentifiableEntity)entity, args);
        }

        static IdentifiableEntity ExecutePrivate(IExecuteOperation operation, IdentifiableEntity entity, params object[] args)
        {
            try
            {
                if (BeginOperation != null)
                    BeginOperation(operation, entity);

                operation.Execute(entity, args);

                if (EndOperation != null)
                    EndOperation(operation, entity);

                return entity;
            }
            catch (Exception ex)
            {
                if (ErrorOperation != null)
                    ErrorOperation(operation, entity, ex);

                throw ex;
            }
        } 
        #endregion

        #region Construct
        public static IdentifiableEntity Construct(Type type, Enum operationKey, params object[] args)
        {
            return ConstructPrivate(Find<IConstructorOperation>(type, operationKey, false), args);
        }

        public static T Construct<T>(Enum operationKey, params object[] args)
            where T : IIdentifiable
        {
            return (T)(IIdentifiable)ConstructPrivate(Find<IConstructorOperation>(typeof(T), operationKey, false), args);
        }

        static IdentifiableEntity ConstructPrivate(IConstructorOperation operation, params object[] parameters)
        {
            try
            {
                if (BeginOperation != null)
                    BeginOperation(operation, null);

                IdentifiableEntity result = operation.Construct(parameters);

                if (EndOperation != null)
                    EndOperation(operation, null);

                return result;
            }
            catch (Exception ex)
            {
                if (ErrorOperation != null)
                    ErrorOperation(operation, null, ex);

                throw ex;
            }
        }
        #endregion

        #region ConstructFrom
        public static IdentifiableEntity ConstructFrom(this IIdentifiable entity, Enum operationKey, params object[] args)
        {
            return ConstructFromPrivate(Find<IConstructorFromOperation>(entity.GetType(), operationKey, false), (IdentifiableEntity)entity, args);
        }

        static IdentifiableEntity ConstructFromPrivate(IConstructorFromOperation operation, IdentifiableEntity entity, params object[] parameters)
        {
            try
            {
                OnBeginOperation(operation, entity);

                IdentifiableEntity result = (IdentifiableEntity)operation.Construct(entity, parameters);

                OnEndOperation(operation, entity);

                return result;
            }
            catch (Exception ex)
            {
                OnErrorOperation(operation, entity, ex);

                throw ex;
            }
        }

        public static IdentifiableEntity ConstructFrom(this Lazy lazy, Enum operationKey, params object[] args)
        {
            return ConstructFromPrivate(Find<IConstructorFromOperation>(lazy.RuntimeType, operationKey, true), lazy, args);
        }

        static IdentifiableEntity ConstructFromPrivate(IConstructorFromOperation operation, Lazy lazy, params object[] parameters)
        {
            try
            {
                OnBeginOperation(operation, null);

                IdentifiableEntity result = (IdentifiableEntity)operation.Construct(lazy, parameters);

                OnEndOperation(operation, result);

                return result;
            }
            catch (Exception ex)
            {
                OnErrorOperation(operation, null, ex);

                throw ex;
            }
        }
        #endregion

        #region ConstructFromMany
        public static IdentifiableEntity ConstructFromMany(List<Lazy> lazies, Type type, Enum operationKey, params object[] args)
        {
            return ConstructFromManyPrivate(Find<IConstructorFromManyOperation>(type, operationKey, true), null, args);
        }

        public static T ConstructFromMany<T>(List<Lazy> lazies, Enum operationKey, params object[] args)
            where T : IIdentifiable
        {
            return (T)(IIdentifiable)ConstructFromManyPrivate(Find<IConstructorFromManyOperation>(typeof(T), operationKey, true), null, args);
        }

        static IdentifiableEntity ConstructFromManyPrivate(IConstructorFromManyOperation operation, List<Lazy> lazies, params object[] parameters)
        {
            try
            {
                OnBeginOperation(operation, null);

                IdentifiableEntity result = (IdentifiableEntity)operation.Construct(lazies, parameters);

                OnEndOperation(operation, result);

                return result;
            }
            catch (Exception ex)
            {
                OnErrorOperation(operation, null, ex);

                throw ex;
            }
        }
        #endregion

        static T Find<T>(Type type, Enum operationKey, bool isLazy)
            where T:IOperation
        {
            IOperation result = TryFind(type, operationKey);
            if (result == null)
                throw new ApplicationException("Operation {0} not found for Type {1}".Formato(operationKey, type));

            if (isLazy && !result.Lazy)
                throw new ApplicationException("Operation {0} is not allowed for lazies");

            if (!isLazy && result.Lazy)
                throw new ApplicationException("Operation {0} needs a Lazy");

            if (!(result is T))
                throw new ApplicationException("Operation {0} is not a {1} ".Formato(operationKey, typeof(T))); 

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
                return (IExecuteOperation)result;

            List<Type> interfaces = type.GetInterfaces()
                .Where(t => typeof(IdentifiableEntity).IsAssignableFrom(t) && operations.TryGetC(t).TryGetC(operationKey) != null)
                .ToList();

            if (interfaces.Count > 1)
                throw new ApplicationException("Ambiguity between interfaces: {0}".Formato(interfaces.ToString(", ")));

            if (interfaces.Count < 1)
                return null;

            return operations[interfaces.Single()][operationKey];
        }
    }

    public delegate void OperationHandler(IOperation operation, IdentifiableEntity entity);
    public delegate void ErrorOperationHandler(IOperation operation, IdentifiableEntity entity, Exception ex);
    public delegate bool AllowOperationHandler(Enum operationKey);
}
