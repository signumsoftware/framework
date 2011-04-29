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
        Dictionary<IdentityTuple, IdentifiableEntity> requests = new Dictionary<IdentityTuple, IdentifiableEntity>();
        Dictionary<IdentityTuple, List<Lite>> liteRequests = new Dictionary<IdentityTuple, List<Lite>>();

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
            if (requests.TryGetValue(tuple, out result))
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

            if (requests.TryGetValue(tuple, out ident))
                return (T)ident;

            T entity = Constructor<T>.Call();
            entity.id = id.Value;
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

            if (requests.TryGetValue(tuple, out ident))
                return (T)(IIdentifiable)ident;

            T entity = (T)giConstruct.GetInvoker(type)(id.Value);
 
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
            liteRequests.GetOrCreate(tuple).Add(result);
            return result;
        }

        public void Dispose()
        {
            while (requests.Count > 0)
            {
                var group = requests.GroupBy(a => a.Key.Type, a => a.Key.Id).OrderByDescending(a => a.Count()).First();

                Database.RetrieveList(group.Key, group.ToList());
            }

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
            }

            foreach (var kvp in retrieved)
            {
                IdentifiableEntity entity = kvp.Value;
                entity.PostRetrieving();
                Schema.Current.OnRetrieved(entity);
                entity.Modified = null;
                entity.IsNew = false;

                entityCache.Add(kvp.Key, entity);
            }

            entityCache.ReleaseRetriever(this);
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
            return Request<T>(id);
        }

        public T RequestIBA<T>(int? id, int? typeId) where T : class, IIdentifiable
        {
            return RequestIBA<T>(id, typeId);
        }

        public Lite<T> RequestLiteIBA<T>(int? id, int? typeId) where T : class, IIdentifiable
        {
            return RequestLiteIBA<T>(id, typeId);
        }

        public void Dispose()
        {
            EntityCache.ReleaseRetriever(this);
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
