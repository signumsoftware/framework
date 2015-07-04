using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Engine.Maps;
using System.Linq.Expressions;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using Signum.Engine.Basics;

namespace Signum.Engine
{
    public interface IRetriever : IDisposable
    {
        T Complete<T>(PrimaryKey? id, Action<T> complete) where T : Entity;
        T Request<T>(PrimaryKey? id) where T : Entity;
        T RequestIBA<T>(PrimaryKey? typeId, string id) where T : class, IEntity;
        Lite<T> RequestLite<T>(Lite<T> lite) where T : class, IEntity;
        T ModifiablePostRetrieving<T>(T entity) where T : Modifiable;
        IRetriever Parent { get; }

        ModifiedState ModifiedState { get; }
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
        Dictionary<IdentityTuple, Entity> retrieved = new Dictionary<IdentityTuple, Entity>();
        Dictionary<Type, Dictionary<PrimaryKey, Entity>> requests;
        Dictionary<IdentityTuple, List<Lite<IEntity>>> liteRequests;
        List<Modifiable> modifiablePostRetrieving = new List<Modifiable>();

        bool TryGetRequest(IdentityTuple key, out Entity value)
        {
            Dictionary<PrimaryKey, Entity> dic;
            if (requests != null && requests.TryGetValue(key.Type, out dic) && dic.TryGetValue(key.Id, out value))
                return true;

            value = null;
            return false;
        }

        public T Complete<T>(PrimaryKey? id, Action<T> complete) where T : Entity
        {
            if (id == null)
                return null;

            IdentityTuple tuple = new IdentityTuple(typeof(T), id.Value);

            Entity result;
            if (entityCache.TryGetValue(tuple, out result))
                return (T)result;

            if (retrieved.TryGetValue(tuple, out result))
                return (T)result;

            T entity;
            if (TryGetRequest(tuple, out result))
            {
                entity = (T)result;
                requests[typeof(T)].Remove(id.Value);
            }
            else
            {
                entity = EntityCache.Construct<T>(id.Value);
            }

            retrieved.Add(tuple, entity);
            complete(entity);

            return entity;
        }

        static GenericInvoker<Func<RealRetriever, PrimaryKey?, Entity>> giRequest =
            new GenericInvoker<Func<RealRetriever, PrimaryKey?, Entity>>((rr, id) => rr.Request<Entity>(id));
        public T Request<T>(PrimaryKey? id) where T : Entity
        {
            if (id == null)
                return null;

            IdentityTuple tuple = new IdentityTuple(typeof(T), id.Value);

            Entity ident;
            if (entityCache.TryGetValue(tuple, out ident))
                return (T)ident;

            if (retrieved.TryGetValue(tuple, out ident))
                return (T)ident;

            ident = (T)requests?.TryGetC(typeof(T))?.TryGetC(id.Value);
            if (ident != null)
                return (T)ident;

            T entity = EntityCache.Construct<T>(id.Value);
            if (requests == null)
                requests = new Dictionary<Type, Dictionary<PrimaryKey, Entity>>();

            requests.GetOrCreate(tuple.Type).Add(tuple.Id, entity);

            return entity;
        }

        public T RequestIBA<T>(PrimaryKey? typeId, string id) where T : class, IEntity
        {
            if (id == null)
                return null;

            Type type = TypeLogic.IdToType[typeId.Value];

            var parsedId = PrimaryKey.Parse(id, type);

            return (T)(IEntity)giRequest.GetInvoker(type)(this, parsedId);
        }

        public Lite<T> RequestLite<T>(Lite<T> lite) where T : class, IEntity
        {
            if (lite == null)
                return null;

            IdentityTuple tuple = new IdentityTuple(lite.EntityType, lite.Id);
            if (liteRequests == null)
                liteRequests = new Dictionary<IdentityTuple, List<Lite<IEntity>>>();
            liteRequests.GetOrCreate(tuple).Add(lite);
            return lite;
        }

        public T ModifiablePostRetrieving<T>(T modifiable) where T : Modifiable
        {
            if (modifiable != null)
                modifiablePostRetrieving.Add(modifiable);

            return modifiable;
        }

