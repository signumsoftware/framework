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

namespace Signum.Engine.Cache
{
    public static class CacheLogic
    {
        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                PermissionAuthLogic.RegisterTypes(typeof(CachePermission));

                sb.Schema.Initializing[InitLevel.Level1SimpleEntities] += OnStart;
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

        interface ICacheLogicController : ICacheController
        {
            int? Count { get; }

            void Invalidate(bool isClean);

            int Invalidations { get; }
            int Loads { get; }

            int AttachedEvents { get; }

            int Hits { get; }

            void OnDisabled();
        }

        static SqlPreCommandSimple GetDependencyQuery(ITable table)
        {
            return new SqlPreCommandSimple("SELECT {0} FROM {1}".Formato(table.Columns.Keys.ToString(c => c.SqlScape(), ", "), table.Name));
        }      

        class CacheController<T> : CacheControllerBase<T>, ICacheLogicController
                where T : IdentifiableEntity
        {
            ResetLazy<List<SqlPreCommandSimple>> depedencyQueries = new ResetLazy<List<SqlPreCommandSimple>>(() =>
            {
                Table table = Schema.Current.Table(typeof(T));

                List<SqlPreCommandSimple> result = new List<SqlPreCommandSimple>();

                result.Add(GetDependencyQuery(table));

                foreach (var rt in table.RelationalTables())
	            {
                     result.Add(GetDependencyQuery(rt));
	            }

                return result;
            });

            
            ResetLazy<Dictionary<int, T>> pack;

            public CacheController(Schema schema)
            {
                pack = new ResetLazy<Dictionary<int, T>>(() =>
                {
                    using (new EntityCache(true))
                    using (ExecutionMode.Global())
                    using (Transaction tr = inCache.Value ? new Transaction() : Transaction.ForceNew())
                    using (CacheLogic.SetInCache())
                    using (HeavyProfiler.Log("CACHE"))
                    using (CacheLogic.NotifyCacheChange(this.SubCacheChanged))
                    {
                        DisabledTypesDuringTransaction().Add(typeof(T)); //do not raise Disabled event

                        var dic = Database.Query<T>().ToDictionary(a => a.Id);

                        Interlocked.Increment(ref loads);

                        foreach (var sql in depedencyQueries.Value)
                        {
                            AttachDependency(sql);
                        }

                        return tr.Commit(dic);
                    }
                });

                var ee = schema.EntityEvents<T>();

                ee.CacheController = this;
                ee.Saving += Saving;
                ee.PreUnsafeDelete += PreUnsafeDelete;
                ee.PreUnsafeUpdate += UnsafeUpdated;
            }

            void AttachDependency(SqlPreCommandSimple query)
            {
                SqlConnector connector = (SqlConnector)Connector.Current;

                using (HeavyProfiler.Log("Attach SqlDependency", query.Sql))
                using (SqlConnection con = connector.EnsureConnection())
                using (SqlCommand cmd = connector.NewCommand(query, con))
                using (HeavyProfiler.Log("SQL", query.Sql))
                {
                    try
                    {
                        SqlDependency dep = new SqlDependency(cmd);
                        dep.OnChange += SqlDependencyChanged;

                        int result = cmd.ExecuteNonQuery();
                    }
                    catch (SqlTypeException ex)
                    {
                        var nex = connector.HandleSqlTypeException(ex, query);
                        if (nex == ex)
                            throw;
                        throw nex;
                    }
                    catch (SqlException ex)
                    {
                        var nex = connector.HandleSqlException(ex);
                        if (nex == ex)
                            throw;
                        throw nex;
                    }
                }


                
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

            public int? Count
            {
                get { return pack.IsValueCreated ? pack.Value.Count : (int?)null; }
            }

            public int AttachedEvents
            {
                get { return invalidation == null ? 0 : invalidation.GetInvocationList().Length; }
            }

            int invalidations;
            public int Invalidations { get { return invalidations; } }
            int hits;
            public int Hits { get { return hits; } }
            int loads;
            public int Loads { get { return loads; } }

            EventHandler invalidation;

            void SubCacheChanged(object sender, EventArgs args)
            {
                Invalidate(isClean: false);
            }

            void SqlDependencyChanged(object sender, SqlNotificationEventArgs args)
            {
                if (args.Info == SqlNotificationInfo.Invalid &&
                    args.Source == SqlNotificationSource.Statement &&
                    args.Type == SqlNotificationType.Subscribe && Debugger.IsAttached)
                    throw new InvalidOperationException("Invalid query for SqlDependency");

                Invalidate(isClean: false);
            }

            public void Invalidate(bool isClean)
            {
                pack.Reset();
                if (invalidation != null)
                    invalidation(this, InvalidatedCacheEventArgs.Instance);

                if (isClean)
                {
                    invalidations = 0;
                    hits = 0;
                    loads = 0;
                }
                else
                {
                    Interlocked.Increment(ref invalidations);
                }

            }

            static object syncLock = new object();

            public override void Load()
            {
                var eh = queryChange.Value;

                if (eh != null)
                    lock (syncLock)
                    {
                        if (invalidation == invalidation - eh)
                            invalidation += eh;
                    }

                if (pack.IsValueCreated)
                    return;

                pack.Load();
            }

            public override IEnumerable<int> GetAllIds()
            {
                Interlocked.Increment(ref hits);
                return pack.Value.Keys;
            }

            public override string GetToString(int id)
            {
                Interlocked.Increment(ref hits);
                return pack.Value[id].ToString();
            }

            readonly Action<T, T, IRetriever> completer = Completer.GetCompleter<T>();

            public override bool CompleteCache(T entity, IRetriever retriver)
            {
                Interlocked.Increment(ref hits);
                var origin = pack.Value.TryGetC(entity.Id);
                if (origin == null)
                    throw new EntityNotFoundException(typeof(T), entity.Id);
                completer(entity, origin, retriver);
                return true;
            }

            public void OnDisabled()
            {
                if (invalidation != null)
                    invalidation(this, DisabledCacheEventArgs.Instance);
            }

            public object sql { get; set; }
        }

        public class DisabledCacheEventArgs : EventArgs
        {
            private DisabledCacheEventArgs() { }

            public static readonly DisabledCacheEventArgs Instance = new DisabledCacheEventArgs();
        }

        public class InvalidatedCacheEventArgs : EventArgs
        {
            private InvalidatedCacheEventArgs() { }

            public static readonly InvalidatedCacheEventArgs Instance = new InvalidatedCacheEventArgs();
        }
   
        static Dictionary<Type, ICacheLogicController> controllers = new Dictionary<Type, ICacheLogicController>(); //CachePack

        static DirectedGraph<Type> dependencies = new DirectedGraph<Type>();

        static readonly ThreadVariable<bool> inCache = Statics.ThreadVariable<bool>("inCache");
        static IDisposable SetInCache()
        {
            var oldInCache = inCache.Value;
            inCache.Value = true;
            return new Disposable(() => inCache.Value = oldInCache);
        }


        static readonly Variable<EventHandler> queryChange = Statics.ThreadVariable<EventHandler>("queryChange");
        public static IDisposable NotifyCacheChange(EventHandler onChange)
        {
            var old = queryChange.Value;
            queryChange.Value = onChange;
            return new Disposable(() => queryChange.Value = old);
        }
   

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

            controllers[type].OnDisabled();
        }

