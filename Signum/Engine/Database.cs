using Signum.Engine.Linq;
using Signum.Engine.Maps;
using Signum.Utilities.Reflection;
using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Signum.Engine.Sync;

namespace Signum.Engine;

public static class Database
{
    #region Save
    public static List<T> SaveList<T>(this IEnumerable<T> entities)
            where T : class, IEntity
    {
        var list = entities.ToList();
        using (new EntityCache())
        using (HeavyProfiler.Log("DBSave", () => "SaveList<{0}>".FormatWith(typeof(T).TypeName())))
        using (var tr = new Transaction())
        {
            Saver.Save(list.Cast<Entity>().ToArray());

            return tr.Commit(list);
        }
    }

    public static void SaveParams(params IEntity[] entities)
    {
        using (new EntityCache())
        using (HeavyProfiler.Log("DBSave", () => "SaveParams"))
        using (var tr = new Transaction())
        {
            Saver.Save(entities.Cast<Entity>().ToArray());

            tr.Commit();
        }
    }

    public static T Save<T>(this T entity)
        where T : class, IEntity
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        try
        {
            using (new EntityCache())
            using (HeavyProfiler.Log("DBSave", () => "Save<{0}>".FormatWith(typeof(T).TypeName())))
            using (var tr = new Transaction())
            {
                Saver.Save((Entity)(IEntity)entity);

                return tr.Commit(entity);
            }
        }
        catch (Exception e)
        {
            e.Data["entity"] = entity;

            throw;
        }
    }

    public static int InsertView<T>(this T viewObject) where T : IView
    {
        var schema = Schema.Current;
        var view = schema.View<T>();
        var parameters = view.GetInsertParameters(viewObject);

        var sql = $@"INSERT INTO {view.Name} ({view.Columns.ToString(p => p.Key.SqlEscape(schema.Settings.IsPostgres), ", ")})
VALUES ({parameters.ToString(p => p.ParameterName, ", ")})";

        return Executor.ExecuteNonQuery(sql, parameters);
    }
    #endregion

    #region Retrieve


    /// <summary>
    /// Returns Lite.Entitiy field if set, othwerise Retrieves the entity from the database and sets the Lite.Entity field.
    /// </summary>
    public static T RetrieveAndRemember<T>(this Lite<T> lite) where T : class, IEntity
    {
        if (lite == null)
            throw new ArgumentNullException(nameof(lite));

        if (lite.EntityOrNull == null)
            lite.SetEntity(Retrieve(lite.EntityType, lite.Id));

        return lite.EntityOrNull!;
    }

    /// <summary>
    /// Returns Lite.Entitiy field if set, othwerise Retrieves the entity from the database asynchronously and sets the Lite.Entity field.
    /// </summary>
    public static async Task<T> RetrieveAndRememberAsyc<T>(this Lite<T> lite, CancellationToken token) where T : class, IEntity
    {
        if (lite == null)
            throw new ArgumentNullException(nameof(lite));

        if (lite.EntityOrNull == null)
            lite.SetEntity(await RetrieveAsync(lite.EntityType, lite.Id, token, lite.PartitionId));

        return lite.EntityOrNull!;
    }

    /// <summary>
    /// Always retrieves the entity from the database WITHOUT reading or writing in the Lite.Entity field.
    /// </summary>
    public static T Retrieve<T>(this Lite<T> lite) where T : class, IEntity
    {
        if (lite == null)
            throw new ArgumentNullException(nameof(lite));

        return (T)(object)Retrieve(lite.EntityType, lite.Id, lite.PartitionId);
    }

    /// <summary>
    /// Always retrieves the entity from the database asynchronously WITHOUT reading or writing in the Lite.Entity field.
    /// </summary>
    public static async Task<T> RetrieveAsync<T>(this Lite<T> lite, CancellationToken token) where T : class, IEntity
    {
        if (lite == null)
            throw new ArgumentNullException(nameof(lite));

        return (T)(object)await RetrieveAsync(lite.EntityType, lite.Id, token, lite.PartitionId);
    }

    static readonly GenericInvoker<Func<PrimaryKey, int?, Entity>> giRetrieve = new((id, partitionId) => Retrieve<Entity>(id, partitionId));
    public static T Retrieve<T>(PrimaryKey id, int? partitionId = null) where T : Entity
    {
        using (HeavyProfiler.Log("DBRetrieve", () => typeof(T).TypeName()))
        {
            if (EntityCache.Created)
            {
                T? cached = EntityCache.Get<T>(id);

                if (cached != null)
                    return cached;
            }

            var alternateEntity = Schema.Current.OnAlternativeRetriving(typeof(T), id);
            if (alternateEntity != null)
                return (T)alternateEntity;

            var cc = GetCacheController<T>();
            if (cc != null)
            {
                var filter = GetFilterQuery<T>(a=>a.Id == id);
                if (filter == null || filter.InMemoryFunction != null)
                {
                    T result;
                    using (new EntityCache())
                    using (var r = EntityCache.NewRetriever())
                    {
                        result = r.Request<T>(id)!;

                        r.CompleteAll();
                    }

                    if (filter != null && !filter.InMemoryFunction!(result))
                        throw new EntityNotFoundException(typeof(T), id);

                    return result;
                }
            }



            T? retrieved = (partitionId != null ? Database.Query<T>().SingleOrDefaultEx(a => a.Id == id && a.PartitionId == partitionId) : null);

            retrieved ??= Database.Query<T>().SingleOrDefaultEx(a => a.Id == id);

            if (retrieved == null)
                throw new EntityNotFoundException(typeof(T), id);

            return retrieved;
        }
    }

    static readonly GenericInvoker<Func<PrimaryKey, CancellationToken, int?, Task>> giRetrieveAsync = new((id, token, partitionId) => RetrieveAsync<Entity>(id, token, partitionId));
    public static async Task<T> RetrieveAsync<T>(PrimaryKey id, CancellationToken token, int? partitionId = null) where T : Entity
    {
        using (HeavyProfiler.Log("DBRetrieve", () => typeof(T).TypeName()))
        {
            if (EntityCache.Created)
            {
                T? cached = EntityCache.Get<T>(id);

                if (cached != null)
                    return cached;
            }

            var alternateEntity = Schema.Current.OnAlternativeRetriving(typeof(T), id);
            if (alternateEntity != null)
                return (T)alternateEntity;

            var cc = GetCacheController<T>();
            if (cc != null)
            {
                var filter = GetFilterQuery<T>(withFilter: a => a.Id == id);
                if (filter == null || filter.InMemoryFunction != null)
                {
                    T result;
                    using (new EntityCache())
                    using (var r = EntityCache.NewRetriever())
                    {
                        result = r.Request<T>(id)!;

                        await r.CompleteAllAsync(token);
                    }

                    if (filter != null && !filter.InMemoryFunction!(result))
                        throw new EntityNotFoundException(typeof(T), id);

                    return result;
                }
            }

            T? retrieved = (partitionId != null ? await Database.Query<T>().SingleOrDefaultAsync(a => a.Id == id && a.PartitionId == partitionId) : null);

            retrieved ??= await Database.Query<T>().SingleOrDefaultAsync(a => a.Id == id);

            if (retrieved == null)
                throw new EntityNotFoundException(typeof(T), id);

            return retrieved;
        }
    }

    static CacheControllerBase<T>? GetCacheController<T>() where T : Entity
    {
        CacheControllerBase<T>? cc = Schema.Current.CacheController<T>();

        if (cc == null || !cc.Enabled)
            return null;

        cc.Load();

        return cc;
    }


    /// <param name="similarQuery">This query won't be executed, is there only for TypeConditions that require FilterQueryArgs</param>
    static FilterQueryResult<T>? GetFilterQuery<T>(Expression<Func<T, bool>>? withFilter) where T : Entity
    {
        if (EntityCache.HasRetriever) //Filtering is not necessary when retrieving IBA?
            return null;

        var args = FilterQueryArgs.FromQuery(withFilter == null ? Database.Query<T>() : Database.Query<T>().Where(withFilter));

        return Schema.Current.OnFilterQuery<T>(args);
    }

    public static Entity Retrieve(Type type, PrimaryKey id, int? partitionId = null)
    {
        return giRetrieve.GetInvoker(type)(id, partitionId);
    }

    public static Task<Entity> RetrieveAsync(Type type, PrimaryKey id, CancellationToken token, int? partitionId = null)
    {
        return giRetrieveAsync.GetInvoker(type)(id, token, partitionId).CastTask<Entity>();
    }

    public static Lite<Entity>? TryRetrieveLite(Type type, PrimaryKey id, Type? modelType = null, int? partitionId = null)
    {
        return giTryRetrieveLite.GetInvoker(type)(id, modelType, partitionId);
    }

    public static Lite<Entity> RetrieveLite(Type type, PrimaryKey id, Type? modelType = null, int? partitionId = null)
    {
        return giRetrieveLite.GetInvoker(type)(id, modelType, partitionId);
    }

    public static Task<Lite<Entity>> RetrieveLiteAsync(Type type, PrimaryKey id, CancellationToken token, Type? modelType = null)
    {
        return giRetrieveLiteAsync.GetInvoker(type)(id, token, modelType).CastTask<Lite<Entity>>();
    }

    static readonly GenericInvoker<Func<PrimaryKey, Type?, int?, Lite<Entity>?>> giTryRetrieveLite = new((id, modelType, partitionId) => TryRetrieveLite<Entity>(id, modelType, partitionId));
    public static Lite<T>? TryRetrieveLite<T>(PrimaryKey id, Type? modelType = null, int? partitionId = null)
        where T : Entity
    {
        using (HeavyProfiler.Log("DBRetrieve", () => "TryRetrieveLite<{0}>".FormatWith(typeof(T).TypeName())))
        {
            try
            {
                var cc = GetCacheController<T>();
                if (cc != null && GetFilterQuery<T>(withFilter: a => a.Id == id) == null)
                {
                    if (!cc.Exists(id))
                        return null;

                    using (new EntityCache())
                    using (var rr = EntityCache.NewRetriever())
                    {
                        var model = cc.GetLiteModel(id, modelType ?? Lite.DefaultModelType(typeof(T)), rr);

                        rr.CompleteAll();

                        return Lite.Create<T>(id, model);
                    }
                }

                var result = (partitionId != null ? Database.Query<T>().Select(a => a.ToLite()).SingleOrDefaultEx(a => a.Id == id && a.PartitionId == partitionId) : null);

                result ??= Database.Query<T>().Select(a => a.ToLite()).SingleOrDefaultEx(a => a.Id == id);

                if (result == null)
                    return null;

                return result;
            }
            catch (Exception e)
            {
                e.Data["type"] = typeof(T).TypeName();
                e.Data["id"] = id;

                throw;
            }
        }
    }


    static readonly GenericInvoker<Func<PrimaryKey, Type?, int?, Lite<Entity>>> giRetrieveLite = new((id, modelType, partitionId) => RetrieveLite<Entity>(id, modelType, partitionId));
    public static Lite<T> RetrieveLite<T>(PrimaryKey id, Type? modelType = null, int? partitionId = null)
        where T : Entity
    {
        using (HeavyProfiler.Log("DBRetrieve", () => "RetrieveLite<{0}>".FormatWith(typeof(T).TypeName())))
        {
            try
            {
                var cc = GetCacheController<T>();
                if (cc != null && GetFilterQuery<T>(withFilter: a => a.Id == id) == null)
                {
                    using (new EntityCache())
                    using (var rr = EntityCache.NewRetriever())
                    {
                        var model = cc.GetLiteModel(id, modelType ?? Lite.DefaultModelType(typeof(T)), rr);

                        rr.CompleteAll();

                        return Lite.Create<T>(id, model);
                    } 
                }

                var result = (partitionId != null ? Database.Query<T>().Select(a => a.ToLite()).SingleOrDefaultEx(a => a.Id == id && a.PartitionId == partitionId) : null);
                result ??= Database.Query<T>().Select(a => a.ToLite()).SingleOrDefaultEx(a => a.Id == id);

                if (result == null)
                    throw new EntityNotFoundException(typeof(T), id);

                return result;
            }
            catch (Exception e)
            {
                e.Data["type"] = typeof(T).TypeName();
                e.Data["id"] = id;

                throw;
            }
        }
    }

    static readonly GenericInvoker<Func<PrimaryKey, CancellationToken, Type?, Task>> giRetrieveLiteAsync =
        new((id, token, modelType) => RetrieveLiteAsync<Entity>(id, token, modelType));
    public static async Task<Lite<T>> RetrieveLiteAsync<T>(PrimaryKey id, CancellationToken token, Type? modelType = null)
        where T : Entity
    {
        using (HeavyProfiler.Log("DBRetrieve", () => "RetrieveLiteAsync<{0}>".FormatWith(typeof(T).TypeName())))
        {
            try
            {
                var cc = GetCacheController<T>();
                if (cc != null && GetFilterQuery<T>(withFilter: a => a.Id == id) == null)
                {
                    using (new EntityCache())
                    using (var rr = EntityCache.NewRetriever())
                    {
                        var model = cc.GetLiteModel(id, modelType ?? Lite.DefaultModelType(typeof(T)), rr);

                        await rr.CompleteAllAsync(token);

                        return Lite.Create<T>(id, model);
                    }
                }

                var result = await Database.Query<T>().Select(a => a.ToLite()).SingleOrDefaultAsync(a => a.Id == id, token);
                if (result == null)
                    throw new EntityNotFoundException(typeof(T), id);

                return result;
            }
            catch (Exception e)
            {
                e.Data["type"] = typeof(T).TypeName();
                e.Data["id"] = id;

                throw;
            }
        }
    }

    public static Lite<T> FillLiteModel<T>(this Lite<T> lite) where T : class, IEntity
    {
        lite.SetModel(GetLiteModel(lite.EntityType, lite.Id, lite.ModelType));

        return lite;
    }

    public static async Task<Lite<T>> FillLiteModelAsync<T>(this Lite<T> lite, CancellationToken token) where T : class, IEntity
    {
        lite.SetModel(await GetLiteModelAsync(lite.EntityType, lite.Id, token, lite.ModelType));

        return lite;
    }


    public static object GetLiteModel(Type type, PrimaryKey id, Type? modelType = null) => giGetLiteModel.GetInvoker(type)(id, modelType);
    static readonly GenericInvoker<Func<PrimaryKey, Type?, object>> giGetLiteModel = new((id, modelType) => GetLiteModel<Entity>(id, modelType));
    public static object GetLiteModel<T>(PrimaryKey id, Type? modelType)
        where T : Entity
    {
        try
        {
            using (HeavyProfiler.Log("DBRetrieve", () => "GetToStr<{0}>".FormatWith(typeof(T).TypeName())))
            {
                var cc = GetCacheController<T>();
                if (cc != null && GetFilterQuery<T>(withFilter: a => a.Id == id) == null)
                {
                    using (new EntityCache())
                    using (var rr = EntityCache.NewRetriever())
                    {
                        var model = cc.GetLiteModel(id, modelType ?? Lite.DefaultModelType(typeof(T)), rr);

                        rr.CompleteAll();

                        return model;
                    }
                }

                if (modelType == null)
                    return Database.Query<T>().Where(a => a.Id == id).Select(a => a.ToLite()).FirstEx().Model!;

                return Database.Query<T>().Where(a => a.Id == id).Select(a => a.ToLite(modelType)).FirstEx().Model!;
            }
        }
        catch (Exception e)
        {
            e.Data["type"] = typeof(T).TypeName();
            e.Data["id"] = id;
            throw;
        }
    }



    public static Task<object> GetLiteModelAsync(Type type, PrimaryKey id, CancellationToken token, Type? modelType) => giGetToStrAsync.GetInvoker(type)(id, token, modelType);
    static readonly GenericInvoker<Func<PrimaryKey, CancellationToken, Type?, Task<object>>> giGetToStrAsync =
        new((id, token, modelType) => GetLiteModelAsync<Entity>(id, token, modelType));
    public static async Task<object> GetLiteModelAsync<T>(PrimaryKey id, CancellationToken token, Type? modelType = null)
        where T : Entity
    {
        try
        {
            using (HeavyProfiler.Log("DBRetrieve", () => "GetToStr<{0}>".FormatWith(typeof(T).TypeName())))
            {
                var cc = GetCacheController<T>();
                if (cc != null && GetFilterQuery<T>(withFilter: a => a.Id == id) == null)
                {
                    using (new EntityCache())
                    using (var rr = EntityCache.NewRetriever())
                    {
                        var model = cc.GetLiteModel(id, modelType ?? Lite.DefaultModelType(typeof(T)), rr);

                        await rr.CompleteAllAsync(token);

                        return model;
                    }
                }

                return await Database.Query<T>().Where(a => a.Id == id).Select(a => a.ToString()).FirstAsync(token);
            }
        }
        catch (Exception e)
        {
            e.Data["type"] = typeof(T).TypeName();
            e.Data["id"] = id;
            throw;
        }
    }


    #endregion

    #region Exists

    public static bool Exists<T>(this Lite<T> lite)
        where T : class, IEntity
    {
        try
        {
            return lite.InDB().Any();
        }
        catch (Exception e)
        {
            e.Data["lite"] = lite;
            throw;
        }
    }

    public static async Task<bool> ExistsAsync<T>(this Lite<T> lite, CancellationToken token)
        where T : class, IEntity
    {
        try
        {
            return await lite.InDB().AnyAsync(token);
        }
        catch (Exception e)
        {
            e.Data["lite"] = lite;
            throw;
        }
    }

    public static bool Exists(Type type, PrimaryKey id) => giExist.GetInvoker(type)(id);
    static readonly GenericInvoker<Func<PrimaryKey, bool>> giExist =
        new(id => Exists<Entity>(id));
    public static bool Exists<T>(PrimaryKey id)
        where T : Entity
    {
        try
        {
            return Database.Query<T>().Any(a => a.Id == id);
        }
        catch (Exception e)
        {
            e.Data["type"] = typeof(T).TypeName();
            e.Data["id"] = id;
            throw;
        }
    }

    public static Task<bool> ExistsAsync(Type type, PrimaryKey id, CancellationToken token) => giExistAsync.GetInvoker(type)(id, token);
    static readonly GenericInvoker<Func<PrimaryKey, CancellationToken, Task<bool>>> giExistAsync =
        new((id, token) => ExistsAsync<Entity>(id, token));
    public static async Task<bool> ExistsAsync<T>(PrimaryKey id, CancellationToken token)
        where T : Entity
    {
        try
        {
            return await Database.Query<T>().AnyAsync(a => a.Id == id, token);
        }
        catch (Exception e)
        {
            e.Data["type"] = typeof(T).TypeName();
            e.Data["id"] = id;
            throw;
        }
    }

    #endregion

    #region Retrieve All Lists Lites
    public static List<T> RetrieveAll<T>()
        where T : Entity
    {
        try
        {
            using (HeavyProfiler.Log("DBRetrieve", () => "All {0}".FormatWith(typeof(T).TypeName())))
            {
                var cc = GetCacheController<T>();
                if (cc != null)
                {
                    var filter = GetFilterQuery<T>(withFilter: null);
                    if (filter == null || filter.InMemoryFunction != null)
                    {
                        List<T> result;
                        using (new EntityCache())
                        using (var r = EntityCache.NewRetriever())
                        {
                            result = cc.GetAllIds().Select(id => r.Request<T>(id)!).ToList();

                            r.CompleteAll();
                        }

                        if (filter != null)
                            result = result.Where(filter.InMemoryFunction!).ToList();

                        return result;
                    }
                }

                return Database.Query<T>().ToList();
            }
        }
        catch (Exception e)
        {
            e.Data["type"] = typeof(T).TypeName();
            throw;
        }
    }

    public static async Task<List<T>> RetrieveAllAsync<T>(CancellationToken token)
        where T : Entity
    {
        try
        {
            using (HeavyProfiler.Log("DBRetrieve", () => "All {0}".FormatWith(typeof(T).TypeName())))
            {
                var cc = GetCacheController<T>();
                if (cc != null)
                {
                    var filter = GetFilterQuery<T>(withFilter: null);
                    if (filter == null || filter.InMemoryFunction != null)
                    {
                        List<T> result;
                        using (new EntityCache())
                        using (var r = EntityCache.NewRetriever())
                        {
                            result = cc.GetAllIds().Select(id => r.Request<T>(id)!).ToList();

                            await r.CompleteAllAsync(token);
                        }

                        if (filter != null)
                            result = result.Where(filter.InMemoryFunction!).ToList();

                        return result;
                    }
                }

                return await Database.Query<T>().ToListAsync(token);
            }
        }
        catch (Exception e)
        {
            e.Data["type"] = typeof(T).TypeName();
            throw;
        }
    }



    static readonly GenericInvoker<Func<IList>> giRetrieveAll = new(() => RetrieveAll<TypeEntity>());
    public static List<Entity> RetrieveAll(Type type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        IList list = giRetrieveAll.GetInvoker(type)();
        return list.Cast<Entity>().ToList();
    }

    static Task<IList> RetrieveAllAsyncIList<T>(CancellationToken token) where T : Entity => RetrieveAllAsync<T>(token).ContinueWith(t => (IList)t.Result);
    static readonly GenericInvoker<Func<CancellationToken, Task<IList>>> giRetrieveAllAsyncIList =
        new(token => RetrieveAllAsyncIList<TypeEntity>(token));
    public static async Task<List<Entity>> RetrieveAllAsync(Type type, CancellationToken token)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        IList list = await giRetrieveAllAsyncIList.GetInvoker(type)(token);
        return list.Cast<Entity>().ToList();
    }


    static readonly GenericInvoker<Func<Type?, IList>> giRetrieveAllLite = new(modelType => Database.RetrieveAllLite<TypeEntity>(modelType));
    public static List<Lite<T>> RetrieveAllLite<T>(Type? modelType = null)
        where T : Entity
    {
        try
        {
            using (HeavyProfiler.Log("DBRetrieve", () => "All Lite<{0}>".FormatWith(typeof(T).TypeName())))
            {
                var cc = GetCacheController<T>();
                if (cc != null && GetFilterQuery<T>(withFilter: null) == null)
                {
                    var mt = modelType ?? Lite.DefaultModelType(typeof(T));

                    using (new EntityCache())
                    using (var rr = EntityCache.NewRetriever())
                    {
                        var model = cc.GetAllIds().Select(id => Lite.Create<T>(id, cc.GetLiteModel(id, mt, rr))).ToList();

                        rr.CompleteAll();

                        return model;
                    }
                }

                return Database.Query<T>().Select(e => e.ToLite()).ToList();
            }
        }
        catch (Exception e)
        {
            e.Data["type"] = typeof(T).TypeName();
            throw;
        }
    }

    static readonly GenericInvoker<Func<CancellationToken, Task<IList>>> giRetrieveAllLiteAsync =
        new(token => Database.RetrieveAllLiteAsyncIList<TypeEntity>(token));
    static Task<IList> RetrieveAllLiteAsyncIList<T>(CancellationToken token) where T : Entity => RetrieveAllLiteAsync<T>(token).ContinueWith(r => (IList)r.Result);
    public static async Task<List<Lite<T>>> RetrieveAllLiteAsync<T>(CancellationToken token, Type? modelType = null)
        where T : Entity
    {
        try
        {
            using (HeavyProfiler.Log("DBRetrieve", () => "All Lite<{0}>".FormatWith(typeof(T).TypeName())))
            {
                var cc = GetCacheController<T>();
                if (cc != null && GetFilterQuery<T>(withFilter: null) == null)
                {
                    var mt = modelType ?? Lite.DefaultModelType(typeof(T));

                    using (new EntityCache())
                    using (var rr = EntityCache.NewRetriever())
                    {
                        var model = cc.GetAllIds().Select(id => Lite.Create<T>(id, cc.GetLiteModel(id, mt, rr))).ToList();

                        await rr.CompleteAllAsync(token);

                        return model;
                    }
                }

                return await Database.Query<T>().Select(e => e.ToLite()).ToListAsync(token);
            }
        }
        catch (Exception e)
        {
            e.Data["type"] = typeof(T).TypeName();
            throw;
        }
    }

    public static List<Lite<Entity>> RetrieveAllLite(Type type, Type? modelType = null)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        IList list = giRetrieveAllLite.GetInvoker(type)(modelType);
        return list.Cast<Lite<Entity>>().ToList();
    }

    public static async Task<List<Lite<Entity>>> RetrieveAllLiteAsync(Type type, CancellationToken token)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        IList list = await giRetrieveAllLiteAsync.GetInvoker(type)(token);
        return list.Cast<Lite<Entity>>().ToList();
    }


    private static readonly GenericInvoker<Func<List<PrimaryKey>, string?, IList>> giRetrieveList =
        new((ids, message) => RetrieveList<Entity>(ids, message));
    public static List<T> RetrieveList<T>(List<PrimaryKey> ids, string? message = null)
        where T : Entity
    {
        using (HeavyProfiler.Log("DBRetrieve", () => "List<{0}>".FormatWith(typeof(T).TypeName())))
        {
            if (ids == null)
                throw new ArgumentNullException(nameof(ids));
            List<PrimaryKey> remainingIds;
            Dictionary<PrimaryKey, T>? result = null;
            if (EntityCache.Created)
            {
                result = ids.Select(id => EntityCache.Get<T>(id)).NotNull().ToDictionary(a => a.Id);
                if (result.Count == 0)
                    remainingIds = ids;
                else
                    remainingIds = ids.Where(id => !result.ContainsKey(id)).ToList();
            }
            else
            {
                remainingIds = ids;
            }

            if (remainingIds.Count > 0)
            {
                var retrieved = RetrieveFromDatabaseOrCache<T>(remainingIds, message).ToDictionary(a => a.Id);

                var missing = remainingIds.Except(retrieved.Keys);

                if (missing.Any())
                    throw new EntityNotFoundException(typeof(T), missing.ToArray());

                if (result == null)
                    result = retrieved;
                else
                    result.AddRange(retrieved);
            }
            else
            {
                if (result == null)
                    result = new Dictionary<PrimaryKey, T>();
            }

            return ids.Select(id => result[id]).ToList(); //Preserve order
        }
    }

    static List<T> RetrieveFromDatabaseOrCache<T>(List<PrimaryKey> ids, string? message = null) where T : Entity
    {
        var cc = GetCacheController<T>();
        if (cc != null)
        {
            var filter = GetFilterQuery<T>(withFilter: null);
            if (filter == null || filter.InMemoryFunction != null)
            {
                List<T> result;

                using (new EntityCache())
                using (var rr = EntityCache.NewRetriever())
                {
                    result = ids.Select(id => rr.Request<T>(id)!).ToList();

                    rr.CompleteAll();
                }

                if (filter != null)
                    result = result.Where(filter.InMemoryFunction!).ToList();

                return result;
            }
        }

        if (message == null)
            return ids.Chunk(Schema.Current.Settings.MaxNumberOfParameters)
                .SelectMany(gr => Database.Query<T>().Where(a => gr.Contains(a.Id)))
                .ToList();
        else
        {
            SafeConsole.WriteLineColor(ConsoleColor.Cyan, message == "auto" ? "Retriving " + typeof(T).Name : message);

            var result = new List<T>();
            var groups = ids.Chunk(Schema.Current.Settings.MaxNumberOfParameters).ToList();
            groups.ProgressForeach(gr => gr.Length.ToString(), gr =>
            {
                result.AddRange(Database.Query<T>().Where(a => gr.Contains(a.Id)));
            });

            return result;
        }
    }

    static readonly GenericInvoker<Func<List<PrimaryKey>, CancellationToken, Task<IList>>> giRetrieveListAsync =
        new((ids, token) => RetrieveListAsyncIList<Entity>(ids, token));
    static Task<IList> RetrieveListAsyncIList<T>(List<PrimaryKey> ids, CancellationToken token) where T : Entity =>
        RetrieveListAsync<T>(ids, token).ContinueWith(p => (IList)p.Result);
    public static async Task<List<T>> RetrieveListAsync<T>(List<PrimaryKey> ids, CancellationToken token)
        where T : Entity
    {
        using (HeavyProfiler.Log("DBRetrieve", () => "List<{0}>".FormatWith(typeof(T).TypeName())))
        {
            if (ids == null)
                throw new ArgumentNullException(nameof(ids));
            List<PrimaryKey> remainingIds;
            Dictionary<PrimaryKey, T>? result = null;
            if (EntityCache.Created)
            {
                result = ids.Select(id => EntityCache.Get<T>(id)).NotNull().ToDictionary(a => a.Id);
                if (result.Count == 0)
                    remainingIds = ids;
                else
                    remainingIds = ids.Where(id => !result.ContainsKey(id)).ToList();
            }
            else
            {
                remainingIds = ids;
            }

            if (remainingIds.Count > 0)
            {
                var retrieved = (await RetrieveFromDatabaseOrCache<T>(remainingIds, token)).ToDictionary(a => a.Id);

                var missing = remainingIds.Except(retrieved.Keys);

                if (missing.Any())
                    throw new EntityNotFoundException(typeof(T), missing.ToArray());

                if (result == null)
                    result = retrieved;
                else
                    result.AddRange(retrieved);
            }
            else
            {
                if (result == null)
                    result = new Dictionary<PrimaryKey, T>();
            }

            return ids.Select(id => result[id]).ToList(); //Preserve order
        }
    }

    static async Task<List<T>> RetrieveFromDatabaseOrCache<T>(List<PrimaryKey> ids, CancellationToken token) where T : Entity
    {
        var cc = GetCacheController<T>();
        if (cc != null)
        {
            var filter = GetFilterQuery<T>(withFilter: null);
            if (filter == null || filter.InMemoryFunction != null)
            {
                List<T> result;

                using (new EntityCache())
                using (var rr = EntityCache.NewRetriever())
                {
                    result = ids.Select(id => rr.Request<T>(id)!).ToList();

                    await rr.CompleteAllAsync(token);
                }

                if (filter != null)
                    result = result.Where(filter.InMemoryFunction!).ToList();

                return result;
            }
        }

        var tasks = ids.Chunk(Schema.Current.Settings.MaxNumberOfParameters)
            .Select(gr => Database.Query<T>().Where(a => gr.Contains(a.Id)).ToListAsync(token))
            .ToList();

        var task = await Task.WhenAll(tasks);

        return task.SelectMany(list => list).ToList();
    }

    public static List<Entity> RetrieveList(Type type, List<PrimaryKey> ids, string? message = null)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        IList list = giRetrieveList.GetInvoker(type)(ids, message);
        return list.Cast<Entity>().ToList();
    }

    public static async Task<List<Entity>> RetrieveListAsync(Type type, List<PrimaryKey> ids, CancellationToken token)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        IList list = await giRetrieveListAsync.GetInvoker(type)(ids, token);
        return list.Cast<Entity>().ToList();
    }

    static readonly GenericInvoker<Func<List<PrimaryKey>, Type?, IList>> giRetrieveListLite =
        new((ids, modelType) => RetrieveListLite<Entity>(ids, modelType));
    public static List<Lite<T>> RetrieveListLite<T>(List<PrimaryKey> ids, Type? modelType = null)
        where T : Entity
    {
        using (HeavyProfiler.Log("DBRetrieve", () => "List<Lite<{0}>>".FormatWith(typeof(T).TypeName())))
        {
            if (ids == null)
                throw new ArgumentNullException(nameof(ids));

            var cc = GetCacheController<T>();
            if (cc != null && GetFilterQuery<T>(withFilter: e => ids.Contains(e.Id)) == null)
            {
                var mt = modelType ?? Lite.DefaultModelType(typeof(T));

                using (new EntityCache())
                using (var rr = EntityCache.NewRetriever())
                {
                    var model = ids.Select(id => Lite.Create<T>(id, cc.GetLiteModel(id, mt, rr))).ToList();

                    rr.CompleteAll();

                    return model;
                }
            }

            var retrieved = ids.Chunk(Schema.Current.Settings.MaxNumberOfParameters).SelectMany(gr => Database.Query<T>().Where(a => gr.Contains(a.Id)).Select(a => a.ToLite())).ToDictionary(a => a.Id);

            var missing = ids.Except(retrieved.Keys);

            if (missing.Any())
                throw new EntityNotFoundException(typeof(T), missing.ToArray());

            return ids.Select(id => retrieved[id]).ToList(); //Preserve order
        }
    }

    static readonly GenericInvoker<Func<List<PrimaryKey>, CancellationToken, Task<IList>>> giRetrieveListLiteAsync =
        new((ids, token) => RetrieveListLiteAsyncIList<Entity>(ids, token));
    static Task<IList> RetrieveListLiteAsyncIList<T>(List<PrimaryKey> ids, CancellationToken token) where T : Entity => RetrieveListLiteAsync<T>(ids, token).ContinueWith(t => (IList)t.Result);
    public static async Task<List<Lite<T>>> RetrieveListLiteAsync<T>(List<PrimaryKey> ids, CancellationToken token, Type? modelType = null)
        where T : Entity
    {
        using (HeavyProfiler.Log("DBRetrieve", () => "List<Lite<{0}>>".FormatWith(typeof(T).TypeName())))
        {
            if (ids == null)
                throw new ArgumentNullException(nameof(ids));

            var cc = GetCacheController<T>();
            if (cc != null && GetFilterQuery<T>(withFilter: e => ids.Contains(e.Id)) == null)
            {
                var mt = modelType ?? Lite.DefaultModelType(typeof(T));

                using (new EntityCache())
                using (var rr = EntityCache.NewRetriever())
                {
                    var model = ids.Select(id => Lite.Create<T>(id, cc.GetLiteModel(id, mt, rr))).ToList();

                    await rr.CompleteAllAsync(token);

                    return model;
                }
            }

            var tasks = ids.Chunk(Schema.Current.Settings.MaxNumberOfParameters)
                .Select(gr => Database.Query<T>().Where(a => gr.Contains(a.Id)).Select(a => a.ToLite()).ToListAsync(token))
                .ToList();

            var list = await Task.WhenAll(tasks);

            var retrieved = list.SelectMany(li => li).ToDictionary(a => a.Id);

            var missing = ids.Except(retrieved.Keys);

            if (missing.Any())
                throw new EntityNotFoundException(typeof(T), missing.ToArray());

            return ids.Select(id => retrieved[id]).ToList(); //Preserve order
        }
    }

    public static List<Lite<Entity>> RetrieveListLite(Type type, List<PrimaryKey> ids, Type? modelType = null)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        IList list = giRetrieveListLite.GetInvoker(type).Invoke(ids, modelType);
        return list.Cast<Lite<Entity>>().ToList();
    }

    public static async Task<List<Lite<Entity>>> RetrieveListLite(Type type, List<PrimaryKey> ids, CancellationToken token)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        IList list = await giRetrieveListLiteAsync.GetInvoker(type).Invoke(ids, token);
        return list.Cast<Lite<Entity>>().ToList();
    }

    public static List<T> RetrieveList<T>(this IEnumerable<Lite<T>> lites, string? message = null)
        where T : class, IEntity
    {
        if (lites == null)
            throw new ArgumentNullException(nameof(lites));

        if (lites.IsEmpty())
            return new List<T>();

        using (var tr = new Transaction())
        {
            var dic = lites.AgGroupToDictionary(a => a.EntityType, gr =>
                RetrieveList(gr.Key, gr.Select(a => a.Id).Distinct().ToList(), message).ToDictionaryEx(a => a.Id));

            var result = lites.Select(l => (T)(object)dic[l.EntityType][l.Id]).ToList(); // keep same order

            return tr.Commit(result);
        }
    }

    public static async Task<List<T>> RetrieveListAsync<T>(this IEnumerable<Lite<T>> lites, CancellationToken token)
       where T : class, IEntity
    {
        if (lites == null)
            throw new ArgumentNullException(nameof(lites));

        if (lites.IsEmpty())
            return new List<T>();

        using (var tr = new Transaction())
        {
            var tasks = lites.GroupBy(a => a.EntityType).Select(gr =>
                RetrieveListAsync(gr.Key, gr.Select(a => a.Id).ToList(), token)).ToList();

            var list = await Task.WhenAll(tasks);

            var dic = list.SelectMany(li => li).ToDictionary(a => (Lite<T>)a.ToLite());

            var result = lites.Select(lite => (T)(object)dic[lite]).ToList(); // keep same order

            return tr.Commit(result);
        }
    }

    #endregion

    #region Delete
    public static void Delete(Type type, PrimaryKey id)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        giDeleteId.GetInvoker(type)(id);
    }

    public static void Delete<T>(this Lite<T> lite)
        where T : class, IEntity
    {
        if (lite == null)
            throw new ArgumentNullException(nameof(lite));

        if (lite.IsNew)
            throw new ArgumentException("lite is New");

        giDeleteId.GetInvoker(lite.EntityType)(lite.Id);
    }

    public static void Delete<T>(this T ident)
        where T : class, IEntity
    {
        if (ident == null)
            throw new ArgumentNullException(nameof(ident));

        if (ident.IsNew)
            throw new ArgumentException("ident is New");

        giDeleteId.GetInvoker(ident.GetType())(ident.Id);
    }

    static readonly GenericInvoker<Action<PrimaryKey>> giDeleteId = new(id => Delete<Entity>(id));
    public static void Delete<T>(PrimaryKey id)
        where T : Entity
    {
        using (HeavyProfiler.Log("DBDelete", () => typeof(T).TypeName()))
        {
            int result = Database.Query<T>().Where(a => a.Id == id).UnsafeDelete();
            if (result != 1)
                throw new EntityNotFoundException(typeof(T), id);
        }
    }


    public static void DeleteList<T>(IList<T> collection)
        where T : IEntity
    {
        if (collection == null)
            throw new ArgumentNullException(nameof(collection));

        if (collection.IsEmpty()) return;

        var areNew = collection.Where(a => a.IsNew);
        if (areNew.Any())
            throw new InvalidOperationException("The following entities are new:\n" +
                areNew.ToString(a => "\t{0}".FormatWith(a), "\n"));

        var groups = collection.GroupBy(a => a.GetType(), a => a.Id).ToList();

        foreach (var gr in groups)
        {
            giDeleteList.GetInvoker(gr.Key)(gr.ToList());
        }
    }

    public static void DeleteList<T>(IList<Lite<T>> collection)
        where T : class, IEntity
    {
        if (collection == null)
            throw new ArgumentNullException(nameof(collection));

        if (collection.IsEmpty()) return;

        var areNew = collection.Where(a => a.IdOrNull == null);
        if (areNew.Any())
            throw new InvalidOperationException("The following entities are new:\n" +
                areNew.ToString(a => "\t{0}".FormatWith(a), "\n"));


        var groups = collection.GroupBy(a => a.EntityType, a => a.Id).ToList();

        using (var tr = new Transaction())
        {
            foreach (var gr in groups)
            {
                giDeleteList.GetInvoker(gr.Key)(gr.ToList());
            }

            tr.Commit();
        }
    }

    public static void DeleteList(Type type, IList<PrimaryKey> ids)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        giDeleteList.GetInvoker(type)(ids);
    }

    static readonly GenericInvoker<Action<IList<PrimaryKey>>> giDeleteList = new(l => DeleteList<Entity>(l));
    public static void DeleteList<T>(IList<PrimaryKey> ids)
        where T : Entity
    {
        if (ids == null)
            throw new ArgumentNullException(nameof(ids));

        using (HeavyProfiler.Log("DBDelete", () => "List<{0}>".FormatWith(typeof(T).TypeName())))
        {
            using (var tr = new Transaction())
            {
                var groups = ids.Chunk(Schema.Current.Settings.MaxNumberOfParameters);
                int result = 0;
                foreach (var group in groups)
                    result += Database.Query<T>().Where(a => group.Contains(a.Id)).UnsafeDelete();

                if (result != ids.Count)
                    throw new InvalidOperationException("not all the elements have been deleted");
                tr.Commit();
            }
        }
    }

    #endregion

    #region Query
    [DebuggerStepThrough]
    public static IQueryable<T> Query<T>()
        where T : Entity
    {
        return new SignumTable<T>(DbQueryProvider.Single, Schema.Current.Table<T>());
    }

    [DebuggerStepThrough]
    public static IQueryable<T> Query<T>(SystemTime systemTime)
    where T : Entity
    {
        return new SignumTable<T>(DbQueryProvider.Single, Schema.Current.Table<T>(), systemTime: systemTime);
    }

    /// <summary>
    /// Example: Database.MListQuery((OrderEntity o) => o.Lines)
    /// </summary>
    [MethodExpander(typeof(MListQueryExpander))]
    public static IQueryable<MListElement<E, V>> MListQuery<E, V>(Expression<Func<E, MList<V>>> mListProperty)
        where E : Entity
    {
        PropertyInfo pi = ReflectionTools.GetPropertyInfo(mListProperty);

        var list = (FieldMList)Schema.Current.Field(mListProperty);

        var mlistTable = Schema.Current.TableMList(mListProperty);

        return new SignumTable<MListElement<E, V>>(DbQueryProvider.Single, mlistTable);
    }

    class MListQueryExpander : IMethodExpander
    {
        public Expression Expand(Expression? instance, Expression[] arguments, MethodInfo mi)
        {
            var query = Expression.Lambda<Func<IQueryable>>(Expression.Call(mi, arguments)).Compile()();

            return Expression.Constant(query, mi.ReturnType);
        }
    }

    [MethodExpander(typeof(MListElementsExpander))]
    public static IQueryable<MListElement<E, V>> MListElements<E, V>(this E entity, Expression<Func<E, MList<V>>> mListProperty)
            where E : Entity
    {
        return MListQuery(mListProperty).DisableQueryFilter().Where(mle => mle.Parent == entity);
    }

    [MethodExpander(typeof(MListElementsExpander))]
    public static IQueryable<MListElement<E, V>> MListElementsLite<E, V>(this Lite<E> entity, Expression<Func<E, MList<V>>> mListProperty)
            where E : Entity
    {
        
        return MListQuery(mListProperty).DisableQueryFilter().Where(mle => mle.Parent.ToLite().Is(entity));
    }

    class MListElementsExpander : IMethodExpander
    {
        static readonly MethodInfo miMListQuery = ReflectionTools.GetMethodInfo(() => Database.MListQuery<Entity, int>(null!)).GetGenericMethodDefinition();
        static readonly MethodInfo miDisableQueryFilger = ReflectionTools.GetMethodInfo(() => LinqHints.DisableQueryFilter<Entity>(null!)).GetGenericMethodDefinition();
        static readonly MethodInfo miWhere = ReflectionTools.GetMethodInfo(() => Queryable.Where<Entity>(null!, a => false)).GetGenericMethodDefinition();
        static readonly MethodInfo miToLite = ReflectionTools.GetMethodInfo((Entity e) => e.ToLite()).GetGenericMethodDefinition();

        public Expression Expand(Expression? instance, Expression[] arguments, MethodInfo mi)
        {
            Type[] types = mi.GetGenericArguments();

            Type mleType = typeof(MListElement<,>).MakeGenericType(types);

            var query = Expression.Lambda<Func<IQueryable>>(Expression.Call(miMListQuery.MakeGenericMethod(mi.GetGenericArguments()), arguments[1])).Compile()();

            var p = Expression.Parameter(mleType, "e");

            var prop = (Expression)Expression.Property(p, "Parent");

            var entity = arguments[0];

            if (entity.Type.IsLite())
                prop = Expression.Call(miToLite.MakeGenericMethod(prop.Type), prop);

            var lambda = Expression.Lambda(Expression.Equal(prop, entity), p);

            return Expression.Call(miWhere.MakeGenericMethod(mleType),
                Expression.Call(miDisableQueryFilger.MakeGenericMethod(mleType),
                    Expression.Constant(query, mi.ReturnType)
                ), lambda);
        }
    }

    public static IQueryable<E> InDB<E>(this E entity)
         where E : class, IEntity
    {
        return (IQueryable<E>)giInDB.GetInvoker(typeof(E), entity.GetType()).Invoke(entity);
    }

    [MethodExpander(typeof(InDbExpander))]
    public static R InDB<E, R>(this E entity, Expression<Func<E, R>> selector) where E : class, IEntity
    {
        return entity.InDB().Select(selector).SingleEx();
    }

    static readonly GenericInvoker<Func<IEntity, IQueryable>> giInDB =
        new((ie) => InDB<Entity, Entity>((Entity)ie));
    static IQueryable<S> InDB<S, RT>(S entity)
        where S : class, IEntity
        where RT : Entity, S
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        if (entity.IsNew)
            throw new ArgumentException("entity is new");

        var result = Database.Query<RT>().Where(rt => rt == entity);

        if (typeof(S) == typeof(RT))
            return result;

        return result.Select(rt => (S)rt);
    }

    public static IQueryable<E> InDB<E>(this Lite<E> lite)
       where E : class, IEntity
    {
        if (lite == null)
            throw new ArgumentNullException(nameof(lite));

        //return Database.Query<E>().Where(e => e.Is(lite));

        return (IQueryable<E>)giInDBLite.GetInvoker(typeof(E), lite.EntityType).Invoke(lite);
    }

    [MethodExpander(typeof(InDbExpander))]
    public static R InDB<E, R>(this Lite<E> lite, Expression<Func<E, R>> selector) where E : class, IEntity
    {
        return lite.InDB().Select(selector).SingleEx();
    }

    static readonly GenericInvoker<Func<Lite<IEntity>, IQueryable>> giInDBLite =
        new(l => InDB<IEntity, Entity>((Lite<Entity>)l));
    static IQueryable<S> InDB<S, RT>(Lite<RT> lite)
        where S : class, IEntity
        where RT : Entity, S
    {
        if (lite == null)
            throw new ArgumentNullException(nameof(lite));

        var result = Database.Query<RT>().Where(rt => rt.ToLite().Is(lite));

        if (typeof(S) == typeof(RT))
            return result;

        return result.Select(rt => (S)rt);
    }

    public class InDbExpander : IMethodExpander
    {
        static readonly MethodInfo miSelect = ReflectionTools.GetMethodInfo(() => ((IQueryable<int>)null!).Select(a => a)).GetGenericMethodDefinition();
        static readonly MethodInfo miSingleEx = ReflectionTools.GetMethodInfo(() => ((IQueryable<int>)null!).SingleEx()).GetGenericMethodDefinition();

        public Expression Expand(Expression? instance, Expression[] arguments, MethodInfo mi)
        {
            var entity = arguments[0];
            var lambda = arguments[1];

            var isLite = entity.Type.IsLite();

            var partialEntity = ExpressionEvaluator.PartialEval(entity);

            if (partialEntity.NodeType != ExpressionType.Constant)
                return Expression.Invoke(lambda.StripQuotes(), isLite ? Expression.Property(entity, "Entity") : entity);

            var value = ((ConstantExpression)partialEntity).Value!;

            var genericArguments = mi.GetGenericArguments();

            var staticType = genericArguments[0];

            var entityType = isLite ? ((Lite<Entity>)value).EntityType : value.GetType();

            Expression query = !isLite ?
                giInDB.GetInvoker(staticType, entityType)((IEntity)value).Expression :
                giInDBLite.GetInvoker(staticType, entityType)((Lite<Entity>)value).Expression;

            var select = Expression.Call(miSelect.MakeGenericMethod(genericArguments), query, arguments[1]);

            var single = Expression.Call(miSingleEx.MakeGenericMethod(genericArguments[1]), select);

            return single;
        }
    }


    public static IQueryable<T> View<T>()
        where T : IView
    {
        return new SignumTable<T>(DbQueryProvider.Single, Schema.Current.View<T>());
    }
    #endregion

    #region UnsafeDelete
    public static int UnsafeDelete<T>(this IQueryable<T> query, string? message = null)
        where T : Entity
    {
        if (message != null)
            return SafeConsole.WaitRows(message == "auto" ? $"Deleting {typeof(T).TypeName()}" : message,
                () => query.UnsafeDelete(message: null));


        using (HeavyProfiler.Log("DBUnsafeDelete", () => typeof(T).TypeName()))
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));

            using (var tr = new Transaction())
            {
                int rows;
                using (Schema.Current.OnPreUnsafeDelete<T>(query))
                    rows = DbQueryProvider.Single.Delete(query, sql => (int)sql.ExecuteScalar()!);

                return tr.Commit(rows);
            }
        }
    }

    public static int UnsafeDeleteMList<E, V>(this IQueryable<MListElement<E, V>> mlistQuery, string? message = null)
        where E : Entity
    {
        if (message != null)
            return SafeConsole.WaitRows(message == "auto" ? $"Deleting MList<{typeof(V).TypeName()}> in {typeof(E).TypeName()}" : message,
                () => mlistQuery.UnsafeDeleteMList(message: null));

        using (HeavyProfiler.Log("DBUnsafeDelete", () => typeof(MListElement<E, V>).TypeName()))
        {
            if (mlistQuery == null)
                throw new ArgumentNullException(nameof(mlistQuery));

            using (var tr = new Transaction())
            {
                int rows;
                using (Schema.Current.OnPreUnsafeMListDelete<E>(mlistQuery, mlistQuery.Select(mle => mle.Parent)))
                    rows = DbQueryProvider.Single.Delete(mlistQuery, sql => (int)sql.ExecuteScalar()!);

                return tr.Commit(rows);
            }
        }
    }

    public static int UnsafeDeleteView<T>(this IQueryable<T> query, string? message = null)
       where T : IView
    {
        if (message != null)
            return SafeConsole.WaitRows(message == "auto" ? $"Deleting {typeof(T).TypeName()}" : message,
                () => query.UnsafeDeleteView(message: null));


        using (HeavyProfiler.Log("DBUnsafeDelete", () => typeof(T).TypeName()))
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));

            using (var tr = new Transaction())
            {
                int rows = DbQueryProvider.Single.Delete(query, sql => (int)sql.ExecuteScalar()!);

                return tr.Commit(rows);
            }
        }
    }

    public static int UnsafeDeleteChunks<T>(this IQueryable<T> query, int chunkSize = 10000, int maxChunks = int.MaxValue, int? pauseMilliseconds = null, CancellationToken? cancellationToken = null)
     where T : Entity
    {
        int total = 0;
        for (int i = 0; i < maxChunks; i++)
        {
            int num = query.OrderBy(a => a.Id).Take(chunkSize).UnsafeDelete();
            total += num;
            if (num < chunkSize)
                break;

            if (cancellationToken.HasValue)
                cancellationToken.Value.ThrowIfCancellationRequested();

            if (pauseMilliseconds.HasValue)
                Thread.Sleep(pauseMilliseconds.Value);
        }
        return total;
    }

    public static int UnsafeDeleteMListChunks<E, V>(this IQueryable<MListElement<E, V>> mlistQuery, int chunkSize = 10000, int maxChunks = int.MaxValue, int? pauseMilliseconds = null, CancellationToken? token = null)
        where E : Entity
    {
        int total = 0;
        for (int i = 0; i < maxChunks; i++)
        {
            int num = mlistQuery.OrderBy(a => a.RowId).Take(chunkSize).UnsafeDeleteMList();
            total += num;
            if (num < chunkSize)
                break;

            if (token.HasValue)
                token.Value.ThrowIfCancellationRequested();

            if (pauseMilliseconds.HasValue)
                Thread.Sleep(pauseMilliseconds.Value);
        }
        return total;
    }
    #endregion

    #region UnsafeUpdate
    public static IUpdateable<E> UnsafeUpdate<E>(this IQueryable<E> query)
      where E : Entity
    {
        return new Updateable<E>(query, null);
    }

    public static int UnsafeUpdate<E, X>(this IQueryable<E> query, Expression<Func<E, X>> propertyExpression, Expression<Func<E, X>> valueExpression, string? message = null)
        where E : Entity
    {
        return query.UnsafeUpdate().Set(propertyExpression, valueExpression).Execute(message);
    }

    public static IUpdateable<MListElement<E, V>> UnsafeUpdateMList<E, V>(this IQueryable<MListElement<E, V>> query)
         where E : Entity
    {
        return new Updateable<MListElement<E, V>>(query, null);
    }

    public static int UnsafeUpdateMList<E, V, X>(this IQueryable<MListElement<E, V>> query, Expression<Func<MListElement<E, V>, X>> propertyExpression, Expression<Func<MListElement<E, V>, X>> valueExpression, string? message = null)
         where E : Entity
    {
        return query.UnsafeUpdateMList().Set(propertyExpression, valueExpression).Execute(message);
    }

    public static IUpdateable<V> UnsafeUpdateView<V>(this IQueryable<V> query)
         where V : IView
    {
        return new Updateable<V>(query, null);
    }

    public static IUpdateablePart<A, E> UnsafeUpdatePart<A, E>(this IQueryable<A> query, Expression<Func<A, E>> partSelector)
        where E : Entity
    {
        return new UpdateablePart<A, E>(query, partSelector, null);
    }

    public static IUpdateablePart<A, MListElement<E, V>> UnsafeUpdateMListPart<A, E, V>(this IQueryable<A> query, Expression<Func<A, MListElement<E, V>>> partSelector)
        where E : Entity
    {
        return new UpdateablePart<A, MListElement<E, V>>(query, partSelector, null);
    }

    public static IUpdateablePart<A, V> UnsafeUpdateViewPart<A, V>(this IQueryable<A> query, Expression<Func<A, V>> partSelector)
           where V : Entity
    {
        return new UpdateablePart<A, V>(query, partSelector, null);
    }

    public static int Execute(this IUpdateable update, string? message = null)
    {
        if (message != null)
            return SafeConsole.WaitRows(message == "auto" ? $"Updating { update.EntityType.TypeName()}" : message,
                () => update.Execute(message: null));

        using (HeavyProfiler.Log("DBUnsafeUpdate", () => update.EntityType.TypeName()))
        {
            if (update == null)
                throw new ArgumentNullException(nameof(update));

            using (var tr = new Transaction())
            {
                int rows;
                using (Schema.Current.OnPreUnsafeUpdate(update))
                    rows = DbQueryProvider.Single.Update(update, sql => (int)sql.ExecuteScalar()!);

                return tr.Commit(rows);
            }
        }
    }

    public static int ExecuteChunks(this IUpdateable update, int chunkSize = 10000, int maxQueries = int.MaxValue, int? pauseMilliseconds = null, CancellationToken? cancellationToken = null)
    {
        int total = 0;
        for (int i = 0; i < maxQueries; i++)
        {
            int num = update.Take(chunkSize).Execute();
            total += num;
            if (num < chunkSize)
                break;

            if (cancellationToken.HasValue)
                cancellationToken.Value.ThrowIfCancellationRequested();

            if (pauseMilliseconds.HasValue)
                Thread.Sleep(pauseMilliseconds.Value);
        }
        return total;
    }
    #endregion

    #region UnsafeInsert

    public static int UnsafeInsertDisableIdentity<E>(this IQueryable<E> query, string? message = null)
        where E : Entity
    {
        using (var tr = new Transaction())
        {
            int result;
            using (Administrator.DisableIdentity(Schema.Current.Table(typeof(E))))
                result = query.UnsafeInsert(a => a, message);
            return tr.Commit(result);
        }
    }

    public static int UnsafeInsertDisableIdentity<T, E>(this IQueryable<T> query, Expression<Func<T, E>> constructor, string? message = null)
          where E : Entity
    {
        using (var tr = new Transaction())
        {
            int result;
            using (Administrator.DisableIdentity(Schema.Current.Table(typeof(E))))
                result = query.UnsafeInsert(constructor, message);
            return tr.Commit(result);
        }
    }

    public static int UnsafeInsert<E>(this IQueryable<E> query, string? message = null)
          where E : Entity
    {
        return query.UnsafeInsert(a => a, message);
    }

    public static int UnsafeInsert<T, E>(this IQueryable<T> query, Expression<Func<T, E>> constructor, string? message = null)
        where E : Entity
    {
        if (message != null)
            return SafeConsole.WaitRows(message == "auto" ? $"Inserting { typeof(E).TypeName()}" : message,
                () => query.UnsafeInsert(constructor, message: null));

        using (HeavyProfiler.Log("DBUnsafeInsert", () => typeof(E).TypeName()))
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));

            if (constructor == null)
                throw new ArgumentNullException(nameof(constructor));

            using (var tr = new Transaction())
            {
                constructor = (Expression<Func<T, E>>)Schema.Current.OnPreUnsafeInsert(typeof(E), query, constructor, query.Select(constructor));
                var table = Schema.Current.Table(typeof(E));
                int rows = DbQueryProvider.Single.Insert(query, constructor, table, sql => (int)sql.ExecuteScalar()!);

                return tr.Commit(rows);
            }
        }
    }

    public static int UnsafeInsertMList<E, V>(this IQueryable<MListElement<E, V>> query, Expression<Func<E, MList<V>>> mListProperty, string? message = null)
        where E : Entity
    {
        return query.UnsafeInsertMList(mListProperty, a => a, message);
    }

    public static int UnsafeInsertMList<T, E, V>(this IQueryable<T> query, Expression<Func<E, MList<V>>> mListProperty, Expression<Func<T, MListElement<E, V>>> constructor, string? message = null)
           where E : Entity
    {

        if (message != null)
            return SafeConsole.WaitRows(message == "auto" ? $"Inserting MList<{ typeof(V).TypeName()}> in { typeof(E).TypeName()}" : message,
                () => query.UnsafeInsertMList(mListProperty, constructor, message: null));

        using (HeavyProfiler.Log("UnsafeInsertMList", () => typeof(E).TypeName()))
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));

            if (constructor == null)
                throw new ArgumentNullException(nameof(constructor));

            using (var tr = new Transaction())
            {
                constructor = (Expression<Func<T, MListElement<E, V>>>)Schema.Current.OnPreUnsafeInsert(typeof(E), query, constructor, query.Select(constructor).Select(c => c.Parent));
                var table = ((FieldMList)Schema.Current.Field(mListProperty)).TableMList;
                int rows = DbQueryProvider.Single.Insert(query, constructor, table, sql => (int)sql.ExecuteScalar()!);

                return tr.Commit(rows);
            }
        }
    }

    public static int UnsafeInsertView<T, E>(this IQueryable<T> query, Expression<Func<T, E>> constructor, string? message = null)
        where E : IView
    {
        if (message != null)
            return SafeConsole.WaitRows(message == "auto" ? $"Inserting { typeof(E).TypeName()}" : message,
                () => query.UnsafeInsertView(constructor, message: null));

        using (HeavyProfiler.Log("UnsafeInsertView", () => typeof(E).TypeName()))
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));

            if (constructor == null)
                throw new ArgumentNullException(nameof(constructor));

            using (var tr = new Transaction())
            {
                constructor = (Expression<Func<T, E>>)Schema.Current.OnPreUnsafeInsert(typeof(E), query, constructor, query.Select(constructor));
                var table = Schema.Current.View(typeof(E));
                int rows = DbQueryProvider.Single.Insert(query, constructor, table, sql => (int)sql.ExecuteScalar()!);

                return tr.Commit(rows);
            }
        }
    }

    #endregion

    public static void Merge<E, A>(string? title, IQueryable<E> should, IQueryable<E> current, Expression<Func<E, A>> getKey, List<Expression<Func<E, object>>>? toUpdate = null)
        where E : Entity
        where A : class
    {
        if (title != null)
            SafeConsole.WriteLineColor(ConsoleColor.DarkMagenta, title);

        current.Where(c => !should.Any(s => getKey.Evaluate(c) == getKey.Evaluate(s))).UnsafeDelete(title != null ? "auto" : null);

        should.Where(s => !current.Any(c => getKey.Evaluate(c) == getKey.Evaluate(s))).UnsafeInsert(p => p, title != null ? "auto" : null);

        if (toUpdate != null)
        {
            var updater = (from c in current
                           join s in should on getKey.Evaluate(c) equals getKey.Evaluate(s)
                           select new { c, s }).UnsafeUpdatePart(a => a.c);

            foreach (var prop in toUpdate)
            {
                updater = updater.Set(prop, a => prop.Evaluate(a.s));
            }

            updater.Execute(title != null ? "auto" : null);
        }
    }

    public static void MergeMList<E, V, A>(string? title, IQueryable<MListElement<E, V>> should, IQueryable<MListElement<E, V>> current, Expression<Func<MListElement<E, V>, A>> getKey, Expression<Func<E, MList<V>>> mList)
        where E : Entity
        where A : class
    {
        if (title != null)
            SafeConsole.WriteLineColor(ConsoleColor.DarkMagenta, title);

        current.Where(c => !should.Any(s => getKey.Evaluate(c) == getKey.Evaluate(s))).UnsafeDeleteMList(title != null ? "auto" : null);

        should.Where(s => !current.Any(c => getKey.Evaluate(c) == getKey.Evaluate(s))).UnsafeInsertMList(mList, p => p, title != null ? "auto" : null);
    }

    public static List<T> ToListWait<T>(this IEnumerable<T> query, string message)
    {
        message = message == "auto" ? typeof(T).TypeName() : message;

        var result = SafeConsole.WaitQuery(message, () => query.ToList());
        lock (SafeConsole.SyncKey)
        {
            SafeConsole.WriteColor(ConsoleColor.White, " {0} ", result.Count);
            SafeConsole.WriteLineColor(ConsoleColor.DarkGray, "rows returned");
        }

        return result;
    }

    public static List<T> ToListWait<T>(this IQueryable<T> query, int timeoutSeconds, string? message = null)
    {
        using (Connector.CommandTimeoutScope(timeoutSeconds))
        {
            if (message == null)
                return query.ToList();
            return query.ToListWait(message);
        }
    }
}




