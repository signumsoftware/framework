using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Maps;
using Signum.Entities;
using System.Collections;
using System.Threading;
using Signum.Utilities;
using Signum.Engine.Exceptions;
using System.Collections.Concurrent;
using Signum.Utilities.DataStructures;
using Signum.Entities.Reflection;
using Signum.Utilities.Reflection;
using System.Reflection;
using Signum.Entities.Cache;
using Signum.Engine.Authorization;
using System.Drawing;
using Signum.Entities.Basics;
using System.Xml.Linq;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Data.SqlTypes;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Engine.Cache
{
    public static class CacheLogic
    {
        /// <summary>
        /// If you have invalidation problems look at exceptions in: select * from sys.transmission_queue 
        /// If there are exceptions like: 'Could not obtain information about Windows NT group/user'
        ///    Change login to a SqlServer authentication (i.e.: sa)
        ///    Change Server Authentication mode and enable SA: http://msdn.microsoft.com/en-us/library/ms188670.aspx
        ///    Change Database ownership to sa: ALTER AUTHORIZATION ON DATABASE::yourDatabase TO sa
        /// </summary>
        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                PermissionAuthLogic.RegisterTypes(typeof(CachePermission));

                GlobalLazy.Manager = new CacheGlobalLazyManager();

                sb.Schema.Initializing[InitLevel.Level_0BeforeAnyQuery] += OnStart;
            }
        }

        static void OnStart()
        {
            try
            {
                SqlDependency.Start(((SqlConnector)Connector.Current).ConnectionString);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.Contains("SQL Server Service Broker"))
                    throw new InvalidOperationException(@"CacheLogic requires SQL Server Service Broker to be activated. Execute: 
ALTER DATABASE {0} SET ENABLE_BROKER".Formato(Connector.Current.DatabaseName()));
            }
        }

        public static void Shutdown()
        {
            SqlDependency.Stop(((SqlConnector)Connector.Current).ConnectionString);
        }

        static SqlPreCommandSimple GetDependencyQuery(ITable table)
        {
            return new SqlPreCommandSimple("SELECT {0} FROM {1}".Formato(table.Columns.Keys.ToString(c => c.SqlScape(), ", "), table.Name));
        }      

        class CacheController<T> : CacheControllerBase<T>, ICacheLogicController
                where T : IdentifiableEntity
        {
            public CachedTable<T> cachedTable;
            public CachedTableBase CachedTable { get { return cachedTable; } }

            public CacheController(Schema schema)
            {
                var ee = schema.EntityEvents<T>();

                ee.CacheController = this;
                ee.Saving += Saving;
                ee.PreUnsafeDelete += PreUnsafeDelete;
                ee.PreUnsafeUpdate += UnsafeUpdated;
            }

            public void BuildCachedTable()
            {
                cachedTable = new CachedTable<T>(this, new Linq.AliasGenerator(), null, null);
            }

            void UnsafeUpdated(IQueryable<T> query)
            {
                DisableAllConnectedTypesInTransaction(typeof(T));
            }

            void PreUnsafeDelete(IQueryable<T> query)
            {
                DisableTypeInTransaction(typeof(T));
            }

            void Saving(T ident)
            {
                if (ident.Modified.Value)
                {
                    if (ident.IsNew)
                    {
                        DisableTypeInTransaction(typeof(T));
                    }
                    else
                    {
                        DisableAllConnectedTypesInTransaction(typeof(T));
                    }
                }
            }

            public override bool Enabled
            {
                get { return !GloballyDisabled && !tempDisabled.Value && !IsDisabledInTransaction(typeof(T)); }
            }

            private void AssertEnabled()
            {
                if (!Enabled)
                    throw new InvalidOperationException("Cache for {0} is not enabled".Formato(typeof(T).TypeName()));
            }

            public event EventHandler<CacheEventArgs> Invalidated;

            public void OnChange(object sender, SqlNotificationEventArgs args)
            {
                NotifyInvalidateAllConnectedTypes(typeof(T));
            }

            static object syncLock = new object();

            public override void Load()
            {
                cachedTable.LoadAll();
            }

            public void ForceReset()
            {
                cachedTable.ForceReset();
            }

            public override IEnumerable<int> GetAllIds()
            {
                AssertEnabled();

                return cachedTable.GetAllIds();
            }

            public override string GetToString(int id)
            {
                AssertEnabled();

                return cachedTable.GetToString(id);
            }

            public override void Complete(T entity, IRetriever retriver)
            {
                AssertEnabled();

                cachedTable.Complete(entity, retriver);
            }

            public void NotifyDisabled()
            {
                if (Invalidated != null)
                    Invalidated(this, CacheEventArgs.Disabled);
            }

            public void NotifyInvalidated()
            {
                if (Invalidated != null)
                    Invalidated(this, CacheEventArgs.Invalidated);
            }
        }

        static Dictionary<Type, ICacheLogicController> controllers = new Dictionary<Type, ICacheLogicController>(); //CachePack

        static DirectedGraph<Type> inverseDependencies = new DirectedGraph<Type>();

        public static bool GloballyDisabled { get; set; }

        static readonly Variable<bool> tempDisabled = Statics.ThreadVariable<bool>("cacheTempDisabled");
        public static IDisposable Disable()
        {
            if (tempDisabled.Value) return null;
            tempDisabled.Value = true;
            return new Disposable(() => tempDisabled.Value = false); 
        }

        const string DisabledCachesKey = "disabledCaches";

        static HashSet<Type> DisabledTypesDuringTransaction()
        {
            var hs = Transaction.UserData.TryGetC(DisabledCachesKey) as HashSet<Type>;
            if (hs == null)
            {
                Transaction.UserData[DisabledCachesKey] = hs = new HashSet<Type>();
            }

            return hs; 
        }

        static bool IsDisabledInTransaction(Type type)
        {
            if (!Transaction.HasTransaction)
                return false;

            HashSet<Type> disabledTypes = Transaction.UserData.TryGetC(DisabledCachesKey) as HashSet<Type>;

            return disabledTypes != null && disabledTypes.Contains(type);
        }

        private static void DisableTypeInTransaction(Type type)
        {
            DisabledTypesDuringTransaction().Add(type);

            controllers[type].NotifyDisabled();
        }

        private static void DisableAllConnectedTypesInTransaction(Type type)
        {
            var connected = inverseDependencies.IndirectlyRelatedTo(type, true);

            var hs = DisabledTypesDuringTransaction();

            foreach (var stype in connected)
            {
                hs.Add(stype);
                controllers[stype].NotifyDisabled();
            }
        }

        public static void CacheAllGlobalLazyTables(SchemaBuilder sb)
        {
            foreach (var type in GlobalLazy.TypesForInvalidation())
            {
                if (!controllers.ContainsKey(type))
                    giCacheTable.GetInvoker(type)(sb);
            }
        }

        static GenericInvoker<Action<SchemaBuilder>> giCacheTable = new GenericInvoker<Action<SchemaBuilder>>(sb => CacheTable<IdentifiableEntity>(sb));
        public static void CacheTable<T>(SchemaBuilder sb) where T : IdentifiableEntity
        {
            var cc = new CacheController<T>(sb.Schema);
            controllers.AddOrThrow(typeof(T), cc, "{0} already registered");

            TryCacheSubTables(typeof(T), sb);

            cc.BuildCachedTable();
        }

        public static void SemiCacheTable<T>(SchemaBuilder sb) where T : IdentifiableEntity
        {
            controllers.AddOrThrow(typeof(T), null, "{0} already registered");

            TryCacheSubTables(typeof(T), sb);
        }

        private static void TryCacheSubTables(Type type, SchemaBuilder sb)
        {
            List<Type> relatedTypes = sb.Schema.Table(type).DependentTables()
                .Where(kvp =>!kvp.Key.Type.IsInstantiationOf(typeof(EnumEntity<>)))
                .Select(t => t.Key.Type).ToList();

            inverseDependencies.Add(type);

            foreach (var rType in relatedTypes)
            {
                if (!controllers.ContainsKey(rType))
                    giCacheTable.GetInvoker(rType)(sb);

                inverseDependencies.Add(rType, type);
            }
        }
    
        static ICacheLogicController GetController(Type type)
        {
            var controller = controllers.GetOrThrow(type, "{0} is not registered in CacheLogic");

            if (controller == null)
                throw new InvalidOperationException("{0} is just semi cached");

            return controller;
        }

        private static void NotifyInvalidateAllConnectedTypes(Type type)
        {
            var connected = inverseDependencies.IndirectlyRelatedTo(type, includeParentNode : true);

            foreach (var stype in connected)
            {
                controllers[stype].NotifyInvalidated();
            }
        }

        public static List<CachedTableBase> Statistics()
        {
            return controllers.Values.NotNull().Select(a => a.CachedTable).OrderByDescending(a => a.Count).ToList();
        }

        public static CacheType GetCacheType(Type type)
        {
            ICacheLogicController controller;
            if (!controllers.TryGetValue(type, out controller))
                return CacheType.None;

            if (controller == null)
                return CacheType.Semi;

            return CacheType.Cached;
        }

        public static void ForceReset()
        {
            foreach (var controller in controllers.Values.NotNull())
            {
                controller.ForceReset();
            }
        }

        public static XDocument SchemaGraph(Func<Type, bool> cacheHint)
        {
            var dgml = Schema.Current.ToDirectedGraph().ToDGML(t =>
                new[]
            {
                new XAttribute("Label", t.Name),
                new XAttribute("Background", GetColor(t.Type, cacheHint).ToHtml())
            }, info => new[]
            {
                info.IsLite ? new XAttribute("StrokeDashArray",  "2 3") : null,
            }.NotNull().ToArray());

            return dgml;
        }

        static Color GetColor(Type type, Func<Type, bool> cacheHint)
        {
            if (type.IsEnumEntity())
                return Color.Red;

            switch (CacheLogic.GetCacheType(type))
            {
                case CacheType.Cached: return Color.Purple;
                case CacheType.Semi: return Color.Pink;
            }

            if (typeof(MultiEnumDN).IsAssignableFrom(type))
                return Color.Orange;

            if (cacheHint != null && cacheHint(type))
                return Color.Yellow;

            return Color.Green;
        }

        public class CacheGlobalLazyManager : GlobalLazyManager
        {
            public override void AttachInvalidations(InvalidateWith invalidateWith, EventHandler invalidate)
            {
                EventHandler<CacheEventArgs> onInvalidation = (sender, args) =>
                {
                    if (args == CacheEventArgs.Invalidated)
                    {
                        invalidate(sender, args);
                    }
                    else if (args == CacheEventArgs.Disabled)
                    {
                        if (Transaction.InTestTransaction)
                        {
                            invalidate(sender, args);
                            Transaction.Rolledback += () => invalidate(sender, args);
                        }

                        Transaction.PostRealCommit += dic => invalidate(sender, args);
                    }
                };

                foreach (var t in invalidateWith.Types)
                {
                    var ctrlr = controllers.TryGetC(t);
                    if (ctrlr == null)
                        throw new InvalidOperationException(@"Impossible to attach invalidations to {0} because is not registered in CacheLogic. 
Consider calling CacheLogic.CacheAllGlobalLazyTables at the end of your Starter.Start method".Formato(t.Name));

                    ctrlr.Invalidated += onInvalidation;
                }
            }

            public override void AssertAttached(InvalidateWith invalidateWith)
            {
                foreach (var type in invalidateWith.Types)
                {
                    GetController(type).Load();
                }
            }
        }
    }

    internal interface ICacheLogicController : ICacheController
    {
        event EventHandler<CacheEventArgs> Invalidated; 

        CachedTableBase CachedTable { get; }

        void NotifyDisabled();

        void NotifyInvalidated();

        void OnChange(object sender, SqlNotificationEventArgs args);

        void ForceReset();
    }

    public class CacheEventArgs : EventArgs
    {
        private CacheEventArgs() { }

        public static readonly CacheEventArgs Invalidated = new CacheEventArgs();
        public static readonly CacheEventArgs Disabled = new CacheEventArgs();
    }

    public enum CacheType
    {
        Cached,
        Semi,
        None
    }
}
