using Signum.Engine.Maps;
using Signum.Utilities.DataStructures;
using Signum.Utilities.Reflection;
using System.Diagnostics.CodeAnalysis;

namespace Signum.Engine;

public interface IRetriever : IDisposable
{
    Dictionary<string, object> GetUserData();

    T? Complete<T>(PrimaryKey? id, Action<T> complete) where T : Entity;
    T? Request<T>(PrimaryKey? id) where T : Entity;
    T? RequestIBA<T>(PrimaryKey? typeId, IComparable? id) where T : class, IEntity;
    Lite<T>? RequestLite<T>(Lite<T>? lite) where T : class, IEntity;
    T? ModifiablePostRetrieving<T>(T? entity) where T : Modifiable;
    IRetriever? Parent { get; }

    void CompleteAll();
    Task CompleteAllAsync(CancellationToken token);

    ModifiedState ModifiedState { get; }
}

class RealRetriever : IRetriever
{
    Dictionary<string, object>? userData;
    public Dictionary<string, object> GetUserData() => userData ?? (userData = new Dictionary<string, object>());

    public IRetriever? Parent
    {
        get { return null; }
    }

    public RealRetriever(EntityCache.RealEntityCache entityCache)
    {
        this.entityCache = entityCache;
    }

    EntityCache.RealEntityCache entityCache;
    Dictionary<(Type type, PrimaryKey id), Entity> retrieved = new Dictionary<(Type type, PrimaryKey id), Entity>();
    Dictionary<Type, Dictionary<PrimaryKey, Entity>>? requests;
    Dictionary<(Type type, Type modelType, PrimaryKey id), List<Lite<IEntity>>>? liteRequests;
    List<Modifiable> modifiablePostRetrieving = new List<Modifiable>();

    bool TryGetRequest((Type type, PrimaryKey id) key, [NotNullWhen(true)]out Entity? value)
    {
        if (requests != null && requests.TryGetValue(key.type, out var dic) && dic.TryGetValue(key.id, out value))
            return true;

        value = null!;
        return false;
    }

