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

namespace Signum.Engine.Cache
{
    public static class CacheLogic
    {
        interface ICacheLogicController : ICacheController
        {
            int? Count { get; }

            void Invalidate(bool isClean);

            int Invalidations { get; }
            int Loads { get; }

            int RegisteredEvents { get; }

            int Hits { get; }

            void OnDisabled();
        }

        class CacheController<T> : CacheControllerBase<T>, ICacheLogicController
                where T : IdentifiableEntity
        {
            class Pack
            {
                public Pack(List<T> list)
                {
                    this.List = list;
                    this.Dictionary = list.ToDictionary(a => a.Id);
                }

                public readonly List<T> List;
                public readonly Dictionary<int, T> Dictionary;
            }

            public List<T> List { get { return pack.Value.List; } }
            public Dictionary<int, T> Dictionary { get { return pack.Value.Dictionary; } }

            ResetLazy<Pack> pack;

            public CacheController(Schema schema)
            {
                pack = new ResetLazy<Pack>(() =>
                {
                    using (new EntityCache(true))
                    using (ExecutionMode.Global())
                    using (Transaction tr = inCache.Value ? new Transaction() : Transaction.ForceNew())
                    using (CacheLogic.SetInCache())
                    using (HeavyProfiler.Log("CACHE"))
                    using (Connector.NotifyQueryChange((sender, aggs) => this.Invalidate(isClean: false)))
                    {
                        DisabledTypesDuringTransaction().Add(typeof(T)); //do not raise Disabled event

                        List<T> result = Database.Query<T>().ToList();

                        Interlocked.Increment(ref loads);

                        return tr.Commit(new Pack(result));
                    }
                });

                var ee = schema.EntityEvents<T>();

                ee.CacheController = this;
                ee.Saving += Saving;
                ee.PreUnsafeDelete += PreUnsafeDelete;
                ee.PreUnsafeUpdate += UnsafeUpdated;
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

            public override bool IsComplete
            {
                get { return true; }
            }

            public int? Count
            {
                get { return pack.IsValueCreated ? pack.Value.List.Count : (int?)null; }
            }

            public int RegisteredEvents
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
                var eh = Connector.CurrentOnQueryChange;

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
                return pack.Value.Dictionary.Keys;
            }

            public override string GetToString(int id)
            {
                Interlocked.Increment(ref hits);
                return pack.Value.Dictionary[id].ToString();
            }

            readonly Action<T, T, IRetriever> completer = Completer.GetCompleter<T>();

            public override bool CompleteCache(T entity, IRetriever retriver)
            {
                Interlocked.Increment(ref hits);
                var origin = pack.Value.Dictionary.TryGetC(entity.Id);
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


        static readonly ThreadVariable<bool> inCache = Statics.ThreadVariable<bool>("inCache");

        static IDisposable SetInCache()
        {
            var oldInCache = inCache.Value;
            inCache.Value = true;
            return new Disposable(() => inCache.Value = oldInCache);
        }

        static Dictionary<Type, ICacheLogicController> controllers = new Dictionary<Type, ICacheLogicController>(); //CachePack

        static DirectedGraph<Type> invalidations = new DirectedGraph<Type>();

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
            var connected = invalidations.IndirectlyRelatedTo(type, true);

            var hs = DisabledTypesDuringTransaction();

            foreach (var stype in connected)
            {
                hs.Add(stype);
                controllers[stype].OnDisabled();
            }
        }


        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                PermissionAuthLogic.RegisterTypes(typeof(CachePermission));
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

            invalidations.Add(type); 

            foreach (var rType in relatedTypes)
            {
                if (!controllers.ContainsKey(rType))
                    giCacheTable.GetInvoker(rType)(sb);

                invalidations.Add(rType, type);
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
            var connected = invalidations.IndirectlyRelatedTo(type, includeParentNode);

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
    }

    public enum InvalidationStrategy
    {
        /// <summary>
        /// 
        /// </summary>
        EngineInvalidation,
    }
}
