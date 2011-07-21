using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Engine.Maps;
using System.Linq.Expressions;
using Signum.Utilities;
using Signum.Utilities.Reflection;

namespace Signum.Engine
{
    interface IRetriever : IDisposable
    {
        T Cached<T>(int? id, Action<T> complete) where T : IdentifiableEntity;
        T Request<T>(int? id) where T : IdentifiableEntity;
        T RequestIBA<T>(int? id, int? typeId) where T : class, IIdentifiable;
        Lite<T> RequestLiteIBA<T>(int? id, int? typeId) where T : class, IIdentifiable;
        IRetriever Parent { get; }

        void ForceAddRequests<T>(IEnumerable<T> entities) where T : IdentifiableEntity;
    }

    class RealRetriever : IRetriever
    {
        public IRetriever Parent
        {
            get { return null; }
        }

        public RealRetriever(EntityCache.RealEntityCache entityCache)
        {
            this.entityCache = entityCache;
        }

        EntityCache.RealEntityCache entityCache;
        Dictionary<IdentityTuple, IdentifiableEntity> retrieved = new Dictionary<IdentityTuple, IdentifiableEntity>();
        Dictionary<IdentityTuple, IdentifiableEntity> requests;
        Dictionary<IdentityTuple, List<Lite>> liteRequests; 
        HashSet<IdentityTuple> fromCache;
        HashSet<ICacheController> cacheControllers;

        public T Cached<T>(int? id, Action<T> complete) where T : IdentifiableEntity
        {
            if (id == null)
                return null;

            IdentityTuple tuple = new IdentityTuple(typeof(T), id.Value);

            IdentifiableEntity result;
            if (entityCache.TryGetValue(tuple, out result))
                return (T)result;

            if (retrieved.TryGetValue(tuple, out result))
                return (T)result;

            T entity;
            if (requests != null && requests.TryGetValue(tuple, out result))
            {
                entity = (T)result;
                requests.Remove(tuple);
            }
            else
            {
                entity = Constructor<T>.Call();
                entity.id = id.Value;
            }

            complete(entity);
            retrieved.Add(tuple, entity);

            return entity;
        }


        public T Request<T>(int? id) where T : IdentifiableEntity
        {
            if (id == null)
                return null;

            IdentityTuple tuple = new IdentityTuple(typeof(T), id.Value);

            IdentifiableEntity ident;
            if (entityCache.TryGetValue(tuple, out ident))
                return (T)ident;

            if (retrieved.TryGetValue(tuple, out ident))
                return (T)ident;

            if (requests != null && requests.TryGetValue(tuple, out ident))
                return (T)ident;

            var cc = Schema.Current.CacheController<T>();
            if (cc != null)
            {
                var result = cc.GetOrRequest(id.Value);
                retrieved.Add(tuple, result);
                if (fromCache == null)
                    fromCache = new HashSet<IdentityTuple>();
                fromCache.Add(tuple);

                if (cacheControllers == null)
                    cacheControllers = new HashSet<ICacheController>();
                cacheControllers.Add(cc);
                return result;
            }

            T entity = Constructor<T>.Call();
            entity.id = id.Value;
            if (requests == null)
                requests = new Dictionary<IdentityTuple, IdentifiableEntity>();

            requests.Add(tuple, entity);
            return entity;
        }

        public T RequestIBA<T>(int? id, int? typeId) where T : class, IIdentifiable
        {
            if (id == null)
                return null;

            Type type = Schema.Current.IdToType[typeId.Value];

            IdentityTuple tuple = new IdentityTuple(type, id.Value);

            IdentifiableEntity ident;
            if (entityCache.TryGetValue(tuple, out ident))
                return (T)(IIdentifiable)ident;

            if (retrieved.TryGetValue(tuple, out ident))
                return (T)(IIdentifiable)ident;

            if (requests != null && requests.TryGetValue(tuple, out ident))
                return (T)(IIdentifiable)ident;

            var cc = Schema.Current.CacheController(type);
            if (cc != null && cc.Enabled)
            {
                var result = cc.GetOrRequest(id.Value); 
                retrieved.Add(tuple, result);

                if (fromCache == null)
                    fromCache = new HashSet<IdentityTuple>();
                fromCache.Add(tuple);

                if (cacheControllers == null)
                    cacheControllers = new HashSet<ICacheController>();

                cacheControllers.Add(cc);
                return (T)(IIdentifiable)result;
            }

            T entity = (T)giConstruct.GetInvoker(type)(id.Value);

            if (requests == null)
                requests = new Dictionary<IdentityTuple, IdentifiableEntity>(); 

            requests.Add(tuple, (IdentifiableEntity)(IIdentifiable)entity);

            return entity;
        }