    public T? Complete<T>(PrimaryKey? id, Action<T> complete) where T : Entity
    {
        if (id == null)
            return null;

        var tuple = (typeof(T), id.Value);

        if (entityCache.TryGetValue(tuple, out var result))
            return (T)result;

        if (retrieved.TryGetValue(tuple, out result))
            return (T)result;

        T entity;
        if (TryGetRequest(tuple, out result))
        {
            entity = (T)result;
            requests![typeof(T)].Remove(id.Value);
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
        new((rr, id) => rr.Request<Entity>(id)!);
    public T? Request<T>(PrimaryKey? id) where T : Entity
    {
        if (id == null)
            return null;

        var tuple = (type: typeof(T), id: id.Value);

        if (entityCache.TryGetValue(tuple, out Entity? ident))
            return (T)ident;

        if (retrieved.TryGetValue(tuple, out ident))
            return (T)ident;

        ICacheController? cc = Schema.Current.CacheController(typeof(T));
        if (cc != null && cc.Enabled)
        {
            T entityFromCache = EntityCache.Construct<T>(id.Value);
            retrieved.Add(tuple, entityFromCache); //Cycles
            cc.Complete(entityFromCache, this);
            return entityFromCache;
        }

        ident = (T?)requests?.TryGetC(typeof(T))?.TryGetC(id.Value);
        if (ident != null)
            return (T)ident;

        T entity = EntityCache.Construct<T>(id.Value);
        if (requests == null)
            requests = new Dictionary<Type, Dictionary<PrimaryKey, Entity>>();

        requests.GetOrCreate(tuple.type).Add(tuple.id, entity);

        return entity;
    }

    public T? RequestIBA<T>(PrimaryKey? typeId, IComparable? id) where T : class, IEntity
    {
        if (id == null)
            return null;

        Type type = TypeLogic.IdToType[typeId!.Value];

        var parsedId = new PrimaryKey(id);

        return (T)(IEntity)giRequest.GetInvoker(type)(this, parsedId);
    }

    public Lite<T>? RequestLite<T>(Lite<T>? lite) where T : class, IEntity
    {
        if (lite == null)
            return null;

        ICacheController? cc = Schema.Current.CacheController(lite.EntityType);
        if (cc != null && cc.Enabled)
        {
            lite.SetModel(cc.TryGetLiteModel(lite.Id, lite.ModelType, this) ?? Lite.GetNotFoundModel(lite));
            return lite;
        }

        var tuple = (type: lite.EntityType, modelType: lite.ModelType, id: lite.Id);
        if (liteRequests == null)
            liteRequests = new Dictionary<(Type type, Type modelType, PrimaryKey id), List<Lite<IEntity>>>();
        liteRequests.GetOrCreate(tuple).Add(lite);
        return lite;
    }

    public T? ModifiablePostRetrieving<T>(T? modifiable) where T : Modifiable
    {
        if (modifiable != null)
            modifiablePostRetrieving.Add(modifiable);

        return modifiable;
    }

    public Task CompleteAllAsync(CancellationToken token) => CompleteAllPrivate(token);
    public void CompleteAll()
    {
        try
        {
            CompleteAllPrivate(null).Wait();
        }
        catch (AggregateException ag)
        {
            var ex = ag.InnerExceptions.FirstEx();
            ex.PreserveStackTrace();
            throw ex;
        }
    }

    public async Task CompleteAllPrivate(CancellationToken? token)
    {
    retry:
        if (requests != null)
        {
            while (requests.Count > 0)
            {
                var group = requests.MaxBy(a => a.Value.Count);

                var dic = group.Value;

                ICacheController? cc = Schema.Current.CacheController(group.Key);
                if (cc != null && cc.Enabled)
                {
                    cc.Load();

                    while (dic.Count > 0)
                    {
                        Entity ident = dic.Values.FirstEx();

                        cc.Complete(ident, this);

                        retrieved.Add((type: ident.GetType(), id: ident.Id), ident);
                        dic.Remove(ident.Id);
                    }
                }
                else
                {
                    if (token == null)
                        Database.RetrieveList(group.Key, dic.Keys.ToList());
                    else
                        await Database.RetrieveListAsync(group.Key, dic.Keys.ToList(), token.Value);
                }

                if (dic.Count == 0)
                    requests.Remove(group.Key);
            }
        }

        if (liteRequests != null)
        {
            {
                List<(Type type, Type modelType, PrimaryKey id)>? toRemove = null;
                foreach (var item in liteRequests)
                {
                    var entity = retrieved.TryGetC((item.Key.type, item.Key.id));
                    if (entity != null)
                    {
                        var toStr = Lite.ConstructModel(entity, item.Key.modelType);

                        foreach (var lite in item.Value)
                            lite.SetModel(toStr);

                        if (toRemove == null)
                            toRemove = new();

                        toRemove.Add(item.Key);
                    }
                }

                if (toRemove != null)
                    liteRequests.RemoveRange(toRemove);
            }

            while (liteRequests.Count > 0)
            {
                var group = liteRequests.GroupBy(a => (a.Key.type, a.Key.modelType)).FirstEx();

                var dic = await giLiteModels.GetInvoker(group.Key.type, group.Key.modelType)(group.Select(a => a.Key.id).ToList(), this, token);

                foreach (var item in group)
                {
                    var model = dic.TryGetCN(item.Key.id) ?? Lite.GetNotFoundModel(item.Value.FirstEx());
                    foreach (var lite in item.Value)
                    {
                        lite.SetModel(model);
                    }
                }

                liteRequests.RemoveRange(group.Select(a => a.Key));
            }
        }

        var currentlyRetrieved = retrieved.Values.ToHashSet(ReferenceEqualityComparer<Entity>.Default);
        var currentlyModifiableRetrieved = modifiablePostRetrieving.ToHashSet(ReferenceEqualityComparer<Modifiable>.Default);
        var ctx = new PostRetrievingContext();
        foreach (var entity in currentlyRetrieved)
        {
            entity.PostRetrieving(ctx);
            Schema.Current.OnRetrieved(entity, ctx);
            entityCache.Add(entity);
        }

        foreach (var embedded in currentlyModifiableRetrieved)
            embedded.PostRetrieving(ctx);

        ModifiedState ms = ModifiedState;
        foreach (var entity in currentlyRetrieved)
        {
            entity.Modified = ctx.ForceModifiedState.TryGetS(entity) ?? ms;
            entity.IsNew = false;
        }

        foreach (var embedded in currentlyModifiableRetrieved)
            embedded.Modified = ctx.ForceModifiedState.TryGetS(embedded) ?? ms;

        if (liteRequests != null && liteRequests.Count > 0 ||
            requests != null && requests.Count > 0 ||
            retrieved.Count > currentlyRetrieved.Count
            ) // PostRetrieving could retrieve as well
        {
            retrieved.RemoveAll(a => currentlyRetrieved.Contains(a.Value));
            modifiablePostRetrieving.RemoveAll(a => currentlyModifiableRetrieved.Contains(a));
            goto retry;
        }


    }

    static readonly GenericInvoker<Func<List<PrimaryKey>, IRetriever, CancellationToken?, Task<Dictionary<PrimaryKey, object?>>>> giLiteModels =
        new((ids, retriever, token) => GetLiteModels<Entity, string>(ids, retriever, token));
    static async Task<Dictionary<PrimaryKey, object?>> GetLiteModels<T, M>(List<PrimaryKey> ids, IRetriever retriever, CancellationToken? token) where T : Entity
    {
        ICacheController? cc = Schema.Current.CacheController(typeof(T));
        if (cc != null && cc.Enabled)
        {
            cc.Load();
            return ids.ToDictionary(a => a, a => cc.TryGetLiteModel(a, typeof(M), retriever));
        }

        var modelExpression = Lite.GetModelConstructorExpression<T, M>();
        if (token != null)
        {
            var tasks = ids.Chunk(Schema.Current.Settings.MaxNumberOfParameters)
               .Select(gr => Database.Query<T>().Where(e => gr.Contains(e.Id)).Select(a => KeyValuePair.Create(a.Id, (object?)modelExpression.Evaluate(a))).ToListAsync(token!.Value))
               .ToList();

            var list = await Task.WhenAll(tasks);

            return list.SelectMany(li => li).ToDictionary();

        }
        else
        {
            var dic = ids.Chunk(Schema.Current.Settings.MaxNumberOfParameters)
                .SelectMany(gr => Database.Query<T>().Where(e => gr.Contains(e.Id)).Select(a => KeyValuePair.Create(a.Id, (object?)modelExpression.Evaluate(a))))
                .ToDictionaryEx();

            return dic;
        }
    }

    public void Dispose()
    {
        entityCache.ReleaseRetriever(this);
    }

    public ModifiedState ModifiedState
    {
        get { return this.entityCache.IsSealed ? ModifiedState.Sealed : ModifiedState.Clean; }
    }
}

class ChildRetriever : IRetriever
{
    public Dictionary<string, object> GetUserData() => this.parent.GetUserData();

    EntityCache.RealEntityCache entityCache;
    public IRetriever parent;
    public IRetriever? Parent => parent;
    public ChildRetriever(IRetriever parent, EntityCache.RealEntityCache entityCache)
    {
        this.parent = parent;
        this.entityCache = entityCache;
    }

    public T? Complete<T>(PrimaryKey? id, Action<T> complete) where T : Entity
    {
        return parent.Complete<T>(id, complete);
    }

    public T? Request<T>(PrimaryKey? id) where T : Entity
    {
        return parent.Request<T>(id);
    }

    public T? RequestIBA<T>(PrimaryKey? typeId, IComparable? id) where T : class, IEntity
    {
        return parent.RequestIBA<T>(typeId, id);
    }

    public Lite<T>? RequestLite<T>(Lite<T>? lite) where T : class, IEntity
    {
        return parent.RequestLite<T>(lite);
    }

    public T? ModifiablePostRetrieving<T>(T? entity) where T : Modifiable
    {
        return parent.ModifiablePostRetrieving(entity);
    }

    public void CompleteAll()
    {
    }

    public Task CompleteAllAsync(CancellationToken token)
    {
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        entityCache.ReleaseRetriever(this);
    }

    public ModifiedState ModifiedState
    {
        get { return this.entityCache.IsSealed ? ModifiedState.Sealed : ModifiedState.Clean; }
    }
}


