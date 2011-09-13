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

namespace Signum.Engine.Cache
{
    public static class CacheLogic
    {
        interface ICacheLogicController
        {
            int? Count { get; }

            void Invalidate(); 
        }

        interface IInstanceController
        {
            void SetEntities(Type referingType, DirectedGraph<Modifiable> entities);
        }

        class SemiCached<T> : CacheController<T>, ICacheLogicController, IInstanceController
            where T : IdentifiableEntity
        {
            Dictionary<Type, Dictionary<int, T>> cachedEntities = new Dictionary<Type, Dictionary<int, T>>();
            Dictionary<Type, HashSet<Lite<T>>> sensibleLites = new Dictionary<Type, HashSet<Lite<T>>>();

            public int? Count
            {
                get { return cachedEntities.Sum(a => a.Value.Count); }
            }

            public void Invalidate()
            {
                cachedEntities.Clear();
                sensibleLites.Clear();
            }

            public SemiCached(Schema schema)
            {
                var ee = schema.EntityEvents<T>();

                ee.Saving += Saving;
                ee.PreUnsafeUpdated += new QueryHandler<T>(PreUnsafeUpdated);
            }

            void PreUnsafeUpdated(IQueryable<T> query)
            {
                foreach (var kvp in sensibleLites)
                {
                    if (query.Any(e => kvp.Value.Contains(e.ToLite())))
                    {
                        Transaction.RealCommit += () => InvalidateAll(kvp.Key);
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
                            Transaction.RealCommit += ()=>InvalidateAll(kvp.Key);
                        }
                    }
                }
            }

            private void InvalidateAll(Type referingType)
            {
                cachedEntities.Remove(referingType);
                sensibleLites.Remove(referingType);
                CacheLogic.InvalidateAllConnected(referingType);
            }

            public void SetEntities(Type referingType, DirectedGraph<Modifiable> entities)
            {
                var dic = entities.OfType<T>().ToDictionary(a => a.Id);
                cachedEntities[referingType] = dic;

                var lites = dic.Values.Select(a=>a.ToLite()).ToHashSet();
                lites.AddRange(entities.OfType<Lite>().Where(l=>l.RuntimeType == typeof(T)).Select(l=>l.ToLite<T>()));
                sensibleLites[referingType] = lites;

                var semis = addEntities.TryGetC(typeof(T));
                if (semis == null)
                    return;

                foreach (var item in semis)
                {
                    item.SetEntities(typeof(T), entities); 
                }
            }

            public override bool Enabled
            {
                get { return !GloballyDisabled && !TemporallyDisabled; }
            }


            public override bool IsComplete
            {
                get { return false; }
            }

            public override bool Load()
            {
                throw NoComplete();
            }

            public override IEnumerable<int> GetAllIds()
            {
                throw NoComplete();
            }

            public override Lite<T> RetriveLite(int id)
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
                foreach (var item in cachedEntities)
                {
                    var orig = item.Value.TryGetC(entity.Id);

                    if (orig != null)
                    {
                        completer(entity, orig, retriver);
                        return true;
                    }
                }