        static GenericInvoker<Func<int, IIdentifiable>> giConstruct = new GenericInvoker<Func<int, IIdentifiable>>(id => Construct<TypeDN>(id));
        static T Construct<T>(int id) where T:IdentifiableEntity
        {
            var entity = Constructor<T>.Call();
            entity.id = id;
            return entity;
        }

        public Lite<T> RequestLiteIBA<T>(int? id, int? typeId) where T : class, IIdentifiable
        {
            if (id == null)
                return null; 

            Type type = Schema.Current.IdToType[typeId.Value];
            IdentityTuple tuple = new IdentityTuple(type, id.Value);
            Lite<T> result = new Lite<T>(type, id.Value);
            if (liteRequests == null)
                liteRequests = new Dictionary<IdentityTuple, List<Lite>>();
            liteRequests.GetOrCreate(tuple).Add(result);
            return result;
        }

        public void Dispose()
        {
            if (requests != null)
            {
                while (requests.Count > 0)
                {
                    var group = requests.GroupBy(a => a.Key.Type, a => a.Key).OrderByDescending(a => a.Count()).First();

                    Database.RetrieveList(group.Key, group.Select(t => t.Id).ToList());

                    requests.RemoveRange(group);
                }
            }

            if (liteRequests != null)
            {
                while (liteRequests.Count > 0)
                {
                    var group = liteRequests.GroupBy(a => a.Key.Type).OrderByDescending(a => a.Count()).First();

                    var lites = Database.RetrieveListLite(group.Key, group.Select(a => a.Key.Id).ToList());

                    foreach (var pair in group.Join(lites, g => g.Key.Id, l => l.Id, (g, l) => new { List = g.Value, l.ToStr }))
                    {
                        foreach (var lite in pair.List)
                        {
                            lite.ToStr = pair.ToStr;
                        }
                    }

                    liteRequests.RemoveRange(group.Select(a => a.Key));
                }
            }

            if (cacheControllers != null)
            {
                foreach (var cc in cacheControllers)
                {
                    cc.Load();
                }
            }

            foreach (var kvp in retrieved)
            {
                IdentifiableEntity entity = kvp.Value;
                entity.PostRetrieving();
                Schema.Current.OnRetrieved(entity, fromCache == null ? false : fromCache.Contains(kvp.Key));
                entity.Modified = null;
                entity.IsNew = false;

                entityCache.Add(kvp.Key, entity);
            }

            entityCache.ReleaseRetriever(this);
        }


        public void ForceAddRequests<T>(IEnumerable<T> entities) where T : IdentifiableEntity
        {
            if (requests != null)
                throw new InvalidOperationException("requests should be null");

            requests = entities.ToDictionary(e => new IdentityTuple(e), e => (IdentifiableEntity)e);
        }
    }

    class ChildRetriever : IRetriever
    {
        EntityCache.RealEntityCache Cache;
        public IRetriever Parent { get; set; }
        public ChildRetriever(IRetriever parent, EntityCache.RealEntityCache cache)
        {
            this.Parent = parent;
            this.Cache = cache;
        }

        public T Cached<T>(int? id, Action<T> complete) where T : IdentifiableEntity
        {
            return Parent.Cached<T>(id, complete);
        }

        public T Request<T>(int? id) where T : IdentifiableEntity
        {
            return Parent.Request<T>(id);
        }

        public T RequestIBA<T>(int? id, int? typeId) where T : class, IIdentifiable
        {
            return Parent.RequestIBA<T>(id, typeId);
        }

        public Lite<T> RequestLiteIBA<T>(int? id, int? typeId) where T : class, IIdentifiable
        {
            return Parent.RequestLiteIBA<T>(id, typeId);
        }

        public void Dispose()
        {
            EntityCache.ReleaseRetriever(this);
        }


        public void ForceAddRequests<T>(IEnumerable<T> entities) where T : IdentifiableEntity
        {
            throw new InvalidOperationException("ForceAddRequests works with RealRetriever only"); 
        }
    }

    static class Constructor<T> where T:IdentifiableEntity
    {
        static Func<T> call;
        public static Func<T> Call
        {
            get
            {
                if (call == null)
                    call = Expression.Lambda<Func<T>>(Expression.New(typeof(T))).Compile();
                return call;
            }
        }
    }
}
