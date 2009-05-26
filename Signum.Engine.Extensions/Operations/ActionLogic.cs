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
    public interface IActionOption
    {
        ActionType ActionType { get; set; }
        bool CanExecute(IIdentifiable entity);
        void Execute(IIdentifiable entity, params object[] parameters);
    }

    public class ActionOption<T> : IActionOption
        where T : IIdentifiable
    {
        public ActionType ActionType { get; set; }

        public ActionOption(ActionType actionType, Action<T, object[]> execute, Func<T, bool> canExecute)
        {
            if (execute == null)
                throw new ArgumentException("execute");

            this.execute = execute;
            this.canExecute = canExecute;
            this.ActionType = actionType;
        }

        Func<T, bool> canExecute;
        Action<T, object[]> execute;

        public bool CanExecute(IIdentifiable entity)
        {
            if (canExecute != null)
                return canExecute((T)entity);
            return true;
        }

        public void Execute(IIdentifiable entity, params object[] parameters)
        {
            execute((T)entity, parameters);
        }
    }

    public static class ActionLogic
    {
        static Dictionary<Enum, Dictionary<Type, IActionOption>> actionOptions = new Dictionary<Enum, Dictionary<Type, IActionOption>>();
        internal static Dictionary<Enum, ActionDN> ToAction;
        internal static Dictionary<string, Enum> ToEnum;

        public static event ExecuteActionHandler ExecutingEvent;
        public static event ExecuteActionHandler ExecutedEvent;
        public static event ErrorActionHandler ErrorEvent;

        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined<ActionDN>())
            {
                sb.Include<ActionDN>();
                sb.Include<LogActionDN>();

                sb.Schema.Initializing += Schema_Initializing;
                sb.Schema.Synchronizing += Schema_Synchronizing;
                sb.Schema.Generating += Schema_Generating;
            }
        }

        static void Schema_Initializing(Schema sender)
        {
            using (AuthLogic.Disable())
            using (new ObjectCache(true))
            {
                ToAction = EnumerableExtensions.JoinStrict(
                     Database.RetrieveAll<ActionDN>(),
                     actionOptions.Keys,
                     a => a.Key,
                     k => ActionDN.UniqueKey(k),
                     (a, k) => new { a, k }, "Caching ActionDN").ToDictionary(p => p.k, p => p.a);

                ToEnum = ToAction.Keys.ToDictionary(k => ActionDN.UniqueKey(k));
            }
        }

        static SqlPreCommand Schema_Generating()
        {
            Table table = Schema.Current.Table<ActionDN>();

            return GraphActions().Select(a => table.InsertSqlSync(a)).Combine(Spacing.Simple);
        }

        const string ActionsKey = "Actions";
        static SqlPreCommand Schema_Synchronizing(Replacements replacements)
        {
            Table table = Schema.Current.Table<ActionDN>();

            List<ActionDN> current = Administrator.TryRetrieveAll<ActionDN>(replacements);

            return Synchronizer.SyncronizeReplacing(replacements, ActionsKey,
                current.ToDictionary(c => c.Key),
                GraphActions().ToDictionary(s => s.Key),
                (k, c) => table.DeleteSqlSync(c.Id),
                (k, s) => table.InsertSqlSync(s),
                (k, c, s) =>
                {
                    c.Name = s.Name;
                    c.Key = s.Key;
                    return table.UpdateSqlSync(c);
                }, Spacing.Double);
        }

        static List<ActionDN> GraphActions()
        {
            return actionOptions.Keys.Select(k => ActionDN.FromEnum(k)).ToList();
        }

        public static void Register<T>(Enum actionKey, Action<T, object[]> execute)
           where T : IIdentifiable
        {
            Register<T>(actionKey, new ActionOption<T>(ActionType.Both, execute, null));
        }

        public static void Register<T>(Enum actionKey, Action<T, object[]> execute, Func<T, bool> canExecute)
            where T : IIdentifiable
        {
            Register<T>(actionKey, new ActionOption<T>(ActionType.Both, execute, canExecute));
        }

        public static void Register<T>(Enum actionKey, IActionOption option)
        {
            actionOptions.GetOrCreate(actionKey)[typeof(T)] = option;
        }

        public static List<ActionInfo> GetActionInfos(Lazy lazy)
        {
            IdentifiableEntity entity = Database.Retrieve(lazy);

            return (from k in actionOptions.Keys
                    let ao = TryFind(k, entity.GetType())
                    where ao != null
                    select new ActionInfo
                    {
                        ActionKey = k,
                        ActionType = ao.ActionType,
                        CanExecute = ao.CanExecute(entity)
                    }).ToList();
        }

        public static void ExecuteLazy(this Lazy lazy, Enum actionKey, params object[] parameters)
        {
            IdentifiableEntity entity = Database.Retrieve(lazy);
            ExecutePrivate(Find(actionKey, entity.GetType()), (IdentifiableEntity)entity, actionKey, parameters);
        }

        public static void ExecuteLazy(this Lazy lazy, Type entityType, Enum actionKey, params object[] parameters)
        {
            IdentifiableEntity entity = Database.Retrieve(lazy);
            ExecutePrivate(Find(actionKey, entityType), (IdentifiableEntity)entity, actionKey, parameters);
        }

        public static void Execute(this IIdentifiable entity, Enum actionKey, params object[] parameters)
        {
            ExecutePrivate(Find(actionKey, entity.GetType()), (IdentifiableEntity)entity, actionKey, parameters);
        }

        public static void Execute(this IIdentifiable entity, Type entityType, Enum actionKey, params object[] parameters)
        {
            ExecutePrivate(Find(actionKey, entityType), (IdentifiableEntity)entity, actionKey, parameters);
        }

        static void ExecutePrivate(IActionOption actionOptions, IdentifiableEntity entity, Enum actionKey, params object[] parameters)
        {
            ActionDN actionDN = ToAction[actionKey];

            LogActionDN logAction = null;
            try
            {
                using (Transaction tr = new Transaction())
                {
                    if (ExecutingEvent != null)
                        ExecutingEvent(actionKey, actionDN, entity, parameters);

                    logAction = new LogActionDN
                    {
                        Action = actionDN,
                        Start = DateTime.Now,
                        User = (UserDN)Thread.CurrentPrincipal
                    };

                    actionOptions.Execute(entity, parameters);

                    entity.Save(); //Nothing happens if allready saved

                    logAction.Entity = entity.ToLazy<IdentifiableEntity>();
                    logAction.End = DateTime.Now;
                    logAction.Save();

                    if (ExecutedEvent != null)
                        ExecutedEvent(actionKey, actionDN, entity, parameters);

                    tr.Commit();
                }
            }
            catch (Exception ex)
            {
                if (!entity.IsNew)
                {
                    using (Transaction tr2 = new Transaction(true))
                    {
                        logAction.Exception = ex.Message;
                        logAction.Entity = entity.ToLazy<IdentifiableEntity>();
                        logAction.End = DateTime.Now;
                        logAction.Save();

                        tr2.Commit();
                    }
                }

                if (ErrorEvent != null)
                    ErrorEvent(actionKey, actionDN, entity, ex);
            }
        }

        public static bool CanExecuteLazy(this Lazy lazy, Enum actionKey)
        {
            IdentifiableEntity entity = Database.Retrieve(lazy);
            return Find(actionKey, entity.GetType()).CanExecute(entity);
        }

        public static bool CanExecuteLazy(this Lazy lazy, Type entityType, Enum actionKey)
        {
            return Find(actionKey, entityType).CanExecute(Database.Retrieve(lazy));
        }

        public static bool CanExecute(this IIdentifiable entity, Enum actionKey)
        {
            return Find(actionKey, entity.GetType()).CanExecute((IdentifiableEntity)entity);
        }

        public static bool CanExecute(this IIdentifiable entity, Type entityType, Enum actionKey)
        {
            return Find(actionKey, entityType).CanExecute((IdentifiableEntity)entity);
        }

        static IActionOption Find(Enum actionKey, Type type)
        {
            IActionOption result = TryFind(actionKey, type);
            if (result == null)
                throw new ApplicationException("Action {0} not found for Type {1}".Formato(actionKey, type));
            return result;
        }

        static IActionOption TryFind(Enum actionKey, Type type)
        {
            if (!typeof(IIdentifiable).IsAssignableFrom(type))
                throw new ApplicationException("type is a {0} but to implement {1} at least".Formato(type, typeof(IIdentifiable)));

            var dic = actionOptions.TryGetC(actionKey);

            if (dic == null)
                return null;

            IActionOption result = type.FollowC(t => t.BaseType)
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

    public delegate void ExecuteActionHandler(Enum actionKey, ActionDN actionDN, IdentifiableEntity entity, object[] parameters);
    public delegate void ErrorActionHandler(Enum actionKey, ActionDN actionDN, IdentifiableEntity entity, Exception ex);
    public delegate bool CanExecuteActionHandler(Enum actionKey, ActionDN actionDN, IdentifiableEntity entityOrNull);

}
