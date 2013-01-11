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

            int Hits { get; }

            void OnDisabled();
        }

        interface ISemiCacheController
        {
            void SetEntities(Type referingType, DirectedGraph<Modifiable> entities);
        }


        static readonly ThreadVariable<bool> inCache = Statics.ThreadVariable<bool>("inCache");

        static IDisposable SetInCache()
        {
            var oldInCache = inCache.Value;
            inCache.Value = true;
            return new Disposable(() => inCache.Value = oldInCache);
        }

        class SemiCacheController<T> : CacheController<T>, ICacheLogicController, ISemiCacheController
            where T : IdentifiableEntity
        {
            ConcurrentDictionary<Type, Dictionary<int, T>> cachedEntities = new ConcurrentDictionary<Type, Dictionary<int, T>>();
            ConcurrentDictionary<Type, HashSet<Lite<T>>> sensibleLites = new ConcurrentDictionary<Type, HashSet<Lite<T>>>();

            public int? Count
            {
                get { return cachedEntities.Sum(a => a.Value.Count); }
            }

            int invalidations;
            public int Invalidations { get { return invalidations; } }
            int hits;
            public int Hits { get { return hits; } }
            int loads;
            public int Loads { get { return loads; } }

            public void Invalidate(bool isClean)
            {
                cachedEntities.Clear();
                sensibleLites.Clear();
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

            public SemiCacheController(Schema schema)
            {
                var ee = schema.EntityEvents<T>();

                ee.Saving += Saving;
                ee.PreUnsafeUpdate += new QueryHandler<T>(PreUnsafeUpdated);
            }

            void PreUnsafeUpdated(IQueryable<T> query)
            {
                foreach (var kvp in sensibleLites)
                {
                    if (query.Any(e => kvp.Value.Contains(e.ToLite())))
                    {
                        DisableAndInvalidateAllConnected(kvp.Key);
                    }
                }
            }

            void Saving(T ident)
            {
                if (!ident.IsNew && ident.Modified.Value)
                {
                    foreach (var kvp in sensibleLites)
                    {
                        if(kvp.Value.Contains(ident.ToLite()))
                        {
                            DisableAndInvalidateAllConnected(kvp.Key);
                        }
                    }
                }
            }

            private void DisableAndInvalidateAllConnected(Type referingType)
            {
                DisableAllConnectedTypesInTransaction(typeof(T));

                Transaction.PostRealCommit += ud =>
                {
                    Interlocked.Increment(ref invalidations);
                    cachedEntities[referingType].Clear();
                    sensibleLites[referingType].Clear();

                    InvalidateAllConnectedTypes(typeof(T), false);
                };
            }

            public void SetEntities(Type referingType, DirectedGraph<Modifiable> entities)
            {
                Interlocked.Increment(ref loads);

                var dic = entities.OfType<T>().Where(t => t.GetType() == typeof(T)).ToDictionary(a => a.Id);
                cachedEntities[referingType] = dic;

                var lites = dic.Values.Select(a=>a.ToLite()).ToHashSet();
                lites.AddRange(entities.OfType<Lite<T>>().Where(l => l.EntityType == typeof(T)));
                sensibleLites[referingType] = lites;

                var semis = semiControllersFor.TryGetC(typeof(T));
                if (semis != null)
                {
                    foreach (var item in semis)
                    {
                        item.SetEntities(typeof(T), entities);
                    }
                }
            }

            public override bool Enabled
            {
                get { return !GloballyDisabled && !tempDisabled.Value && !IsDisabledInTransaction(typeof(T)); }
            }

            public override bool IsComplete
            {
                get { return false; }
            }

            public override void Load()
            {
                throw NoComplete();
            }

            public override IEnumerable<int> GetAllIds()
            {
                throw NoComplete();
            }

            public override string GetToString(int id)
            {
                throw NoComplete();
            }

            static Exception NoComplete()
            {
                return new InvalidOperationException("Cache for {0} is not complete".Formato(typeof(T)));
            }

            readonly Action<T, T, IRetriever> completer = Completer.GetCompleter<T>();

            public override bool CompleteCache(T entity, IRetriever retriver)
            {
                Interlocked.Increment(ref hits);
                foreach (var item in cachedEntities)
                {
                    if (item.Value.IsEmpty())
                        controllers[item.Key].Load();

                    var orig = item.Value.TryGetC(entity.Id);

                    if (orig != null)
                    {
                        completer(entity, orig, retriver);
                        return true;
                    }
                }

                return false;
            }

            public override event Action Invalidation
            {
                add { throw NoComplete(); }
                remove { throw NoComplete(); }
            }

            public override event Action Disabled
            {
                add { throw NoComplete(); }
                remove { throw NoComplete(); }
            }


            public void OnDisabled()
            {
            }
        }

        class FullCacheController<T> : CacheController<T>, ICacheLogicController
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
            

            public FullCacheController(Schema schema)
            {
                pack = new ResetLazy<Pack>(() =>
                {
                    using (new EntityCache(true))
                    using (ExecutionMode.Global())
                    using (Transaction tr = inCache.Value ? new Transaction(): Transaction.ForceNew())
                    using (SetInCache())
                    using (HeavyProfiler.Log("CACHE"))
                    {
                        DisabledTypesDuringTransaction().Add(typeof(T)); //do not raise Disabled event

                        List<T> result = Database.Query<T>().ToList();

                        var semis = semiControllersFor.TryGetC(typeof(T));
                        if (semis != null)
                        {
                            var graph = GraphExplorer.FromRootsIdentifiable(result);

                            foreach (var item in semis)
                            {
                                item.SetEntities(typeof(T), graph);
                            }
                        }

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
                DisableAndInvalidateAllConnected();
            }

            void PreUnsafeDelete(IQueryable<T> query)
            {
                DisableAndInvalidate();
            }

            void Saving(T ident)
            {
                if (ident.Modified.Value)
                {
                    if (ident.IsNew)
                    {
                        DisableAndInvalidate();
                    }
                    else
                    {
                        DisableAndInvalidateAllConnected();
                    }
                }
            }


            void DisableAndInvalidate()
            {
                DisableTypeInTransaction(typeof(T));
                Transaction.PostRealCommit += ud => Invalidate(false);
            }

          

            private void DisableAndInvalidateAllConnected()
            {
                DisableAllConnectedTypesInTransaction(typeof(T));

                Transaction.PostRealCommit += ud =>
                {
                    InvalidateAllConnectedTypes(typeof(T), true);
                }; 
            }


            public override bool Enabled
            {
                get { return !GloballyDisabled && !tempDisabled.Value && !IsDisabledInTransaction(typeof(T));  }
            }

            public override bool IsComplete
            {
                get { return true; }
            }

            public int? Count
            {
                get { return pack.IsValueCreated ? pack.Value.List.Count : (int?)null; }
            }

            int invalidations;
            public int Invalidations { get { return invalidations; } }
            int hits;
            public int Hits { get { return hits; } }
            int loads;
            public int Loads { get { return loads; } }

            public override event Action Invalidation;
            public override event Action Disabled;


            public void Invalidate(bool isClean)
            {
                pack.Reset();
                if (Invalidation != null)
                    Invalidation();

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

            public override void Load()
            {
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
                if(origin == null)
                    throw new EntityNotFoundException(typeof(T), entity.Id);
                completer(entity, origin, retriver);
                return true;
            }


            public void OnDisabled()
            {
                if (Disabled != null)
                    Disabled();
            }
        }

        static Dictionary<Type, ICacheLogicController> controllers = new Dictionary<Type, ICacheLogicController>(); //CachePack

        static DirectedGraph<Type> invalidations = new DirectedGraph<Type>();

        static Dictionary<Type, List<ISemiCacheController>> semiControllersFor = new Dictionary<Type, List<ISemiCacheController>>();

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
            var cc = new FullCacheController<T>(sb.Schema);
            controllers.AddOrThrow(typeof(T), cc, "{0} already registered");

            TryCacheSubTables(typeof(T), sb);
        }

        public static void SemiCacheTable<T>(SchemaBuilder sb) where T : IdentifiableEntity
        {
            var cc = new SemiCacheController<T>(sb.Schema);
            controllers.AddOrThrow(typeof(T), cc, "{0} already registered");

            TryCacheSubTables(typeof(T), sb);
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
                    giCacheTable.GetInvoker(rType)(sb); ;

                var ic = controllers[rType] as ISemiCacheController;
                if (ic != null && rType != type) //Cycles of semis could create StackOverflow, but the only possible cycle is an auto-cycle
                    semiControllersFor.GetOrCreate(type).Add(ic);

                invalidations.Add(rType, type);
            }
        }
    
        static FullCacheController<T> GetController<T>() where T : IdentifiableEntity
        {
            var controller = controllers.GetOrThrow(typeof(T), "{0} is not registered in CacheLogic");

            var result = controller as FullCacheController<T>;

            if (result == null)
                throw new InvalidOperationException("{0} is not registered"); 

            return result;
        }

        private static void InvalidateAllConnectedTypes(Type type, bool includeParentNode)
        {
            var connected = invalidations.IndirectlyRelatedTo(type, includeParentNode);

            foreach (var stype in connected)
            {
                controllers[stype].Invalidate(false);
            }
        }

       

        public static List<CacheStatistics> Statistics()
        {
            return (from kvp in controllers
                    orderby kvp.Value.Count descending
                    select new CacheStatistics
                    {
                        Type = kvp.Key,
                        CacheType = CacheLogic.GetCacheType(kvp.Key).Value,
                        Count = kvp.Value.Count, 
                        Hits = kvp.Value.Hits,
                        Loads = kvp.Value.Loads,
                        Invalidations = kvp.Value.Invalidations,
                    }).ToList();
        }

        public static CacheType? GetCacheType(Type type)
        {
            var controller = controllers.TryGetC(type);

            if (controller == null)
                return null;

            if(controller.GetType().IsInstantiationOf(typeof(FullCacheController<>)))
                return CacheType.Cached;

            if (controller.GetType().IsInstantiationOf(typeof(SemiCacheController<>)))
                return CacheType.Semi;

            throw new InvalidOperationException("Not expected");
        }

        public static void InvalidateAll()
        {
            foreach (var item in controllers)
            {
                item.Value.Invalidate(true);
            }
        }

        public static void OnInvalidation<T>(Action action) where T : IdentifiableEntity
        {
            GetController<T>().Invalidation += action;
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

            var ct = CacheLogic.GetCacheType(type);
            if (ct == CacheType.Cached)
                return Color.Purple;

            if (ct == CacheType.Semi)
                return Color.Pink;

            if (typeof(MultiEnumDN).IsAssignableFrom(type))
                return Color.Orange;

            if (cacheHint != null && cacheHint(type))
                return Color.Yellow;

            return Color.Green;
        }
    }

    public enum CacheType
    {
        Cached,
        Semi, 
    }

    public class CacheStatistics
    {
        public Type Type;
        public CacheType CacheType;
        public int? Count;
        public int Hits;
        public int Loads;
        public int Invalidations; 
    }
}
