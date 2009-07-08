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

    public static class OperationLogic
    {
        static Dictionary<Type, Dictionary<Enum, IOperation>> operations = new Dictionary<Type, Dictionary<Enum, IOperation>>();

        internal static HashSet<Enum> OperationKeys; 
        internal static Dictionary<Enum, OperationDN> ToOperation;
        internal static Dictionary<string, Enum> ToEnum;

        public static event ExecuteOperationHandler ExecutingEvent;
        public static event ExecuteOperationHandler ExecutedEvent;
        public static event ErrorOperationHandler ErrorEvent;

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

        static List<OperationDN> GetOperations()
        {
            return operations.Values.SelectMany(b => b.Keys).ToHashSet().Select(k => OperationDN.FromEnum(k)).ToList();
        }

        public static void Register(IOperation operation)
        {
            operation.AssertIsValid(); 

            operations.GetOrCreate(operation.Type)[operation.Key] = operation;
        }

        public static List<OperationInfo> GetOperationInfos(Lazy lazy)
        {
            IdentifiableEntity entity = Database.Retrieve(lazy);

            return (from k in OperationKeys
                    let ao = TryFind(k, entity.GetType())
                    where ao != null
                    select new OperationInfo
                    {
                        Key = k,
                        Lazy = ao.Lazy,
                        Returns = ao.Returns,
                        OperationType = ao.OperationType,
                        CanExecute = ao.CanExecute(entity)
                    }).ToList();
        }

        public static IdentifiableEntity ExecuteLazy(this Lazy lazy, Enum operationKey, params object[] parameters)
        {
            IdentifiableEntity entity = Database.RetrieveAndForget(lazy);
            return ExecutePrivate(Find(operationKey, entity.GetType(), true), (IdentifiableEntity)entity, parameters);
        }

        public static IdentifiableEntity ExecuteLazy(this Lazy lazy, Type entityType, Enum operationKey, params object[] parameters)
        {
            return ExecutePrivate(Find(operationKey, entityType, true), Database.RetrieveAndForget(lazy), parameters);
        }

        public static IdentifiableEntity Execute(this IIdentifiable entity, Enum operationKey, params object[] parameters)
        {
            return ExecutePrivate(Find(operationKey, entity.GetType(), false), (IdentifiableEntity)entity, parameters);
        }

        public static IdentifiableEntity Execute(this IIdentifiable entity, Type entityType, Enum operationKey, params object[] parameters)
        {
            return ExecutePrivate(Find(operationKey, entityType, false), (IdentifiableEntity)entity, parameters);
        }

        static IdentifiableEntity ExecutePrivate(IExecuteOperation operationOptions, IdentifiableEntity entity, params object[] parameters)
        {
            try
            {
                if (ExecutingEvent != null)
                    ExecutingEvent(operationOptions.Key, entity, parameters);

                operationOptions.Execute(entity, parameters);

                if (ExecutedEvent != null)
                    ExecutedEvent(operationOptions.Key, entity, parameters);

                return entity;

            }
            catch (Exception ex)
            {
                if (ErrorEvent != null)
                    ErrorEvent(operationOptions.Key, entity, ex);

                throw ex;
            }
        }

    
        public static bool CanExecuteLazy(this Lazy lazy, Enum operationKey)
        {
            IdentifiableEntity entity = Database.Retrieve(lazy);
            return Find(operationKey, entity.GetType(), true).CanExecute(entity);
        }

        public static bool CanExecuteLazy(this Lazy lazy, Type entityType, Enum operationKey)
        {
            return Find(operationKey, entityType, true).CanExecute(Database.Retrieve(lazy));
        }

        public static bool CanExecute(this IIdentifiable entity, Enum operationKey)
        {
            return Find(operationKey, entity.GetType(), false).CanExecute((IdentifiableEntity)entity);
        }

        public static bool CanExecute(this IIdentifiable entity, Type entityType, Enum operationKey)
        {
            return Find(operationKey, entityType, false).CanExecute((IdentifiableEntity)entity);
        }

        static IExecuteOperation Find(Enum operationKey, Type type, bool isLazy)
        {
            IExecuteOperation result = TryFind(operationKey, type);
            if (result == null)
                throw new ApplicationException("Operation {0} not found for Type {1}".Formato(operationKey, type));

            if (isLazy && !result.Lazy)
                throw new ApplicationException("Operation {0} is not allowed for lazies");

            if (!isLazy && result.Lazy)
                throw new ApplicationException("Operation {0} needs a Lazy");

            if (result.OperationType != OperationType.Execute)
                throw new ApplicationException("Operation {0} is a {1} not a {2}".Formato(operationKey, result.OperationType, OperationType.Execute)); 

            return result;
        }

        static IExecuteOperation TryFind(Enum operationKey, Type type)
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

            return (IExecuteOperation)operations[interfaces.Single()][operationKey];
        }
    }

    public delegate void ExecuteOperationHandler(Enum operationKey, IdentifiableEntity entity, object[] parameters);
    public delegate void ErrorOperationHandler(Enum operationKey, IdentifiableEntity entity, Exception ex);
    public delegate bool CanExecuteActionHandler(Enum operationKey, IdentifiableEntity entityOrNull);

}