        private static void DisableAllConnectedTypesInTransaction(Type type)
        {
            var connected = dependencies.IndirectlyRelatedTo(type, true);

            var hs = DisabledTypesDuringTransaction();

            foreach (var stype in connected)
            {
                hs.Add(stype);
                controllers[stype].OnDisabled();
            }
        }

        

        static GenericInvoker<Action<SchemaBuilder>> giCacheTable = new GenericInvoker<Action<SchemaBuilder>>(sb => CacheTable<IdentifiableEntity>(sb));
        public static void CacheTable<T>(SchemaBuilder sb) where T : IdentifiableEntity
        {
            var cc = new CacheController<T>(sb.Schema);
            controllers.AddOrThrow(typeof(T), cc, "{0} already registered");

            TryCacheSubTables(typeof(T), sb);
        }

        public static void AvoidCacheTable<T>(SchemaBuilder sb) where T : IdentifiableEntity
        {
            controllers.AddOrThrow(typeof(T), null, "{0} already registered");
        }

        private static void TryCacheSubTables(Type type, SchemaBuilder sb)
        {
            List<Type> relatedTypes = sb.Schema.Table(type).DependentTables()
                .Where(kvp =>!kvp.Key.Type.IsInstantiationOf(typeof(EnumEntity<>)))
                .Select(t => t.Key.Type).ToList();

            dependencies.Add(type);

            foreach (var rType in relatedTypes)
            {
                if (!controllers.ContainsKey(rType))
                    giCacheTable.GetInvoker(rType)(sb);

                dependencies.Add(rType, type);
            }
        }
    
        static CacheController<T> GetController<T>() where T : IdentifiableEntity
        {
            var controller = controllers.GetOrThrow(typeof(T), "{0} is not registered in CacheLogic");

            var result = controller as CacheController<T>;

            if (result == null)
                throw new InvalidOperationException("{0} is not registered"); 

            return result;
        }

        private static void InvalidateAllConnectedTypes(Type type, bool includeParentNode)
        {
            var connected = dependencies.IndirectlyRelatedTo(type, includeParentNode);

            foreach (var stype in connected)
            {
                controllers[stype].Invalidate(isClean: false);
            }
        }

       

        public static List<CacheStatistics> Statistics()
        {
            return (from kvp in controllers
                    orderby kvp.Value.Count descending
                    select new CacheStatistics
                    {
                        Type = kvp.Key,
                        Cached = CacheLogic.IsCached(kvp.Key),
                        AttachedEvents = kvp.Value.AttachedEvents,
                        Count = kvp.Value.Count, 
                        Hits = kvp.Value.Hits,
                        Loads = kvp.Value.Loads,
                        Invalidations = kvp.Value.Invalidations,
                    }).ToList();
        }

        public static bool IsCached(Type type)
        {
            return controllers.TryGetC(type) != null;
        }

        public static void InvalidateAll()
        {
            foreach (var item in controllers)
            {
                item.Value.Invalidate(isClean: true);
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

            if (CacheLogic.IsCached(type))
                return Color.Purple;

            if (typeof(MultiEnumDN).IsAssignableFrom(type))
                return Color.Orange;

            if (cacheHint != null && cacheHint(type))
                return Color.Yellow;

            return Color.Green;
        }

    }

    public class CacheStatistics
    {
        public Type Type;
        public bool Cached;
        public int? Count;
        public int Hits;
        public int Loads;
        public int Invalidations;

        public int AttachedEvents;
    }

    public enum InvalidationStrategy
    {
        /// <summary>
        /// 
        /// </summary>
        EngineInvalidation,
    }
}
