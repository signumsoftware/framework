using Signum.Engine.Linq;
using Signum.Engine.Maps;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Entities.Internal;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Signum.Engine
{
    public static class Database
    {
        #region Save
        public static void SaveList<T>(this IEnumerable<T> entities)
            where T : class, IEntity
        {
            using (new EntityCache())
            using (HeavyProfiler.Log("DBSave", () => "SaveList<{0}>".FormatWith(typeof(T).TypeName())))
            using (Transaction tr = new Transaction())
            {
                Saver.Save(entities.Cast<Entity>().ToArray());

                tr.Commit();
            }
        }

        public static void SaveParams(params IEntity[] entities)
        {
            using (new EntityCache())
            using (HeavyProfiler.Log("DBSave", () => "SaveParams"))
            using (Transaction tr = new Transaction())
            {
                Saver.Save(entities.Cast<Entity>().ToArray());

                tr.Commit();
            }
        }

        public static T Save<T>(this T entity)
            where T : class, IEntity
        {
            if (entity == null)
                throw new ArgumentNullException("entity");

            try
            {
                using (new EntityCache())
                using (HeavyProfiler.Log("DBSave", () => "Save<{0}>".FormatWith(typeof(T).TypeName())))
                using (Transaction tr = new Transaction())
                {
                    Saver.Save((Entity)(IEntity)entity);

                    return tr.Commit(entity);
                }
            }
            catch (Exception e)
            {
                e.Data["entity"] = ((Entity)(IEntity)entity).BaseToString();

                throw;
            }
        }
        #endregion

        #region Retrieve



        public static T Retrieve<T>(this Lite<T> lite) where T : class, IEntity
        {
            if (lite == null)
                throw new ArgumentNullException("lite");

            if (lite.EntityOrNull == null)
                lite.SetEntity(Retrieve(lite.EntityType, lite.Id));

            return lite.EntityOrNull;
        }

        public static async Task<T> RetrieveAsyc<T>(this Lite<T> lite, CancellationToken token) where T : class, IEntity
        {
            if (lite == null)
                throw new ArgumentNullException("lite");

            if (lite.EntityOrNull == null)
                lite.SetEntity(await RetrieveAsync(lite.EntityType, lite.Id, token));

            return lite.EntityOrNull;
        }


        public static T RetrieveAndForget<T>(this Lite<T> lite) where T : class, IEntity
        {
            if (lite == null)
                throw new ArgumentNullException("lite");

            return (T)(object)Retrieve(lite.EntityType, lite.Id);
        }

        public static async Task<T> RetrieveAndForgetAsync<T>(this Lite<T> lite, CancellationToken token) where T : class, IEntity
        {
            if (lite == null)
                throw new ArgumentNullException("lite");

            return (T)(object)await RetrieveAsync(lite.EntityType, lite.Id, token);
        }

        static GenericInvoker<Func<PrimaryKey, Entity>> giRetrieve = new GenericInvoker<Func<PrimaryKey, Entity>>(id => Retrieve<Entity>(id));
        public static T Retrieve<T>(PrimaryKey id) where T : Entity
        {
            using (HeavyProfiler.Log("DBRetrieve", () => typeof(T).TypeName()))
            {
                if (EntityCache.Created)
                {
                    T cached = EntityCache.Get<T>(id);

                    if (cached != null)
                        return cached;
                }

                var alternateEntity = Schema.Current.OnAlternativeRetriving(typeof(T), id);
                if (alternateEntity != null)
                    return (T)alternateEntity;

                var cc = GetCacheController<T>();
                if (cc != null)
                {
                    var filter = GetFilterQuery<T>();
                    if (filter == null || filter.InMemoryFunction != null)
                    {
                        T result;
                        using (new EntityCache())
                        using (var r = EntityCache.NewRetriever())
                        {
                            result = r.Request<T>(id);

                            r.CompleteAll();
                        }

                        if (filter != null && !filter.InMemoryFunction(result))
                            throw new EntityNotFoundException(typeof(T), id);

                        return result;
                    }
                }
                
                T retrieved = Database.Query<T>().SingleOrDefaultEx(a => a.Id == id);

                if (retrieved == null)
                    throw new EntityNotFoundException(typeof(T), id);

                return retrieved;
            }
        }

        static GenericInvoker<Func<PrimaryKey, CancellationToken, Task<Entity>>> giRetrieveAsync = new GenericInvoker<Func<PrimaryKey, CancellationToken, Task<Entity>>>((id, token) => RetrieveAsync<Entity>(id, token));
        public static async Task<T> RetrieveAsync<T>(PrimaryKey id, CancellationToken token) where T : Entity
        {
            using (HeavyProfiler.Log("DBRetrieve", () => typeof(T).TypeName()))
            {
                if (EntityCache.Created)
                {
                    T cached = EntityCache.Get<T>(id);

                    if (cached != null)
                        return cached;
                }

                var alternateEntity = Schema.Current.OnAlternativeRetriving(typeof(T), id);
                if (alternateEntity != null)
                    return (T)alternateEntity;

                var cc = GetCacheController<T>();
                if (cc != null)
                {
                    var filter = GetFilterQuery<T>();
                    if (filter == null || filter.InMemoryFunction != null)
                    {
                        T result;
                        using (new EntityCache())
                        using (var r = EntityCache.NewRetriever())
                        {
                            result = r.Request<T>(id);

                            await r.CompleteAllAsync(token);
                        }

                        if (filter != null && !filter.InMemoryFunction(result))
                            throw new EntityNotFoundException(typeof(T), id);

                        return result;
                    }
                }

                T retrieved = await Database.Query<T>().SingleOrDefaultAsync(a => a.Id == id, token);

                if (retrieved == null)
                    throw new EntityNotFoundException(typeof(T), id);

                return retrieved;
            }
        }

        static CacheControllerBase<T> GetCacheController<T>() where T : Entity
        {
            CacheControllerBase<T> cc = Schema.Current.CacheController<T>();

            if (cc == null || !cc.Enabled)
                return null;

            cc.Load();

            return cc;
        }

        static FilterQueryResult<T> GetFilterQuery<T>() where T : Entity
        {
            if (EntityCache.HasRetriever) //Filtering is not necessary when retrieving IBA? 
                return null;

            return Schema.Current.OnFilterQuery<T>();
        }

        public static Entity Retrieve(Type type, PrimaryKey id)
        {
            return giRetrieve.GetInvoker(type)(id);
        }

        public static Task<Entity> RetrieveAsync(Type type, PrimaryKey id, CancellationToken token)
        {
            return giRetrieveAsync.GetInvoker(type)(id, token);
        }

        public static Lite<Entity> RetrieveLite(Type type, PrimaryKey id)
        {
            return giRetrieveLite.GetInvoker(type)(id);
        }

        public static Task<Lite<Entity>> RetrieveLiteAsync(Type type, PrimaryKey id, CancellationToken token)
        {
            return giRetrieveLiteAsync.GetInvoker(type)(id, token);
        }


        static GenericInvoker<Func<PrimaryKey, Lite<Entity>>> giRetrieveLite = new GenericInvoker<Func<PrimaryKey, Lite<Entity>>>(id => RetrieveLite<Entity>(id));
        public static Lite<T> RetrieveLite<T>(PrimaryKey id)
            where T : Entity
        {
            using (HeavyProfiler.Log("DBRetrieve", () => "Lite<{0}>".FormatWith(typeof(T).TypeName())))
            {
                try
                {
                    var cc = GetCacheController<T>();
                    if (cc != null && GetFilterQuery<T>() == null)
                    {
                        return new LiteImp<T>(id, cc.GetToString(id));
                    }

                    var result = Database.Query<T>().Select(a => a.ToLite()).SingleOrDefaultEx(a => a.Id == id);
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

        static GenericInvoker<Func<PrimaryKey, CancellationToken, Task<Lite<Entity>>>> giRetrieveLiteAsync = 
            new GenericInvoker<Func<PrimaryKey, CancellationToken, Task<Lite<Entity>>>>((id, token) => RetrieveLiteAsync<Entity>(id, token));
        public static async Task<Lite<T>> RetrieveLiteAsync<T>(PrimaryKey id, CancellationToken token)
            where T : Entity
        {
            using (HeavyProfiler.Log("DBRetrieve", () => "Lite<{0}>".FormatWith(typeof(T).TypeName())))
            {
                try
                {
                    var cc = GetCacheController<T>();
                    if (cc != null && GetFilterQuery<T>() == null)
                    {
                        return new LiteImp<T>(id, cc.GetToString(id));
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

        public static Lite<T> FillToString<T>(this Lite<T> lite) where T : class, IEntity
        {
            if (lite == null)
                return null;

            lite.SetToString(GetToStr(lite.EntityType, lite.Id));

            return lite;
        }

        public static async Task<Lite<T>> FillToStringAsync<T>(this Lite<T> lite, CancellationToken token) where T : class, IEntity
        {
            if (lite == null)
                return null;

            lite.SetToString(await GetToStrAsync(lite.EntityType, lite.Id, token));

            return lite;
        }


        public static string GetToStr(Type type, PrimaryKey id) => giGetToStr.GetInvoker(type)(id);
        static GenericInvoker<Func<PrimaryKey, string>> giGetToStr = new GenericInvoker<Func<PrimaryKey, string>>(id => GetToStr<Entity>(id));
        public static string GetToStr<T>(PrimaryKey id)
            where T : Entity
        {
            try
            {
                using (HeavyProfiler.Log("DBRetrieve", () => "GetToStr<{0}>".FormatWith(typeof(T).TypeName())))
                {
                    var cc = GetCacheController<T>();
                    if (cc != null && GetFilterQuery<T>() == null)
                        return cc.GetToString(id);

                    return Database.Query<T>().Where(a => a.Id == id).Select(a => a.ToString()).FirstEx();
                }
            }
            catch (Exception e)
            {
                e.Data["type"] = typeof(T).TypeName();
                e.Data["id"] = id;
                throw;
            }
        }


        public static Task<string> GetToStrAsync(Type type, PrimaryKey id, CancellationToken token) => giGetToStrAsync.GetInvoker(type)(id, token);
        static GenericInvoker<Func<PrimaryKey, CancellationToken, Task<string>>> giGetToStrAsync = 
            new GenericInvoker<Func<PrimaryKey, CancellationToken, Task<string>>>((id, token) => GetToStrAsync<Entity>(id, token));
        public static async Task<string> GetToStrAsync<T>(PrimaryKey id, CancellationToken token)
            where T : Entity
        {
            try
            {
                using (HeavyProfiler.Log("DBRetrieve", () => "GetToStr<{0}>".FormatWith(typeof(T).TypeName())))
                {
                    var cc = GetCacheController<T>();
                    if (cc != null && GetFilterQuery<T>() == null)
                        return cc.GetToString(id);

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
        static GenericInvoker<Func<PrimaryKey, bool>> giExist = 
            new GenericInvoker<Func<PrimaryKey, bool>>(id => Exists<Entity>(id));
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
        static GenericInvoker<Func<PrimaryKey, CancellationToken, Task<bool>>> giExistAsync = 
            new GenericInvoker<Func<PrimaryKey, CancellationToken, Task<bool>>>((id, token) => ExistsAsync<Entity>(id, token));
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
                        var filter = GetFilterQuery<T>();
                        if (filter == null || filter.InMemoryFunction != null)
                        {
                            List<T> result;
                            using (new EntityCache())
                            using (var r = EntityCache.NewRetriever())
                            {
                                result = cc.GetAllIds().Select(id => r.Request<T>(id)).ToList();

                                r.CompleteAll();
                            }

                            if (filter != null)
                                result = result.Where(filter.InMemoryFunction).ToList();

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
                        var filter = GetFilterQuery<T>();
                        if (filter == null || filter.InMemoryFunction != null)
                        {
                            List<T> result;
                            using (new EntityCache())
                            using (var r = EntityCache.NewRetriever())
                            {
                                result = cc.GetAllIds().Select(id => r.Request<T>(id)).ToList();

                                await r.CompleteAllAsync(token);
                            }

                            if (filter != null)
                                result = result.Where(filter.InMemoryFunction).ToList();

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

        

        static readonly GenericInvoker<Func<IList>> giRetrieveAll = new GenericInvoker<Func<IList>>(() => RetrieveAll<TypeEntity>());
        public static List<Entity> RetrieveAll(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            IList list = giRetrieveAll.GetInvoker(type)();
            return list.Cast<Entity>().ToList();
        }

        static Task<IList> RetrieveAllAsyncIList<T>(CancellationToken token) where T : Entity => RetrieveAllAsync<T>(token).ContinueWith(t => (IList)t.Result);
        static readonly GenericInvoker<Func<CancellationToken, Task<IList>>> giRetrieveAllAsyncIList = 
            new GenericInvoker<Func<CancellationToken, Task<IList>>>(token => RetrieveAllAsyncIList<TypeEntity>(token));
        public static async Task<List<Entity>> RetrieveAllAsync(Type type, CancellationToken token)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            IList list = await giRetrieveAllAsyncIList.GetInvoker(type)(token);
            return list.Cast<Entity>().ToList();
        }


        static readonly GenericInvoker<Func<IList>> giRetrieveAllLite = new GenericInvoker<Func<IList>>(() => Database.RetrieveAllLite<TypeEntity>());
        public static List<Lite<T>> RetrieveAllLite<T>()
            where T : Entity
        {
            try
            {
                using (HeavyProfiler.Log("DBRetrieve", () => "All Lite<{0}>".FormatWith(typeof(T).TypeName())))
                {
                    var cc = GetCacheController<T>();
                    if (cc != null && GetFilterQuery<T>() == null)
                    {
                        return cc.GetAllIds().Select(id => (Lite<T>)new LiteImp<T>(id, cc.GetToString(id))).ToList();
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
            new GenericInvoker<Func<CancellationToken, Task<IList>>>(token => Database.RetrieveAllLiteAsyncIList<TypeEntity>(token));
        static Task<IList> RetrieveAllLiteAsyncIList<T>(CancellationToken token) where T : Entity => RetrieveAllLiteAsync<T>(token).ContinueWith(r => (IList)r.Result);
        public static async Task<List<Lite<T>>> RetrieveAllLiteAsync<T>(CancellationToken token)
            where T : Entity
        {
            try
            {
                using (HeavyProfiler.Log("DBRetrieve", () => "All Lite<{0}>".FormatWith(typeof(T).TypeName())))
                {
                    var cc = GetCacheController<T>();
                    if (cc != null && GetFilterQuery<T>() == null)
                    {
                        return cc.GetAllIds().Select(id => (Lite<T>)new LiteImp<T>(id, cc.GetToString(id))).ToList();
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

        public static List<Lite<Entity>> RetrieveAllLite(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            IList list = giRetrieveAllLite.GetInvoker(type)();
            return list.Cast<Lite<Entity>>().ToList();
        }

        public static async Task<List<Lite<Entity>>> RetrieveAllLiteAsync(Type type, CancellationToken token)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            IList list = await giRetrieveAllLiteAsync.GetInvoker(type)(token);
            return list.Cast<Lite<Entity>>().ToList();
        }


        private static GenericInvoker<Func<List<PrimaryKey>, string, IList>> giRetrieveList =
            new GenericInvoker<Func<List<PrimaryKey>, string, IList>>((ids, message) => RetrieveList<Entity>(ids, message));
        public static List<T> RetrieveList<T>(List<PrimaryKey> ids, string message = null)
            where T : Entity
        {
            using (HeavyProfiler.Log("DBRetrieve", () => "List<{0}>".FormatWith(typeof(T).TypeName())))
            {
                if (ids == null)
                    throw new ArgumentNullException("ids");
                List<PrimaryKey> remainingIds;
                Dictionary<PrimaryKey, T> result = null;
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

        static List<T> RetrieveFromDatabaseOrCache<T>(List<PrimaryKey> ids, string message = null) where T : Entity
        {
            var cc = GetCacheController<T>();
            if (cc != null)
            {
                var filter = GetFilterQuery<T>();
                if (filter == null || filter.InMemoryFunction != null)
                {
                    List<T> result;

                    using (new EntityCache())
                    using (var rr = EntityCache.NewRetriever())
                    {
                        result = ids.Select(id => rr.Request<T>(id)).ToList();

                        rr.CompleteAll();
                    }

                    if (filter != null)
                        result = result.Where(filter.InMemoryFunction).ToList();

                    return result;
                }
            }

            if (message == null)
                return ids.GroupsOf(Schema.Current.Settings.MaxNumberOfParameters)
                    .SelectMany(gr => Database.Query<T>().Where(a => gr.Contains(a.Id)))
                    .ToList();
            else
            {
                SafeConsole.WriteLineColor(ConsoleColor.Cyan, message == "auto" ? "Retriving " + typeof(T).Name : message);

                var result = new List<T>();
                var groups = ids.GroupsOf(Schema.Current.Settings.MaxNumberOfParameters).ToList();
                groups.ProgressForeach(gr => gr.Count.ToString(), gr =>
                {
                    result.AddRange(Database.Query<T>().Where(a => gr.Contains(a.Id)));
                });

                return result;
            }
        }

        static GenericInvoker<Func<List<PrimaryKey>, CancellationToken, Task<IList>>> giRetrieveListAsync = 
            new GenericInvoker<Func<List<PrimaryKey>, CancellationToken, Task<IList>>>((ids, token) => RetrieveListAsyncIList<Entity>(ids, token));
        static Task<IList> RetrieveListAsyncIList<T>(List<PrimaryKey> ids, CancellationToken token) where T : Entity =>
            RetrieveListAsync<T>(ids, token).ContinueWith(p => (IList)p.Result);
        public static async Task<List<T>> RetrieveListAsync<T>(List<PrimaryKey> ids, CancellationToken token)
            where T : Entity
        {
            using (HeavyProfiler.Log("DBRetrieve", () => "List<{0}>".FormatWith(typeof(T).TypeName())))
            {
                if (ids == null)
                    throw new ArgumentNullException("ids");
                List<PrimaryKey> remainingIds;
                Dictionary<PrimaryKey, T> result = null;
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
                var filter = GetFilterQuery<T>();
                if (filter == null || filter.InMemoryFunction != null)
                {
                    List<T> result;

                    using (new EntityCache())
                    using (var rr = EntityCache.NewRetriever())
                    {
                        result = ids.Select(id => rr.Request<T>(id)).ToList();

                        await rr.CompleteAllAsync(token);
                    }

                    if (filter != null)
                        result = result.Where(filter.InMemoryFunction).ToList();

                    return result;
                }
            }

            var tasks = ids.GroupsOf(Schema.Current.Settings.MaxNumberOfParameters)
                .Select(gr => Database.Query<T>().Where(a => gr.Contains(a.Id)).ToListAsync(token))
                .ToList();

            var task = await Task.WhenAll(tasks);

            return task.SelectMany(list => list).ToList();
        }

        public static List<Entity> RetrieveList(Type type, List<PrimaryKey> ids, string message = null)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            IList list = giRetrieveList.GetInvoker(type)(ids, message);
            return list.Cast<Entity>().ToList();
        }

        public static async Task<List<Entity>> RetrieveListAsync(Type type, List<PrimaryKey> ids, CancellationToken token)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            IList list = await giRetrieveListAsync.GetInvoker(type)(ids, token);
            return list.Cast<Entity>().ToList();
        }
        
        static GenericInvoker<Func<List<PrimaryKey>, IList>> giRetrieveListLite = 
            new GenericInvoker<Func<List<PrimaryKey>, IList>>(ids => RetrieveListLite<Entity>(ids));
        public static List<Lite<T>> RetrieveListLite<T>(List<PrimaryKey> ids)
            where T : Entity
        {
            using (HeavyProfiler.Log("DBRetrieve", () => "List<Lite<{0}>>".FormatWith(typeof(T).TypeName())))
            {
                if (ids == null)
                    throw new ArgumentNullException("ids");

                var cc = GetCacheController<T>();
                if (cc != null && GetFilterQuery<T>() == null)
                {
                    return ids.Select(id => (Lite<T>)new LiteImp<T>(id, cc.GetToString(id))).ToList();
                }

                var retrieved = ids.GroupsOf(Schema.Current.Settings.MaxNumberOfParameters).SelectMany(gr => Database.Query<T>().Where(a => gr.Contains(a.Id)).Select(a => a.ToLite())).ToDictionary(a => a.Id);

                var missing = ids.Except(retrieved.Keys);

                if (missing.Any())
                    throw new EntityNotFoundException(typeof(T), missing.ToArray());

                return ids.Select(id => retrieved[id]).ToList(); //Preserve order
            }
        }

        static GenericInvoker<Func<List<PrimaryKey>, CancellationToken, Task<IList>>> giRetrieveListLiteAsync =
            new GenericInvoker<Func<List<PrimaryKey>, CancellationToken, Task<IList>>>((ids, token) => RetrieveListLiteAsyncIList<Entity>(ids, token));
        static Task<IList> RetrieveListLiteAsyncIList<T>(List<PrimaryKey> ids, CancellationToken token)  where T : Entity => RetrieveListLiteAsync<T>(ids, token).ContinueWith(t => (IList)t.Result);
        public static async Task<List<Lite<T>>> RetrieveListLiteAsync<T>(List<PrimaryKey> ids, CancellationToken token)
            where T : Entity
        {
            using (HeavyProfiler.Log("DBRetrieve", () => "List<Lite<{0}>>".FormatWith(typeof(T).TypeName())))
            {
                if (ids == null)
                    throw new ArgumentNullException("ids");

                var cc = GetCacheController<T>();
                if (cc != null && GetFilterQuery<T>() == null)
                {
                    return ids.Select(id => (Lite<T>)new LiteImp<T>(id, cc.GetToString(id))).ToList();
                }

                var tasks = ids.GroupsOf(Schema.Current.Settings.MaxNumberOfParameters)
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

        public static List<Lite<Entity>> RetrieveListLite(Type type, List<PrimaryKey> ids)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            IList list = giRetrieveListLite.GetInvoker(type).Invoke(ids);
            return list.Cast<Lite<Entity>>().ToList();
        }

        public static async Task<List<Lite<Entity>>> RetrieveListLite(Type type, List<PrimaryKey> ids, CancellationToken token)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            IList list = await giRetrieveListLiteAsync.GetInvoker(type).Invoke(ids, token);
            return list.Cast<Lite<Entity>>().ToList();
        }

        public static List<T> RetrieveFromListOfLite<T>(this IEnumerable<Lite<T>> lites, string message = null)
            where T : class, IEntity
        {
            if (lites == null)
                throw new ArgumentNullException("lites");

            if (lites.IsEmpty())
                return new List<T>();

            using (Transaction tr = new Transaction())
            {
                var dic = lites.AgGroupToDictionary(a => a.EntityType, gr =>
                    RetrieveList(gr.Key, gr.Select(a => a.Id).ToList(), message).ToDictionary(a => a.Id));

                var result = lites.Select(l => (T)(object)dic[l.EntityType][l.Id]).ToList(); // keep same order

                return tr.Commit(result);
            }
        }

        public static async Task<List<T>> RetrieveFromListOfLiteAsync<T>(this IEnumerable<Lite<T>> lites, CancellationToken token)
           where T : class, IEntity
        {
            if (lites == null)
                throw new ArgumentNullException("lites");

            if (lites.IsEmpty())
                return new List<T>();

            using (Transaction tr = new Transaction())
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
                throw new ArgumentNullException("type");

            giDeleteId.GetInvoker(type)(id);
        }

        public static void Delete<T>(this Lite<T> lite)
            where T : class, IEntity
        {
            if (lite == null)
                throw new ArgumentNullException("lite");

            if (lite.IsNew)
                throw new ArgumentNullException("lite is New");

            giDeleteId.GetInvoker(lite.EntityType)(lite.Id);
        }

        public static void Delete<T>(this T ident)
            where T : class, IEntity
        {
            if (ident == null)
                throw new ArgumentNullException("ident");

            if (ident.IsNew)
                throw new ArgumentNullException("ident is New");

            giDeleteId.GetInvoker(ident.GetType())(ident.Id);
        }

        static GenericInvoker<Action<PrimaryKey>> giDeleteId = new GenericInvoker<Action<PrimaryKey>>(id => Delete<Entity>(id));
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
                throw new ArgumentNullException("collection");

            if (collection.IsEmpty()) return;

            var areNew = collection.Where(a => a.IsNew);
            if (areNew.Any())
                throw new InvalidOperationException("The following entities are new:\r\n" +
                    areNew.ToString(a => "\t{0}".FormatWith(a), "\r\n"));

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
                throw new ArgumentNullException("collection");

            if (collection.IsEmpty()) return;

            var areNew = collection.Where(a => a.IdOrNull == null);
            if (areNew.Any())
                throw new InvalidOperationException("The following entities are new:\r\n" +
                    areNew.ToString(a => "\t{0}".FormatWith(a), "\r\n"));


            var groups = collection.GroupBy(a => a.EntityType, a => a.Id).ToList();

            using (Transaction tr = new Transaction())
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
                throw new ArgumentNullException("type");

            giDeleteList.GetInvoker(type)(ids);
        }

        static GenericInvoker<Action<IList<PrimaryKey>>> giDeleteList = new GenericInvoker<Action<IList<PrimaryKey>>>(l => DeleteList<Entity>(l));
        public static void DeleteList<T>(IList<PrimaryKey> ids)
            where T : Entity
        {
            if (ids == null)
                throw new ArgumentNullException("ids");

            using (HeavyProfiler.Log("DBDelete", () => "List<{0}>".FormatWith(typeof(T).TypeName())))
            {
                using (Transaction tr = new Transaction())
                {
                    var groups = ids.GroupsOf(Schema.Current.Settings.MaxNumberOfParameters);
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
            public Expression Expand(Expression instance, Expression[] arguments, MethodInfo mi)
            {
                var query = Expression.Lambda<Func<IQueryable>>(Expression.Call(mi, arguments)).Compile()();

                return Expression.Constant(query, mi.ReturnType);
            }
        }

        [MethodExpander(typeof(MListElementsExpander))]
        public static IQueryable<MListElement<E, V>> MListElements<E, V>(this E entity, Expression<Func<E, MList<V>>> mListProperty)
                where E : Entity
        {
            return MListQuery(mListProperty).Where(mle => mle.Parent == entity);
        }

        [MethodExpander(typeof(MListElementsExpander))]
        public static IQueryable<MListElement<E, V>> MListElementsLite<E, V>(this Lite<E> entity, Expression<Func<E, MList<V>>> mListProperty)
                where E : Entity
        {
            return MListQuery(mListProperty).Where(mle => mle.Parent.ToLite() == entity);
        }

        class MListElementsExpander : IMethodExpander
        {
            static readonly MethodInfo miMListQuery = ReflectionTools.GetMethodInfo(() => Database.MListQuery<Entity, int>(null)).GetGenericMethodDefinition();
            static readonly MethodInfo miWhere = ReflectionTools.GetMethodInfo(() => Queryable.Where<Entity>(null, a => false)).GetGenericMethodDefinition();
            static readonly MethodInfo miToLite = ReflectionTools.GetMethodInfo((Entity e) => e.ToLite()).GetGenericMethodDefinition();

            public Expression Expand(Expression instance, Expression[] arguments, MethodInfo mi)
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

                return Expression.Call(miWhere.MakeGenericMethod(mleType), Expression.Constant(query, mi.ReturnType), lambda);
            }
        }

        public static IQueryable<E> InDB<E>(this E entity)
             where E : class, IEntity
        {
            return (IQueryable<E>)giInDB.GetInvoker(typeof(E), entity.GetType()).Invoke(entity);
        }

        [MethodExpander(typeof(InDbExpander))]
        public static R InDBEntity<E, R>(this E entity, Expression<Func<E, R>> selector) where E : class, IEntity
        {
            return entity.InDB().Select(selector).SingleEx();
        }

        static GenericInvoker<Func<IEntity, IQueryable>> giInDB =
            new GenericInvoker<Func<IEntity, IQueryable>>((ie) => InDB<Entity, Entity>((Entity)ie));
        static IQueryable<S> InDB<S, RT>(S entity)
            where S : class, IEntity
            where RT : Entity, S
        {
            if (entity == null)
                throw new ArgumentNullException("entity");

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
                throw new ArgumentNullException("lite");

            return (IQueryable<E>)giInDBLite.GetInvoker(typeof(E), lite.EntityType).Invoke(lite);
        }

        [MethodExpander(typeof(InDbExpander))]
        public static R InDB<E, R>(this Lite<E> lite, Expression<Func<E, R>> selector) where E : class, IEntity
        {
            return lite.InDB().Select(selector).SingleEx();
        }

        static GenericInvoker<Func<Lite<IEntity>, IQueryable>> giInDBLite =
            new GenericInvoker<Func<Lite<IEntity>, IQueryable>>(l => InDB<IEntity, Entity>((Lite<Entity>)l));
        static IQueryable<S> InDB<S, RT>(Lite<RT> lite)
            where S : class, IEntity
            where RT : Entity, S
        {
            if (lite == null)
                throw new ArgumentNullException("lite");

            var result = Database.Query<RT>().Where(rt => rt.ToLite() == lite);

            if (typeof(S) == typeof(RT))
                return result;

            return result.Select(rt => (S)rt);
        }

        public class InDbExpander : IMethodExpander
        {
            static MethodInfo miSelect = ReflectionTools.GetMethodInfo(() => ((IQueryable<int>)null).Select(a => a)).GetGenericMethodDefinition();
            static MethodInfo miSingleEx = ReflectionTools.GetMethodInfo(() => ((IQueryable<int>)null).SingleEx()).GetGenericMethodDefinition();

            public Expression Expand(Expression instance, Expression[] arguments, MethodInfo mi)
            {
                var entity = arguments[0];
                var lambda = arguments[1];

                var isLite = entity.Type.IsLite();

                var partialEntity = ExpressionEvaluator.PartialEval(entity);

                if (partialEntity.NodeType != ExpressionType.Constant)
                    return Expression.Invoke(lambda.StripQuotes(), isLite ? Expression.Property(entity, "Entity") : entity);

                var value = ((ConstantExpression)partialEntity).Value;

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
        public static int UnsafeDelete<T>(this IQueryable<T> query, string message = null)
            where T : Entity
        {
            if (message != null)
                return SafeConsole.WaitRows(message == "auto" ? $"Deleting {typeof(T).TypeName()}" : message,
                    () => query.UnsafeDelete(message: null));


            using (HeavyProfiler.Log("DBUnsafeDelete", () => typeof(T).TypeName()))
            {
                if (query == null)
                    throw new ArgumentNullException("query");

                using (Transaction tr = new Transaction())
                {
                    int rows;
                    using (Schema.Current.OnPreUnsafeDelete<T>(query))
                        rows = DbQueryProvider.Single.Delete(query, sql => (int)sql.ExecuteScalar());

                    return tr.Commit(rows);
                }
            }
        }

        public static int UnsafeDeleteMList<E, V>(this IQueryable<MListElement<E, V>> mlistQuery, string message = null)
            where E : Entity
        {
            if (message != null)
                return SafeConsole.WaitRows(message == "auto" ? $"Deleting MList<{typeof(V).TypeName()}> in {typeof(E).TypeName()}" : message,
                    () => mlistQuery.UnsafeDeleteMList(message: null));

            using (HeavyProfiler.Log("DBUnsafeDelete", () => typeof(MListElement<E, V>).TypeName()))
            {
                if (mlistQuery == null)
                    throw new ArgumentNullException("query");

                using (Transaction tr = new Transaction())
                {
                    int rows;
                    using (Schema.Current.OnPreUnsafeMListDelete<E>(mlistQuery, mlistQuery.Select(mle => mle.Parent)))
                        rows = DbQueryProvider.Single.Delete(mlistQuery, sql => (int)sql.ExecuteScalar());

                    return tr.Commit(rows);
                }
            }
        }

        public static int UnsafeDeleteView<T>(this IQueryable<T> query, string message = null)
           where T : IView
        {
            if (message != null)
                return SafeConsole.WaitRows(message == "auto" ? $"Deleting {typeof(T).TypeName()}" : message,
                    () => query.UnsafeDeleteView(message: null));


            using (HeavyProfiler.Log("DBUnsafeDelete", () => typeof(T).TypeName()))
            {
                if (query == null)
                    throw new ArgumentNullException("query");

                using (Transaction tr = new Transaction())
                {
                    int rows = DbQueryProvider.Single.Delete(query, sql => (int)sql.ExecuteScalar());

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

        public static IUpdateable<MListElement<E, V>> UnsafeUpdateMList<E, V>(this IQueryable<MListElement<E, V>> query)
             where E : Entity
        {
            return new Updateable<MListElement<E, V>>(query, null);
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

        public static int Execute(this IUpdateable update, string message = null)
        {
            if (message != null)
                return SafeConsole.WaitRows(message == "auto" ? UnsafeMessage(update) : message,
                    () => update.Execute(message: null));

            using (HeavyProfiler.Log("DBUnsafeUpdate", () => update.EntityType.TypeName()))
            {
                if (update == null)
                    throw new ArgumentNullException("update");

                using (Transaction tr = new Transaction())
                {
                    int rows;
                    using (Schema.Current.OnPreUnsafeUpdate(update))
                        rows = DbQueryProvider.Single.Update(update, sql => (int)sql.ExecuteScalar());

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

        static string UnsafeMessage(IUpdateable update)
        {
            if (update.PartSelector == null)
                return $"Updating { update.EntityType.TypeName()}";
            else
                return $"Updating MList<{update.GetType().GetGenericArguments()[1].TypeName()}> in {update.EntityType.TypeName()}";
        }
        #endregion

        #region UnsafeInsert

        public static int UnsafeInsert<E>(this IQueryable<E> query, string message = null)
              where E : Entity
        {
            return query.UnsafeInsert(a => a, message);
        }

        public static int UnsafeInsert<T, E>(this IQueryable<T> query, Expression<Func<T, E>> constructor, string message = null)
            where E : Entity
        {
            if (message != null)
                return SafeConsole.WaitRows(message == "auto" ? $"Inserting { typeof(E).TypeName()}" : message,
                    () => query.UnsafeInsert(constructor, message: null));

            using (HeavyProfiler.Log("DBUnsafeInsert", () => typeof(E).TypeName()))
            {
                if (query == null)
                    throw new ArgumentNullException("query");

                if (constructor == null)
                    throw new ArgumentNullException("constructor");

                using (Transaction tr = new Transaction())
                {
                    constructor = (Expression<Func<T, E>>)Schema.Current.OnPreUnsafeInsert(typeof(E), query, constructor, query.Select(constructor));
                    var table = Schema.Current.Table(typeof(E));
                    int rows = DbQueryProvider.Single.Insert(query, constructor, table, sql => (int)sql.ExecuteScalar());

                    return tr.Commit(rows);
                }
            }
        }

        public static int UnsafeInsertMList<E, V>(this IQueryable<MListElement<E, V>> query, Expression<Func<E, MList<V>>> mListProperty, string message = null)
            where E : Entity
        {
            return query.UnsafeInsertMList(mListProperty, a => a, message);
        }

        public static int UnsafeInsertMList<T, E, V>(this IQueryable<T> query, Expression<Func<E, MList<V>>> mListProperty, Expression<Func<T, MListElement<E, V>>> constructor, string message = null)
               where E : Entity
        {

            if (message != null)
                return SafeConsole.WaitRows(message == "auto" ? $"Inserting MList<{ typeof(V).TypeName()}> in { typeof(E).TypeName()}" : message,
                    () => query.UnsafeInsertMList(mListProperty, constructor, message: null));

            using (HeavyProfiler.Log("UnsafeInsertMList", () => typeof(E).TypeName()))
            {
                if (query == null)
                    throw new ArgumentNullException("query");

                if (constructor == null)
                    throw new ArgumentNullException("constructor");

                using (Transaction tr = new Transaction())
                {
                    constructor = (Expression<Func<T, MListElement<E, V>>>)Schema.Current.OnPreUnsafeInsert(typeof(E), query, constructor, query.Select(constructor).Select(c => c.Parent));
                    var table = ((FieldMList)Schema.Current.Field(mListProperty)).TableMList;
                    int rows = DbQueryProvider.Single.Insert(query, constructor, table, sql => (int)sql.ExecuteScalar());

                    return tr.Commit(rows);
                }
            }
        }

        public static int UnsafeInsertView<T, E>(this IQueryable<T> query, Expression<Func<T, E>> constructor, string message = null)
            where E : IView
        {
            if (message != null)
                return SafeConsole.WaitRows(message == "auto" ? $"Inserting { typeof(E).TypeName()}" : message,
                    () => query.UnsafeInsertView(constructor, message: null));

            using (HeavyProfiler.Log("UnsafeInsertView", () => typeof(E).TypeName()))
            {
                if (query == null)
                    throw new ArgumentNullException("query");

                if (constructor == null)
                    throw new ArgumentNullException("constructor");

                using (Transaction tr = new Transaction())
                {
                    constructor = (Expression<Func<T, E>>)Schema.Current.OnPreUnsafeInsert(typeof(E), query, constructor, query.Select(constructor));
                    var table = Schema.Current.View(typeof(E));
                    int rows = DbQueryProvider.Single.Insert(query, constructor, table, sql => (int)sql.ExecuteScalar());

                    return tr.Commit(rows);
                }
            }
        }

        #endregion

        public static void Merge<E, A>(string title, IQueryable<E> should, IQueryable<E> current, Expression<Func<E, A>> getKey, List<Expression<Func<E, object>>> toUpdate = null)
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

        public static void MergeMList<E, V, A>(string title, IQueryable<MListElement<E, V>> should, IQueryable<MListElement<E, V>> current, Expression<Func<MListElement<E, V>, A>> getKey, Expression<Func<E, MList<V>>> mList)
            where E : Entity
            where A : class
        {
            if (title != null)
                SafeConsole.WriteLineColor(ConsoleColor.DarkMagenta, title);

            current.Where(c => !should.Any(s => getKey.Evaluate(c) == getKey.Evaluate(s))).UnsafeDeleteMList(title != null ? "auto" : null);

            should.Where(s => !current.Any(c => getKey.Evaluate(c) == getKey.Evaluate(s))).UnsafeInsertMList(mList, p => p, title != null ? "auto" : null);
        }

        public static List<T> ToListWait<T>(this IQueryable<T> query, string message)
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

        public static List<T> ToListWait<T>(this IQueryable<T> query, int timeoutSeconds, string message = null)
        {
            using (Connector.CommandTimeoutScope(timeoutSeconds))
            {
                if (message == null)
                    return query.ToList();
                return query.ToListWait(message);
            }
        }
    }




    interface ISignumTable
    {
        ITable Table { get; set; }
    }

    internal class SignumTable<E> : Query<E>, ISignumTable
    {
        public ITable Table { get; set; }

        public SignumTable(QueryProvider provider, ITable table)
            : base(provider)
        {
            this.Table = table;
        }
    }

    public interface IUpdateable
    {
        IQueryable Query { get; }
        LambdaExpression PartSelector { get; }
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
        IQueryable<A> query;
        Expression<Func<A, T>> partSelector;
        ReadOnlyCollection<SetterExpressions> settersExpressions;

        public UpdateablePart(IQueryable<A> query, Expression<Func<A, T>> partSelector, IEnumerable<SetterExpressions> setters)
        {
            this.query = query;
            this.partSelector = partSelector;
            this.settersExpressions = (setters ?? Enumerable.Empty<SetterExpressions>()).ToReadOnly();
        }

        public IQueryable Query { get { return this.query; } }

        public LambdaExpression PartSelector { get { return this.partSelector; } }

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
        IQueryable<T> query;
        ReadOnlyCollection<SetterExpressions> settersExpressions;

        public Updateable(IQueryable<T> query, IEnumerable<SetterExpressions> setters)
        {
            this.query = query;
            this.settersExpressions = (setters ?? Enumerable.Empty<SetterExpressions>()).ToReadOnly();
        }

        public IQueryable Query { get { return this.query; } }

        public LambdaExpression PartSelector { get { return null; } }

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
            this.PropertyExpression = propertyExpression ?? throw new ArgumentNullException("propertyExpression");
            this.ValueExpression = valueExpression ?? throw new ArgumentNullException("valueExpression");
        }
    }

    public interface IView : IRootEntity { }
}