interface IQuerySignumTable
{
    ITable Table { get; }
    bool DisableAssertAllowed { get; }
    SystemTime? SystemTime { get; }
}

internal class SignumTable<E> : Query<E>, IQuerySignumTable
{
    public ITable Table { get; }
    public bool DisableAssertAllowed { get; }
    public SystemTime? SystemTime { get; }

    public SignumTable(QueryProvider provider, ITable table, bool disableAssertAllowed = false, SystemTime? systemTime = null)
        : base(provider)
    {
        this.Table = table;
        this.DisableAssertAllowed = disableAssertAllowed;
        this.SystemTime = systemTime;
    }

    public override bool Equals(object? obj)
    {
        return obj is SignumTable<E> st && st.Table == this.Table;
    }

    public override int GetHashCode()
    {
        return this.Table.GetHashCode();
    }
}


public interface IUpdateable
{
    IQueryable Query { get; }
    LambdaExpression? PartSelector { get; }
    IEnumerable<SetterExpressions> SetterExpressions { get; }

    Type EntityType { get; }

    IQueryable<E> EntityQuery<E>() where E : Entity;

    IUpdateable Take(int count);
}

//E -> E
//A -> E
//MLE<E, M> -> MLE<E, M>
//A -> MLE<E, M>

public interface IUpdateablePart<A, T> : IUpdateable
{
    IUpdateablePart<A, T> Set<V>(Expression<Func<T, V>> propertyExpression, Expression<Func<A, V>> valueExpression);
}