        public void Dispose()
        {
        retry:
            if (requests != null)
            {
                while (requests.Count > 0)
                {
                    var group = requests.WithMax(a => a.Value.Count);

                    var dic = group.Value;
                    ICacheController cc = Schema.Current.CacheController(group.Key);

                    if (cc != null && cc.Enabled)
                    {
                        cc.Load();

                        while (dic.Count > 0)
                        {
                            Entity ident = dic.Values.FirstEx();

                            cc.Complete(ident, this);

                            retrieved.Add(new IdentityTuple(ident), ident);
                            dic.Remove(ident.Id);
                        }
                    }
                    else
                    {
                        Database.RetrieveList(group.Key, dic.Keys.ToList());
                    }

                    if (dic.Count == 0)
                        requests.Remove(group.Key);
                }
            }

            if (liteRequests != null)
            {

                {
                    List<IdentityTuple> toRemove = null;
                    foreach (var item in liteRequests)
                    {
                        var entity = retrieved.TryGetC(item.Key);
                        if (entity != null)
                        {
                            var toStr = entity.ToString();

                            foreach (var lite in item.Value)
                                lite.SetToString(toStr);

                            if (toRemove == null)
                                toRemove = new List<IdentityTuple>();

                            toRemove.Add(item.Key);
                        }
                    }

                    if (toRemove != null)
                        liteRequests.RemoveRange(toRemove);
                }

                while (liteRequests.Count > 0)
                {
                    var group = liteRequests.GroupBy(a => a.Key.Type).FirstEx();

                    var dic = giGetStrings.GetInvoker(group.Key)(group.Select(a => a.Key.Id).ToList());

                    foreach (var item in group)
                    {
                        var toStr = dic.TryGetC(item.Key.Id) ?? ("[" + EngineMessage.EntityWithType0AndId1NotFound.NiceToString().FormatWith(item.Key.Type.NiceName(), item.Key.Id) + "]");
                        foreach (var lite in item.Value)
                        {
                            lite.SetToString(toStr);
                        }
                    }

                    liteRequests.RemoveRange(group.Select(a => a.Key));
                }
            }

            foreach (var entity in retrieved.Values)
            {
                entity.PostRetrieving();
                Schema.Current.OnRetrieved(entity);
                entityCache.Add(entity);
            }

            foreach (var embedded in modifiablePostRetrieving)
                embedded.PostRetrieving();

            ModifiedState ms = ModifiedState;
            foreach (var entity in retrieved.Values)
            {
                entity.Modified = ms;
                entity.IsNew = false;
            }

            foreach (var embedded in modifiablePostRetrieving)
                embedded.Modified = ms;

            if (liteRequests != null && liteRequests.Count > 0 ||
                requests != null && requests.Count > 0) // PostRetrieving could retrieve as well
            {
                retrieved.Clear();
                modifiablePostRetrieving.Clear();
                goto retry;
            }

            entityCache.ReleaseRetriever(this);
        }

        static readonly GenericInvoker<Func<List<PrimaryKey>, Dictionary<PrimaryKey, string>>> giGetStrings = new GenericInvoker<Func<List<PrimaryKey>, Dictionary<PrimaryKey, string>>>(ids => GetStrings<Entity>(ids));
        static Dictionary<PrimaryKey, string> GetStrings<T>(List<PrimaryKey> ids) where T : Entity
        {
            ICacheController cc = Schema.Current.CacheController(typeof(T));

            if (cc != null && cc.Enabled)
            {
                cc.Load();
                return ids.ToDictionary(a => a, a => cc.TryGetToString(a));
            }
            else
                return ids.GroupsOf(Schema.Current.Settings.MaxNumberOfParameters)
                    .SelectMany(gr =>
                        Database.Query<T>().Where(e => gr.Contains(e.Id)).Select(a => KVP.Create(a.Id, a.ToString())))
                    .ToDictionary();
        }

        public ModifiedState ModifiedState
        {
            get { return this.entityCache.IsSealed ? ModifiedState.Sealed : ModifiedState.Clean; }
        }
    }

    class ChildRetriever : IRetriever
    {
        EntityCache.RealEntityCache entityCache;
        public IRetriever Parent { get; set; }
        public ChildRetriever(IRetriever parent, EntityCache.RealEntityCache entityCache)
        {
            this.Parent = parent;
            this.entityCache = entityCache;
        }

        public T Complete<T>(PrimaryKey? id, Action<T> complete) where T : Entity
        {
            return Parent.Complete<T>(id, complete);
        }

        public T Request<T>(PrimaryKey? id) where T : Entity
        {
            return Parent.Request<T>(id);
        }

        public T RequestIBA<T>(PrimaryKey? typeId, string id) where T : class, IEntity
        {
            return Parent.RequestIBA<T>(typeId, id);
        }

        public Lite<T> RequestLite<T>(Lite<T> lite) where T : class, IEntity
        {
            return Parent.RequestLite<T>(lite);
        }

        public T ModifiablePostRetrieving<T>(T entity) where T : Modifiable
        {
            return Parent.ModifiablePostRetrieving(entity);
        }

        public void Dispose()
        {
            EntityCache.ReleaseRetriever(this);
        }

        public ModifiedState ModifiedState
        {
            get { return this.entityCache.IsSealed ? ModifiedState.Sealed : ModifiedState.Clean; }
        }
    }
}
