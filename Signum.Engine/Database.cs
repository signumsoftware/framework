using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Utilities;
using Signum.Engine;
using System.Linq.Expressions;
using System.Reflection;
using Signum.Engine.Linq;
using Signum.Engine.Maps;
using Signum.Utilities.ExpressionTrees;
using Signum.Entities.Reflection;
using System.Collections;
using Signum.Utilities.Reflection;
using System.Threading;
using Signum.Entities.Basics;
using System.Collections.ObjectModel;
using Signum.Entities.Internal;

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

        public static T RetrieveAndForget<T>(this Lite<T> lite) where T : class, IEntity
        {
            if (lite == null)
                throw new ArgumentNullException("lite");

            return (T)(object)Retrieve(lite.EntityType, lite.Id);
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
                        }

                        if (filter != null && !filter.InMemoryFunction(result))
                            throw new EntityNotFoundException(typeof(T), id);

                        return result;
                    }
                }

                var retrieved = Database.Query<T>().SingleOrDefaultEx(a => a.Id == id);

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

        public static Lite<Entity> RetrieveLite(Type type, PrimaryKey id)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            return giRetrieveLite.GetInvoker(type)(id);
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

        public static Lite<T> FillToString<T>(this Lite<T> lite) where T : class, IEntity
        {
            if (lite == null)
                return null;

            lite.SetToString(GetToStr(lite.EntityType, lite.Id));

            return lite;
        }

        public static string GetToStr(Type type, PrimaryKey id)
        {
            return giGetToStr.GetInvoker(type)(id);
        }

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

        #endregion

        #region Exists

        public static bool Exists<T>(this Lite<T> lite)
            where T : Entity
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

        static GenericInvoker<Func<PrimaryKey, bool>> giExist = new GenericInvoker<Func<PrimaryKey, bool>>(id => Exists<Entity>(id));
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

        public static bool Exists(Type type, PrimaryKey id)
        {
            return giExist.GetInvoker(type)(id);

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
                                result =  cc.GetAllIds().Select(id => r.Request<T>(id)).ToList();
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

        static readonly GenericInvoker<Func<IList>> giRetrieveAll = new GenericInvoker<Func<IList>>(() => RetrieveAll<TypeEntity>());
        public static List<Entity> RetrieveAll(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            IList list = giRetrieveAll.GetInvoker(type)();
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

        public static List<Lite<Entity>> RetrieveAllLite(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            IList list = giRetrieveAllLite.GetInvoker(type)();
            return list.Cast<Lite<Entity>>().ToList();
        }


        static GenericInvoker<Func<List<PrimaryKey>, IList>> giRetrieveList = new GenericInvoker<Func<List<PrimaryKey>, IList>>(ids => RetrieveList<Entity>(ids));
        public static List<T> RetrieveList<T>(List<PrimaryKey> ids)
            where T : Entity
        {
            using (HeavyProfiler.Log("DBRetrieve", () => "List<{0}>".FormatWith(typeof(T).TypeName())))
            {
                if (ids == null)
                    throw new ArgumentNullException("ids");

                Dictionary<PrimaryKey, T> result = null;
                if (EntityCache.Created)
                {
                    result = ids.Select(id => EntityCache.Get<T>(id)).NotNull().ToDictionary(a => a.Id);
                    if (result.Count > 0)
                        ids.RemoveAll(result.ContainsKey);
                }

                if (ids.Count > 0)
                {
                    var retrieved = RetrieveFromDatabaseOrCache<T>(ids).ToDictionary(a => a.Id);

                    var missing = ids.Except(retrieved.Keys);

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

        private static List<T> RetrieveFromDatabaseOrCache<T>(List<PrimaryKey> ids) where T : Entity
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
                    }

                    if (filter != null)
                        result = result.Where(filter.InMemoryFunction).ToList();

                    return result;
                }
            }

            return ids.GroupsOf(Schema.Current.Settings.MaxNumberOfParameters).SelectMany(gr => Database.Query<T>().Where(a => gr.Contains(a.Id))).ToList();
        }

        public static List<Entity> RetrieveList(Type type, List<PrimaryKey> ids)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            IList list = giRetrieveList.GetInvoker(type)(ids);
            return list.Cast<Entity>().ToList();
        }


        static GenericInvoker<Func<List<PrimaryKey>, IList>> giRetrieveListLite = new GenericInvoker<Func<List<PrimaryKey>, IList>>(ids => RetrieveListLite<Entity>(ids));
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

        public static List<Lite<Entity>> RetrieveListLite(Type type, List<PrimaryKey> ids)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            IList list = (IList)giRetrieveListLite.GetInvoker(type).Invoke(ids);
            return list.Cast<Lite<Entity>>().ToList();
        }

        public static List<T> RetrieveFromListOfLite<T>(this IEnumerable<Lite<T>> lites)
            where T : class, IEntity
        {
            if (lites == null)
                throw new ArgumentNullException("lites");

            if (lites.IsEmpty())
                return new List<T>();

            using (Transaction tr = new Transaction())
            {
                var dic = lites.AgGroupToDictionary(a => a.EntityType, gr =>
                    RetrieveList(gr.Key, gr.Select(a => a.Id).ToList()).ToDictionary(a => a.Id));

                var result = lites.Select(l => (T)(object)dic[l.EntityType][l.Id]).ToList(); // keep same order

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
                    int result = Database.Query<T>().Where(a => ids.Contains(a.Id)).UnsafeDelete();
                    if (result != ids.Count())
                        throw new InvalidOperationException("not all the elements have been deleted");
                    tr.Commit();
                }

            }
        }

        #endregion

        #region Query
        public static IQueryable<T> Query<T>()
            where T : Entity
        {
            return new SignumTable<T>(DbQueryProvider.Single, Schema.Current.Table<T>());
        }

        [MethodExpander(typeof(MListQueryExpander))]
        public static IQueryable<MListElement<E, V>> MListQuery<E, V>(Expression<Func<E, MList<V>>> mListProperty)
            where E : Entity
        {
            PropertyInfo pi = ReflectionTools.GetPropertyInfo(mListProperty);

            var list = (FieldMList)Schema.Current.Field(mListProperty);

            return new SignumTable<MListElement<E, V>>(DbQueryProvider.Single, list.TableMList);
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

                if(entity.Type.IsLite())
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
            return new SignumTable<T>(DbQueryProvider.Single, Schema.Current.ViewBuilder.NewView(typeof(T)));
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
                    Schema.Current.OnPreUnsafeDelete<T>(query);

                    int rows = DbQueryProvider.Single.Delete(query, sql => (int)sql.ExecuteScalar());

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
                    Schema.Current.OnPreUnsafeMListDelete<E>(mlistQuery, mlistQuery.Select(mle => mle.Parent));

                    int rows = DbQueryProvider.Single.Delete(mlistQuery, sql => (int)sql.ExecuteScalar());

                    return tr.Commit(rows);
                }
            }
        }

        public static int UnsafeDeleteChunks<T>(this IQueryable<T> query, int chunkSize = 10000, int maxQueries = int.MaxValue)
         where T : Entity
        {
            int total = 0;            
            for (int i = 0; i < maxQueries; i++)
            {
                int num = query.Take(chunkSize).UnsafeDelete();
                total += num;
                if (num < chunkSize)
                    break;
            }
            return total;
        }

        public static int UnsafeDeleteMListChunks<E, V>(this IQueryable<MListElement<E, V>> mlistQuery, int chunkSize = 10000, int maxQueries = int.MaxValue)
            where E : Entity
        {
            int total = 0;
            for (int i = 0; i < maxQueries; i++)
            {
                int num = mlistQuery.Take(chunkSize).UnsafeDeleteMList();
                total += num;
                if (num < chunkSize)
                    break;
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
                    Schema.Current.OnPreUnsafeUpdate(update);
                    int rows = DbQueryProvider.Single.Update(update, sql => (int)sql.ExecuteScalar());

                    return tr.Commit(rows);
                }
            }
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

         
        public static int UnsafeInsertMList<T, E, V>(this IQueryable<T> query, Expression<Func<E, MList<V>>> mListProperty, Expression<Func<T, MListElement<E, V>>> constructor, string message = null)
               where E : Entity
        {

            if (message != null)
                return SafeConsole.WaitRows(message == "auto" ? $"Inserting MList<{ typeof(V).TypeName()}> in { typeof(E).TypeName()}" : message,
                    () => query.UnsafeInsertMList(mListProperty, constructor, message: null));

            using (HeavyProfiler.Log("DBUnsafeInsert", () => typeof(E).TypeName()))
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

        #endregion
    }

    public class MListElement<E, V> where E : Entity
    {
        public PrimaryKey RowId { get; set; }
        public int Order { get; set; }
        public E Parent { get; set; }
        public V Element { get; set; }
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
        IQueryable Query{ get; }
        LambdaExpression PartSelector { get; }
        IEnumerable<SetterExpressions> SetterExpressions{ get; }

        Type EntityType { get; }

        IQueryable<E> EntityQuery<E>() where E : Entity;
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
    }

    internal class UpdateableConverter
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
    }

    public class SetterExpressions
    {
        public LambdaExpression PropertyExpression { get; private set; }
        public LambdaExpression ValueExpression { get; private set; }

        public SetterExpressions(LambdaExpression propertyExpression, LambdaExpression valueExpression)
        {
            if (propertyExpression == null)
                throw new ArgumentNullException("propertyExpression");

            if (valueExpression == null)
                throw new ArgumentNullException("valueExpression");


            this.PropertyExpression = propertyExpression;
            this.ValueExpression = valueExpression; 
        }
    }

    public interface IView : IRootEntity { }
}