class UpdateablePart<A, T> : IUpdateablePart<A, T>
{
    readonly IQueryable<A> query;
    readonly Expression<Func<A, T>> partSelector;
    readonly ReadOnlyCollection<SetterExpressions> settersExpressions;

    public UpdateablePart(IQueryable<A> query, Expression<Func<A, T>> partSelector, IEnumerable<SetterExpressions>? setters)
    {
        this.query = query;
        this.partSelector = partSelector;
        this.settersExpressions = (setters ?? Enumerable.Empty<SetterExpressions>()).ToReadOnly();
    }

    public IQueryable Query { get { return this.query; } }

    public LambdaExpression? PartSelector { get { return this.partSelector; } }

    public IEnumerable<SetterExpressions> SetterExpressions { get { return this.settersExpressions; } }

    public Type EntityType { get { return typeof(T); } }

    public IUpdateablePart<A, T> Set<V>(Expression<Func<T, V>> propertyExpression, Expression<Func<A, V>> valueExpression)
    {
        return new UpdateablePart<A, T>(this.query, this.partSelector,
            this.settersExpressions.And(new SetterExpressions(propertyExpression, valueExpression)));
    }

    public IQueryable<E> EntityQuery<E>() where E : Entity
    {
        var result = query.Select(partSelector);

        return UpdateableConverter.Convert<T, E>(result);
    }

