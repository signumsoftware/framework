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

namespace Signum.Engine
{
    public static class CacheLogic
    {
        interface ICacheController
        {
            int? Count { get; }

            void Invalidate();
        }

        class CacheLogicController<T> : CacheController<T>, ICacheController where T : IdentifiableEntity
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

            public CacheLogicController()
            {
                pack = new Lazy<Pack>(() =>
                { 
                    using(new EntityCache(true))
                    using (Disable())
                    using(Schema.Current.GlobalMode())
                    {
                        if (requests == null || requests.IsEmpty())
                        {
                            return new Pack(Database.Query<T>().ToList());
                        }
                        else
                        {
                            using (IRetriever retriever = EntityCache.NewRetriever())
                            {
                                retriever.ForceAddRequests(requests.Values);
                                requests = null;
                                return new Pack(Database.Query<T>().ToList());
                            }
                        }
                    }
                }, LazyThreadSafetyMode.PublicationOnly);
            }

            public override bool Enabled
            {
                get { return !TemporallyDisabled; }
            }

            public override List<T> GetAllEntities()
            {
                if (TemporallyDisabled)
                    throw new InvalidOperationException("Cache Disabled");

                return pack.Value.List;
            }

            public override T GetEntity(int id)
            {
                if (TemporallyDisabled)
                    throw new InvalidOperationException("Cache Disabled");

                T result;
                if (pack.Value.Dictionary.TryGetValue(id, out result))
                    return result;

                throw new EntityNotFoundException(typeof(T), id);
            }

            public override List<T> GetEntitiesList(List<int> ids)
            {
                if (TemporallyDisabled)
                    throw new InvalidOperationException("Cache Disabled");

                try
                {
                    return ids.Select(id => pack.Value.Dictionary[id]).ToList();
                }
                catch (KeyNotFoundException)
                {
                    throw new EntityNotFoundException(typeof(T), ids.Where(i => !pack.Value.Dictionary.ContainsKey(i)).ToArray());
                }
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

            ConcurrentDictionary<int, T> requests;  

            public override T GetOrRequest(int id)
            {
                if (pack.IsValueCreated)
                    return GetEntity(id);

                if (requests == null)
                    requests = new ConcurrentDictionary<int, T>();

                return requests.GetOrAdd(id, i =>
                {
                    var result = Constructor<T>.Call();
                    result.id = i;
                    return result;
                });
            }
        }

        static Dictionary<Type, ICacheController> cached = new Dictionary<Type, ICacheController>(); //CachePack

        [ThreadStatic]
        static bool TemporallyDisabled;

        public static IDisposable Disable()
        {
            var oldDisabled = TemporallyDisabled;
            TemporallyDisabled = true;
            return new Disposable(() => TemporallyDisabled = oldDisabled); 
        }

        public static void Register<T>(SchemaBuilder sb) where T : IdentifiableEntity
        {
            var cc = new CacheLogicController<T>();
            cached.Add(typeof(T), cc); 

            sb.Schema.EntityEvents<T>().CacheController = cc;
            sb.Schema.EntityEvents<T>().Saving += CacheLogic_Saving<T>;
            sb.Schema.EntityEvents<T>().PreUnsafeDelete += CacheLogic_PreUnsafeDelete<T>;
        }

     

        public static void Unregister<T>(SchemaBuilder sb) where T : IdentifiableEntity
        {
            cached.Remove(typeof(T));

            sb.Schema.EntityEvents<T>().CacheController = null;
            sb.Schema.EntityEvents<T>().Saving -= CacheLogic_Saving<T>;
            sb.Schema.EntityEvents<T>().PreUnsafeDelete -= CacheLogic_PreUnsafeDelete<T>;
        }

        static void CacheLogic_PreUnsafeDelete<T>(IQueryable<T> query) where T : IdentifiableEntity
        {
            Transaction.RealCommit += () => Invalidate<T>();
        }

        static void CacheLogic_Saving<T>(T ident) where T:IdentifiableEntity
        {
            if (ident.Modified.Value)
                Transaction.RealCommit += () => Invalidate<T>();
        }

        static CacheLogicController<T> Cache<T>() where T : IdentifiableEntity
        {
            return (CacheLogicController<T>)cached.GetOrThrow(typeof(T), "{0} is not registered in CacheLogic");
        }

        public static void Invalidate<T>() where T : IdentifiableEntity
        {
            Cache<T>().Invalidate();
        }

        public static string Statistics()
        {
            return (from kvp in cached
                    orderby kvp.Value.Count descending
                    select "{0} {1}".Formato(kvp.Value.Count.TryToString()?? "-", kvp.Key.Name)).ToString("\r\n");
        }

        public static List<T> RetrieveAllCached<T>() where T:IdentifiableEntity
        {
            return Cache<T>().List;
        }

        public static T RetrieveCached<T>(int id) where T : IdentifiableEntity
        {
            T result;
            if (Cache<T>().Dictionary.TryGetValue(id, out result))
                return result;

            throw new EntityNotFoundException(typeof(T), id); 
        }
    }
}