                return false;
            }
        }
  
        class CacheLogicController<T> : CacheController<T>, ICacheLogicController
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
         
            Lazy<Pack> pack;

            public CacheLogicController(Schema schema)
            {
                pack = new Lazy<Pack>(() =>
                {
                    using (new EntityCache(true))
                    using (DisableThis())
                    using (Schema.Current.GlobalMode())
                    using (HeavyProfiler.Log("CACHE"))
                    {
                        List<T> result = Database.Query<T>().ToList();

                        var semis = addEntities.TryGetC(typeof(T));
                        if (semis != null)
                        {
                            var graph = GraphExplorer.FromRootsIdentifiable(result);

                            foreach (var item in semis)
                            {
                                item.SetEntities(typeof(T), graph);
                            }
                        }

                        return new Pack(result);
                    }
                }, LazyThreadSafetyMode.PublicationOnly);

                var ee = schema.EntityEvents<T>();

                ee.CacheController = this;
                ee.Saving += Saving;
                ee.PreUnsafeDelete += PreUnsafeDelete;
                ee.PreUnsafeUpdated += UnsafeUpdated;
            }

            bool disabledThis; 
            public IDisposable DisableThis()
            {
                bool old = disabledThis;
                disabledThis = true;
                return new Disposable(() => disabledThis = old); 
            }

            void UnsafeUpdated(IQueryable<T> query)
            {
                Transaction.RealCommit -= InvalidateAllConnected;
                Transaction.RealCommit += InvalidateAllConnected;
            }

            void PreUnsafeDelete(IQueryable<T> query)
            {
                Transaction.RealCommit -= Invalidate;
                Transaction.RealCommit += Invalidate;
            }

            void Saving(T ident)
            {
                if (ident.Modified.Value)
                {
                    if (ident.IsNew)
                    {
                        Transaction.RealCommit -= Invalidate;
                        Transaction.RealCommit += Invalidate;
                    }
                    else
                    {
                        Transaction.RealCommit -= InvalidateAllConnected;
                        Transaction.RealCommit += InvalidateAllConnected;
                    }
                }
            }

            private void InvalidateAllConnected()
            {
                CacheLogic.InvalidateAllConnected(typeof(T));
            }

            public override bool Enabled
            {
                get { return !GloballyDisabled && !disabledThis && !TemporallyDisabled; }
            }

            public override bool IsComplete
            {
                get { return true; }
            }

            public int? Count
            {
                get { return pack.IsValueCreated ? pack.Value.List.Count : (int?)null; }
            }

            public void Invalidate()
            {
                pack.ResetPublicationOnly();
            }

            public override bool Load()
            {
                if (pack.IsValueCreated)
                    return false;

                pack.Load();
                return true;
            }

            public override IEnumerable<int> GetAllIds()
            {
                return pack.Value.Dictionary.Keys;
            }

            public override Lite<T> RetriveLite(int id) 
            {
                return pack.Value.Dictionary[id].ToLite<T>(); 
            }

            readonly Action<T, T, IRetriever> completer = Completer.GetCompleter<T>();

            public override bool CompleteCache(T entity, IRetriever retriver)
            {
                var origin = pack.Value.Dictionary.TryGetC(entity.Id);
                if(origin == null)
                    throw new EntityNotFoundException(typeof(T), entity.Id);
                completer(entity, origin, retriver);
                return true;
            }
        }

        static Dictionary<Type, ICacheLogicController> controllers = new Dictionary<Type, ICacheLogicController>(); //CachePack

        static DirectedGraph<Type> invalidations = new DirectedGraph<Type>();

        static Dictionary<Type, List<IInstanceController>> addEntities = new Dictionary<Type, List<IInstanceController>>();

        public static bool GloballyDisabled { get; set; }

        [ThreadStatic]
        static bool TemporallyDisabled;

        public static IDisposable Disable()
        {
            var oldDisabled = TemporallyDisabled;
            TemporallyDisabled = true;
            return new Disposable(() => TemporallyDisabled = oldDisabled); 
        }

        static GenericInvoker<Action<SchemaBuilder>> giCacheTable = new GenericInvoker<Action<SchemaBuilder>>(sb => CacheTable<IdentifiableEntity>(sb));
        public static void CacheTable<T>(SchemaBuilder sb) where T : IdentifiableEntity
        {
            var cc = new CacheLogicController<T>(sb.Schema);
            controllers.AddOrThrow(typeof(T), cc, "{0} already registered");

            TryCacheSubTables(typeof(T), sb);
        }

        public static void SemiCacheTable<T>(SchemaBuilder sb) where T : IdentifiableEntity
        {
            var cc = new SemiCached<T>(sb.Schema);
            controllers.AddOrThrow(typeof(T), cc, "{0} already registered");

            TryCacheSubTables(typeof(T), sb);
        }

        private static void TryCacheSubTables(Type type, SchemaBuilder sb)
        {


            List<Type> relatedTypes = sb.Schema.Table(type).DependentTables().Where(kvp =>!kvp.Key.Type.IsInstantiationOf(typeof(EnumProxy<>))).Select(t => t.Key.Type).ToList();

            foreach (var rType in relatedTypes)
            {
                if (!controllers.ContainsKey(rType))
                    giCacheTable.GetInvoker(rType)(sb); ;

                var ic = controllers[rType] as IInstanceController;
                if (ic != null)
                    addEntities.GetOrCreate(type).Add(ic);

                invalidations.Add(rType, type);
            }
        }
    
        static CacheLogicController<T> GetController<T>() where T : IdentifiableEntity
        {
            var controller = controllers.GetOrThrow(typeof(T), "{0} is not registered in CacheLogic");

            var result = controller as CacheLogicController<T>;

            if (result == null)
                throw new InvalidOperationException("{0} is not registered"); 

            return result;
        }

        public static void InvalidateAllConnected(Type type) 
        {
            controllers[type].Invalidate();

            foreach (var stype in invalidations.IndirectlyRelatedTo(type))
            {
                controllers[stype].Invalidate();
            }
        }

        public static List<CacheStatistics> Statistics()
        {
            return (from kvp in controllers
                    orderby kvp.Value.Count descending
                    select new CacheStatistics
                    {
                        Type = kvp.Key,
                        CacheType = CacheLogic.GetCacheController(kvp.Key).Value,
                        Count = kvp.Value.Count, 
                    }).ToList();
        }

        public static CacheControllerType? GetCacheController(Type type)
        {
            var controller = controllers.TryGetC(type);

            if (controller == null)
                return null;

            if(controller.GetType().IsInstantiationOf(typeof(CacheLogicController<>)))
                return CacheControllerType.Cached;

            if (controller.GetType().IsInstantiationOf(typeof(SemiCached<>)))
                return CacheControllerType.Semi;

            throw new InvalidOperationException("Not expected");
        }

        public static void InvalidateAll()
        {
            foreach (var item in controllers)
            {
                item.Value.Invalidate();
            }
        }
    }

    public enum CacheControllerType
    {
        Cached,
        Semi,
        NotCached, 
    }

    public class CacheStatistics
    {
        public Type Type;
        public CacheControllerType CacheType;
        public int? Count;
    }
}