    public IUpdateable Take(int count)
    {
        return new UpdateablePart<A, T>(this.query.Take(count), this.partSelector, this.settersExpressions);
    }
}

internal static class UpdateableConverter
{
    public static IQueryable<E> Convert<T, E>(IQueryable<T> query)
    {
        if (typeof(T) == typeof(E))
            return (IQueryable<E>)query;

        if (typeof(T).IsInstantiationOf(typeof(MListElement<,>)) && typeof(T).GetGenericArguments().First() == typeof(E))
        {
            var param = Expression.Parameter(typeof(T));

            var lambda = Expression.Lambda<Func<T, E>>(Expression.Property(param, "Parent"), param);

            return query.Select(lambda);
        }

        throw new InvalidOperationException("Impossible to convert {0} to {1}".FormatWith(
            typeof(IQueryable<T>).TypeName(), typeof(IQueryable<E>).TypeName()));
    }
}

public interface IUpdateable<T> : IUpdateable
{
    IUpdateable<T> Set<V>(Expression<Func<T, V>> propertyExpression, Expression<Func<T, V>> valueExpression);
}

class Updateable<T> : IUpdateable<T>
{
    readonly IQueryable<T> query;
    readonly ReadOnlyCollection<SetterExpressions> settersExpressions;

    public Updateable(IQueryable<T> query, IEnumerable<SetterExpressions>? setters)
    {
        this.query = query;
        this.settersExpressions = (setters ?? Enumerable.Empty<SetterExpressions>()).ToReadOnly();
    }

