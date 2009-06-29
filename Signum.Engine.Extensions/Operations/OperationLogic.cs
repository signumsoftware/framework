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
        OperationFlags Flags { get; set; }
        bool CanExecuteOperation(IIdentifiable entity);
        void ExecuteOperation(IIdentifiable entity, params object[] parameters);
    }

    public class Operation<T> : IOperation
        where T : IIdentifiable
    {
        public OperationFlags Flags { get; set; }

        public Operation(OperationFlags flags, Action<T, object[]> execute, Func<T, bool> canExecute)
        {
            if (execute == null)
                throw new ArgumentException("execute");

            this.execute = execute;
            this.canExecute = canExecute;
            this.Flags = flags;
        }

        Func<T, bool> canExecute;
        Action<T, object[]> execute;

        public bool CanExecuteOperation(IIdentifiable entity)
        {
            if (canExecute != null)
                return canExecute((T)entity);
            return true;
        }

        public void ExecuteOperation(IIdentifiable entity, params object[] parameters)
        {
            execute((T)entity, parameters);
        }
    }

    public static class OperationLogic
    {
        static Dictionary<Enum, Dictionary<Type, IOperation>> operations = new Dictionary<Enum, Dictionary<Type, IOperation>>();
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
                ToOperation = EnumerableExtensions.JoinStrict(
                     Database.RetrieveAll<OperationDN>(),
                     operations.Keys,
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
                (k, c) => table.DeleteSqlSync(c.Id),
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
            return operations.Keys.Select(k => OperationDN.FromEnum(k)).ToList();
        }

        public static void Register<T>(Enum operationKey, Action<T, object[]> execute)
           where T : IIdentifiable
        {
            Register<T>(operationKey, new Operation<T>(OperationFlags.Default, execute, null));
        }

        public static void Register<T>(Enum operationKey, Action<T, object[]> execute, Func<T, bool> canExecute)
            where T : IIdentifiable
        {
            Register<T>(operationKey, new Operation<T>(OperationFlags.Default, execute, canExecute));
        }

        public static void Register<T>(Enum operationKey, IOperation option)
             where T : IIdentifiable
        {
            operations.GetOrCreate(operationKey)[typeof(T)] = option;
        }

        public static List<OperationInfo> GetOperationInfos(Lazy lazy)
        {
            IdentifiableEntity entity = Database.Retrieve(lazy);

            return (from k in operations.Keys
                    let ao = TryFind(k, entity.GetType())
                    where ao != null
                    select new OperationInfo
                    {
                        OperationKey = k,
                        Flags = ao.Flags,
                        CanExecute = ao.CanExecuteOperation(entity)
                    }).ToList();
        }

        public static IdentifiableEntity ExecuteLazy(this Lazy lazy, Enum operationKey, params object[] parameters)
        {
            IdentifiableEntity entity = Database.RetrieveAndForget(lazy);
            return ExecutePrivate(Find(operationKey, entity.GetType(), true), (IdentifiableEntity)entity, operationKey, parameters);
        }

        public static IdentifiableEntity ExecuteLazy(this Lazy lazy, Type entityType, Enum operationKey, params object[] parameters)
        {
            return ExecutePrivate(Find(operationKey, entityType, true), Database.RetrieveAndForget(lazy), operationKey, parameters);
        }

        public static IdentifiableEntity Execute(this IIdentifiable entity, Enum operationKey, params object[] parameters)
        {
            return ExecutePrivate(Find(operationKey, entity.GetType(), false), (IdentifiableEntity)entity, operationKey, parameters);
        }

        public static IdentifiableEntity Execute(this IIdentifiable entity, Type entityType, Enum operationKey, params object[] parameters)
        {
            return ExecutePrivate(Find(operationKey, entityType, false), (IdentifiableEntity)entity, operationKey, parameters);
        }

        static IdentifiableEntity ExecutePrivate(IOperation operationOptions, IdentifiableEntity entity, Enum operationKey, params object[] parameters)
        {
            OperationDN operation = ToOperation[operationKey];

            LogOperationDN log = null;
            try
            {
                using (Transaction tr = new Transaction())
                {
                    if (ExecutingEvent != null)
                        ExecutingEvent(operationKey, operation, entity, parameters);

                    log = new LogOperationDN
                    {
                        Operation = operation,
                        Start = DateTime.Now,
                        User = (UserDN)Thread.CurrentPrincipal
                    };

                    operationOptions.ExecuteOperation(entity, parameters);

                    entity.Save(); //Nothing happens if allready saved

                    log.Entity = entity.ToLazy<IdentifiableEntity>();
                    log.End = DateTime.Now;
                    log.Save();

                    if (ExecutedEvent != null)
                        ExecutedEvent(operationKey, operation, entity, parameters);

                    return tr.Commit((operationOptions.Flags & OperationFlags.Returns) == OperationFlags.Returns ? entity : null);
                }
            }
            catch (Exception ex)
            {
                if (!entity.IsNew)
                {
                    using (Transaction tr2 = new Transaction(true))
                    {
                        log.Exception = ex.Message;
                        log.Entity = entity.ToLazy<IdentifiableEntity>();
                        log.End = DateTime.Now;
                        log.Save();

                        tr2.Commit();
                    }
                }

                if (ErrorEvent != null)
                    ErrorEvent(operationKey, operation, entity, ex);

                throw ex; 
            }
        }

        public static bool CanExecuteLazy(this Lazy lazy, Enum operationKey)
        {
            IdentifiableEntity entity = Database.Retrieve(lazy);
            return Find(operationKey, entity.GetType(), true).CanExecuteOperation(entity);
        }

        public static bool CanExecuteLazy(this Lazy lazy, Type entityType, Enum operationKey)
        {
            return Find(operationKey, entityType, true).CanExecuteOperation(Database.Retrieve(lazy));
        }

        public static bool CanExecute(this IIdentifiable entity, Enum operationKey)
        {
            return Find(operationKey, entity.GetType(), false).CanExecuteOperation((IdentifiableEntity)entity);
        }

        public static bool CanExecute(this IIdentifiable entity, Type entityType, Enum operationKey)
        {
            return Find(operationKey, entityType, false).CanExecuteOperation((IdentifiableEntity)entity);
        }

        static IOperation Find(Enum operationKey, Type type, bool isLazy)
        {
            IOperation result = TryFind(operationKey, type);
            if (result == null)
                throw new ApplicationException("Operation {0} not found for Type {1}".Formato(operationKey, type));

            if ((result.Flags & OperationFlags.Lazy) != OperationFlags.Lazy && isLazy)
                throw new ApplicationException("Operation {0} is not alowed for lazies");

            if ((result.Flags & OperationFlags.Entity) != OperationFlags.Entity && !isLazy)
                throw new ApplicationException("Operation {0} is not alowed for lazies"); 

            return result;
        }

        static IOperation TryFind(Enum operationKey, Type type)
        {
            if (!typeof(IIdentifiable).IsAssignableFrom(type))
                throw new ApplicationException("type is a {0} but to implement {1} at least".Formato(type, typeof(IIdentifiable)));

            var dic = operations.TryGetC(operationKey);

            if (dic == null)
                return null;

            IOperation result = type.FollowC(t => t.BaseType)
                .TakeWhile(t => typeof(IdentifiableEntity).IsAssignableFrom(t))
                .Select(t => dic.TryGetC(t)).FirstOrDefault();

            if (result != null)
                return result;

            List<Type> interfaces = type.GetInterfaces()
                .Where(t => typeof(IdentifiableEntity).IsAssignableFrom(t) && dic.ContainsKey(t))
                .ToList();

            if (interfaces.Count > 1)
                throw new ApplicationException("Ambiguity between interfaces: {0}".Formato(interfaces.ToString(", ")));

            if (interfaces.Count < 1)
                return null;

            return dic[interfaces.Single()];
        }
    }

    public delegate void ExecuteOperationHandler(Enum operationKey, OperationDN actionDN, IdentifiableEntity entity, object[] parameters);
    public delegate void ErrorOperationHandler(Enum operationKey, OperationDN actionDN, IdentifiableEntity entity, Exception ex);
    public delegate bool CanExecuteActionHandler(Enum operationKey, OperationDN actionDN, IdentifiableEntity entityOrNull);

}