    public IQueryable Query { get { return this.query; } }

    public LambdaExpression? PartSelector { get { return null; } }

    public IEnumerable<SetterExpressions> SetterExpressions { get { return this.settersExpressions; } }

    public Type EntityType { get { return typeof(T); } }

    public IUpdateable<T> Set<V>(Expression<Func<T, V>> propertyExpression, Expression<Func<T, V>> valueExpression)
    {
        return new Updateable<T>(this.query,
            this.settersExpressions.And(new SetterExpressions(propertyExpression, valueExpression)));
    }

    public IQueryable<E> EntityQuery<E>() where E : Entity
    {
        return UpdateableConverter.Convert<T, E>(query);
    }

    public IUpdateable Take(int count)
    {
        return new Updateable<T>(this.query.Take(count), this.settersExpressions);
    }
}

public class SetterExpressions
{
    public LambdaExpression PropertyExpression { get; private set; }
    public LambdaExpression ValueExpression { get; private set; }

    public SetterExpressions(LambdaExpression propertyExpression, LambdaExpression valueExpression)
    {
        this.PropertyExpression = propertyExpression ?? throw new ArgumentNullException(nameof(propertyExpression));
        this.ValueExpression = valueExpression ?? throw new ArgumentNullException(nameof(valueExpression));
    }
}

public interface IView : IRootEntity { }
