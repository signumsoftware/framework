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
using Signum.Engine.Exceptions;
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
            where T : class, IIdentifiable
        {
            using (new EntityCache())
            using (HeavyProfiler.Log("DBSave", () => "SaveList<{0}>".Formato(typeof(T).TypeName())))
            using (Transaction tr = new Transaction())
            {
                Saver.Save(entities.Cast<IdentifiableEntity>().ToArray());

                tr.Commit();
            }
        }

        public static void SaveParams(params IIdentifiable[] entities)
        {
            using (new EntityCache())
            using (HeavyProfiler.Log("DBSave", () => "SaveParams"))
            using (Transaction tr = new Transaction())
            {
                Saver.Save(entities.Cast<IdentifiableEntity>().ToArray());

                tr.Commit();
            }
        }

        public static T Save<T>(this T entity)
            where T : class, IIdentifiable
        {
            if (entity == null)
                throw new ArgumentNullException("entity");

            try
            {
                using (new EntityCache())
                using (HeavyProfiler.Log("DBSave", () => "Save<{0}>".Formato(typeof(T).TypeName())))
                using (Transaction tr = new Transaction())
                {
                    Saver.Save((IdentifiableEntity)(IIdentifiable)entity);

                    return tr.Commit(entity);
                }
            }
            catch (Exception e)
            {
                e.Data["entity"] = ((IdentifiableEntity)(IIdentifiable)entity).BaseToString();

                throw;
            }
        }
        #endregion

        #region Retrieve
        public static T Retrieve<T>(this Lite<T> lite) where T : class, IIdentifiable
        {
            if (lite == null)
                throw new ArgumentNullException("lite");

            if (lite.EntityOrNull == null)
                lite.SetEntity(Retrieve(lite.EntityType, lite.Id));

            return lite.EntityOrNull;
        }

        public static T RetrieveAndForget<T>(this Lite<T> lite) where T : class, IIdentifiable
        {
            if (lite == null)
                throw new ArgumentNullException("lite");

            return (T)(object)Retrieve(lite.EntityType, lite.Id);
        }

        static GenericInvoker<Func<int, IdentifiableEntity>> giRetrieve = new GenericInvoker<Func<int, IdentifiableEntity>>(id => Retrieve<IdentifiableEntity>(id));
        public static T Retrieve<T>(int id) where T : IdentifiableEntity
        {
            using (HeavyProfiler.Log("DBRetrieve", () => typeof(T).TypeName()))
            {
                var cc = CanUseCache<T>();
                if (cc != null)
                {
                    using (new EntityCache())
                    using (var r = EntityCache.NewRetriever())
                    {
                        return r.Request<T>(id);
                    }
                }

                if (EntityCache.Created)
                {
                    T cached = EntityCache.Get<T>(id);

                    if (cached != null)
                        return cached;
                }

                var retrieved = Database.Query<T>().SingleOrDefaultEx(a => a.Id == id);

                if (retrieved == null)
                    throw new EntityNotFoundException(typeof(T), id);

                return retrieved;
            }
        }

        static CacheControllerBase<T> CanUseCache<T>() where T : IdentifiableEntity
        {
            CacheControllerBase<T> cc = Schema.Current.CacheController<T>();

            if (cc == null || !cc.Enabled)
                return null;

            if (!EntityCache.HasRetriever && Schema.Current.HasQueryFilter(typeof(T)))
                return null;

            cc.Load();

            return cc;
        }

        public static IdentifiableEntity Retrieve(Type type, int id)
        {
            return giRetrieve.GetInvoker(type)(id);
        }

        public static Lite<IdentifiableEntity> RetrieveLite(Type type, int id)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            return giRetrieveLite.GetInvoker(type)(id);
        }

        static GenericInvoker<Func<int, Lite<IdentifiableEntity>>> giRetrieveLite = new GenericInvoker<Func<int, Lite<IdentifiableEntity>>>(id => RetrieveLite<IdentifiableEntity>(id));
        public static Lite<T> RetrieveLite<T>(int id)
            where T : IdentifiableEntity
        {
            using (HeavyProfiler.Log("DBRetrieve", () => "Lite<{0}>".Formato(typeof(T).TypeName())))
            {
                try
                {
                    var cc = CanUseCache<T>();
                    if (cc != null)
                        return new LiteImp<T>(id, cc.GetToString(id));

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

        public static Lite<T> FillToString<T>(this Lite<T> lite) where T : class, IIdentifiable
        {
            if (lite == null)
                return null;

            lite.SetToString(GetToStr(lite.EntityType, lite.Id));

            return lite;
        }

        public static string GetToStr(Type type, int id)
        {
            return giGetToStr.GetInvoker(type)(id);
        }

        static GenericInvoker<Func<int, string>> giGetToStr = new GenericInvoker<Func<int, string>>(id => GetToStr<IdentifiableEntity>(id));
        public static string GetToStr<T>(int id)
            where T : IdentifiableEntity
        {
            try
            {
                using (HeavyProfiler.Log("DBRetrieve", () => "GetToStr<{0}>".Formato(typeof(T).TypeName())))
                {
                    var cc = CanUseCache<T>();
                    if (cc != null)
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
            where T : IdentifiableEntity
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

        static GenericInvoker<Func<int, bool>> giExist = new GenericInvoker<Func<int, bool>>(id => Exists<IdentifiableEntity>(id));
        public static bool Exists<T>(int id)
            where T : IdentifiableEntity
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

        public static bool Exists(Type type, int id)
        {
            return giExist.GetInvoker(type)(id);

        }
        #endregion

        #region Retrieve All Lists Lites
        public static List<T> RetrieveAll<T>()
            where T : IdentifiableEntity
        {
            try
            {
                using (HeavyProfiler.Log("DBRetrieve", () => "All {0}".Formato(typeof(T).TypeName())))
                {
                    var cc = CanUseCache<T>();
                    if (cc != null)
                    {
                        using (new EntityCache())
                        using (var r = EntityCache.NewRetriever())
                        {
                            return cc.GetAllIds().Select(id => r.Request<T>(id)).ToList();
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

        static readonly GenericInvoker<Func<IList>> giRetrieveAll = new GenericInvoker<Func<IList>>(() => RetrieveAll<TypeDN>());
        public static List<IdentifiableEntity> RetrieveAll(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            IList list = giRetrieveAll.GetInvoker(type)();
            return list.Cast<IdentifiableEntity>().ToList();
        }

        static readonly GenericInvoker<Func<IList>> giRetrieveAllLite = new GenericInvoker<Func<IList>>(() => Database.RetrieveAllLite<TypeDN>());
        public static List<Lite<T>> RetrieveAllLite<T>()
            where T : IdentifiableEntity
        {
            try
            {
                using (HeavyProfiler.Log("DBRetrieve", () => "All Lite<{0}>".Formato(typeof(T).TypeName())))
                {
                    var cc = CanUseCache<T>();
                    if (cc != null)
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

        public static List<Lite<IdentifiableEntity>> RetrieveAllLite(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            IList list = giRetrieveAllLite.GetInvoker(type)();
            return list.Cast<Lite<IdentifiableEntity>>().ToList();
        }


        static GenericInvoker<Func<List<int>, IList>> giRetrieveList = new GenericInvoker<Func<List<int>, IList>>(ids => RetrieveList<IdentifiableEntity>(ids));
        public static List<T> RetrieveList<T>(List<int> ids)
            where T : IdentifiableEntity
        {
            using (HeavyProfiler.Log("DBRetrieve", () => "List<{0}>".Formato(typeof(T).TypeName())))
            {
                if (ids == null)
                    throw new ArgumentNullException("ids");

                var cc = CanUseCache<T>();
                if (cc != null)
                {
                    using (new EntityCache())
                    using (var rr = EntityCache.NewRetriever())
                    {
                        return ids.Select(id => rr.Request<T>(id)).ToList();
                    }
                }

                Dictionary<int, T> result = null;

                if (EntityCache.Created)
                {
                    result = ids.Select(id => EntityCache.Get<T>(id)).NotNull().ToDictionary(a => a.Id);
                    if (result.Count > 0)
                        ids.RemoveAll(result.ContainsKey);
                }

                if (ids.Count > 0)
                {
                    var retrieved = ids.GroupsOf(Schema.Current.Settings.MaxNumberOfParameters).SelectMany(gr => Database.Query<T>().Where(a => gr.Contains(a.Id))).ToDictionary(a => a.Id);

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
                        result = new Dictionary<int, T>();
                }

                return ids.Select(id => result[id]).ToList(); //Preserve order
            }
        }

        public static List<IdentifiableEntity> RetrieveList(Type type, List<int> ids)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            IList list = giRetrieveList.GetInvoker(type)(ids);
            return list.Cast<IdentifiableEntity>().ToList();
        }


        static GenericInvoker<Func<List<int>, IList>> giRetrieveListLite = new GenericInvoker<Func<List<int>, IList>>(ids => RetrieveListLite<IdentifiableEntity>(ids));
        public static List<Lite<T>> RetrieveListLite<T>(List<int> ids)
            where T : IdentifiableEntity
        {
            using (HeavyProfiler.Log("DBRetrieve", () => "List<Lite<{0}>>".Formato(typeof(T).TypeName())))
            {
                if (ids == null)
                    throw new ArgumentNullException("ids");

                var cc = CanUseCache<T>();
                if (cc != null)
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

        public static List<Lite<IdentifiableEntity>> RetrieveListLite(Type type, List<int> ids)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            IList list = (IList)giRetrieveListLite.GetInvoker(type).Invoke(ids);
            return list.Cast<Lite<IdentifiableEntity>>().ToList();
        }

        public static List<T> RetrieveFromListOfLite<T>(this IEnumerable<Lite<T>> lites)
            where T : class, IIdentifiable
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
        public static void Delete(Type type, int id)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            giDeleteId.GetInvoker(type)(id);
        }

        public static void Delete<T>(this Lite<T> lite)
            where T : class, IIdentifiable
        {
            if (lite == null)
                throw new ArgumentNullException("lite");

            if (lite.IsNew)
                throw new ArgumentNullException("lite is New");

            giDeleteId.GetInvoker(lite.EntityType)(lite.Id);
        }

        public static void Delete<T>(this T ident)
            where T : class, IIdentifiable
        {
            if (ident == null)
                throw new ArgumentNullException("ident");

            if (ident.IsNew)
                throw new ArgumentNullException("ident is New");

            giDeleteId.GetInvoker(ident.GetType())(ident.Id);
        }

        static GenericInvoker<Action<int>> giDeleteId = new GenericInvoker<Action<int>>(id => Delete<IdentifiableEntity>(id));
        public static void Delete<T>(int id)
            where T : IdentifiableEntity
        {
            using (HeavyProfiler.Log("DBDelete", () => typeof(T).TypeName()))
            {
                int result = Database.Query<T>().Where(a => a.Id == id).UnsafeDelete();
                if (result != 1)
                    throw new EntityNotFoundException(typeof(T), id);
            }
        }


        public static void DeleteList<T>(IList<T> collection)
            where T : IIdentifiable
        {
            if (collection == null)
                throw new ArgumentNullException("collection");

            if (collection.IsEmpty()) return;

            var areNew = collection.Where(a => a.IsNew);
            if (areNew.Any())
                throw new InvalidOperationException("The following entities are new:\r\n" +
                    areNew.ToString(a => "\t{0}".Formato(a), "\r\n"));

            var groups = collection.GroupBy(a => a.GetType(), a => a.Id).ToList();

            foreach (var gr in groups)
            {
                giDeleteList.GetInvoker(gr.Key)(gr.ToList());
            }
        }

        public static void DeleteList<T>(IList<Lite<T>> collection)
            where T : class, IIdentifiable
        {
            if (collection == null)
                throw new ArgumentNullException("collection");

            if (collection.IsEmpty()) return;

            var areNew = collection.Where(a => a.IdOrNull == null);
            if (areNew.Any())
                throw new InvalidOperationException("The following entities are new:\r\n" +
                    areNew.ToString(a => "\t{0}".Formato(a), "\r\n"));


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

        public static void DeleteList(Type type, IList<int> ids)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            giDeleteList.GetInvoker(type)(ids);
        }

        static GenericInvoker<Action<IList<int>>> giDeleteList = new GenericInvoker<Action<IList<int>>>(l => DeleteList<IdentifiableEntity>(l));
        public static void DeleteList<T>(IList<int> ids)
            where T : IdentifiableEntity
        {
            if (ids == null)
                throw new ArgumentNullException("ids");

            using (HeavyProfiler.Log("DBDelete", () => "List<{0}>".Formato(typeof(T).TypeName())))
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
            where T : IdentifiableEntity
        {
            return new SignumTable<T>(DbQueryProvider.Single, Schema.Current.Table<T>());
        }

        [MethodExpander(typeof(MListQueryExpander))]
        public static IQueryable<MListElement<E, V>> MListQuery<E, V>(Expression<Func<E, MList<V>>> mListProperty)
            where E : IdentifiableEntity
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
                where E : IdentifiableEntity
        {
            return MListQuery(mListProperty).Where(mle => mle.Parent == entity);
        }

        [MethodExpander(typeof(MListElementsExpander))]
        public static IQueryable<MListElement<E, V>> MListElementsLite<E, V>(this Lite<E> entity, Expression<Func<E, MList<V>>> mListProperty)
                where E : IdentifiableEntity
        {
            return MListQuery(mListProperty).Where(mle => mle.Parent.ToLite() == entity);
        }

        class MListElementsExpander : IMethodExpander
        {
            static readonly MethodInfo miMListQuery = ReflectionTools.GetMethodInfo(() => Database.MListQuery<IdentifiableEntity, int>(null)).GetGenericMethodDefinition();
            static readonly MethodInfo miWhere = ReflectionTools.GetMethodInfo(() => Queryable.Where<IdentifiableEntity>(null, a => false)).GetGenericMethodDefinition();
            static readonly MethodInfo miToLite = ReflectionTools.GetMethodInfo((IdentifiableEntity e) => e.ToLite()).GetGenericMethodDefinition();

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
             where E : class, IIdentifiable
        {
            return (IQueryable<E>)giInDB.GetInvoker(typeof(E), entity.GetType()).Invoke(entity);
        }

        [MethodExpander(typeof(InDbExpander))]
        public static R InDBEntity<E, R>(this E entity, Expression<Func<E, R>> selector) where E : class, IIdentifiable
        {
            return entity.InDB().Select(selector).SingleEx();
        }

        static GenericInvoker<Func<IIdentifiable, IQueryable>> giInDB =
            new GenericInvoker<Func<IIdentifiable, IQueryable>>((ie) => InDB<IdentifiableEntity, IdentifiableEntity>((IdentifiableEntity)ie));
        static IQueryable<S> InDB<S, RT>(S entity)
            where S : class, IIdentifiable
            where RT : IdentifiableEntity, S
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
           where E : class, IIdentifiable
        {
            if (lite == null)
                throw new ArgumentNullException("lite");

            return (IQueryable<E>)giInDBLite.GetInvoker(typeof(E), lite.EntityType).Invoke(lite);
        }

        [MethodExpander(typeof(InDbExpander))]
        public static R InDB<E, R>(this Lite<E> lite, Expression<Func<E, R>> selector) where E : class, IIdentifiable
        {
            return lite.InDB().Select(selector).SingleEx();
        }

        static GenericInvoker<Func<Lite<IIdentifiable>, IQueryable>> giInDBLite =
            new GenericInvoker<Func<Lite<IIdentifiable>, IQueryable>>(l => InDB<IIdentifiable, IdentifiableEntity>((Lite<IdentifiableEntity>)l));
        static IQueryable<S> InDB<S, RT>(Lite<RT> lite)
            where S : class, IIdentifiable
            where RT : IdentifiableEntity, S
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

                var entityType = isLite ? ((Lite<IdentifiableEntity>)value).EntityType : value.GetType();

                Expression query = !isLite ?
                    giInDB.GetInvoker(staticType, entityType)((IIdentifiable)value).Expression :
                    giInDBLite.GetInvoker(staticType, entityType)((Lite<IdentifiableEntity>)value).Expression;

                var select = Expression.Call(miSelect.MakeGenericMethod(genericArguments), query, arguments[1]);

                var single = Expression.Call(miSingleEx.MakeGenericMethod(genericArguments[1]), select);

                return single;
            }
        }


        public static IQueryable<T> View<T>()
            where T : IView
        {
            return new SignumTable<T>(DbQueryProvider.Single, new ViewBuilder(Schema.Current).NewView(typeof(T)));
        }
        #endregion

        #region UnsafeDelete
        public static int UnsafeDelete<T>(this IQueryable<T> query)
            where T : IdentifiableEntity
        {
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

        public static int UnsafeDeleteMList<E, V>(this IQueryable<MListElement<E, V>> mlistQuery)
            where E : IdentifiableEntity
        {
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
         where T : IdentifiableEntity
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
            where E : IdentifiableEntity
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
          where E : IdentifiableEntity
        {
            return new Updateable<E>(query, null);
        }

        public static IUpdateable<MListElement<E, V>> UnsafeUpdateMList<E, V>(this IQueryable<MListElement<E, V>> query)
             where E : IdentifiableEntity
        {
            return new Updateable<MListElement<E, V>>(query, null);
        }

        public static IUpdateablePart<A, E> UnsafeUpdatePart<A, E>(this IQueryable<A> query, Expression<Func<A, E>> partSelector)
            where E : IdentifiableEntity
        {
            return new UpdateablePart<A, E>(query, partSelector, null);
        }

        public static IUpdateablePart<A, MListElement<E, V>> UnsafeUpdateMListPart<A, E, V>(this IQueryable<A> query, Expression<Func<A, MListElement<E, V>>> partSelector)
            where E : IdentifiableEntity
        {
            return new UpdateablePart<A, MListElement<E, V>>(query, partSelector, null);
        }

        public static int Execute(this IUpdateable update)
        {
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
        #endregion

        #region UnsafeInsert

        public static int UnsafeInsert<T, E>(this IQueryable<T> query, Expression<Func<T, E>> constructor)
            where E : IdentifiableEntity
        {
            using (HeavyProfiler.Log("DBUnsafeInsert", () => typeof(E).TypeName()))
            {
                if (query == null)
                    throw new ArgumentNullException("query");

                if (constructor == null)
                    throw new ArgumentNullException("constructor");

                using (Transaction tr = new Transaction())
                {
                    Schema.Current.OnPreUnsafeInsert(typeof(E), query, constructor, query.Select(constructor));
                    var table = Schema.Current.Table(typeof(E));
                    int rows = DbQueryProvider.Single.Insert(query, constructor, table, sql => (int)sql.ExecuteScalar());

                    return tr.Commit(rows);
                }
            }
        }

         
        public static int UnsafeInsertMList<T, E, V>(this IQueryable<T> query, Expression<Func<E, MList<V>>> mListProperty,  Expression<Func<T, MListElement<E, V>>> constructor)
               where E : IdentifiableEntity
        {
            using (HeavyProfiler.Log("DBUnsafeInsert", () => typeof(E).TypeName()))
            {
                if (query == null)
                    throw new ArgumentNullException("query");

                if (constructor == null)
                    throw new ArgumentNullException("constructor");

                using (Transaction tr = new Transaction())
                {
                    Schema.Current.OnPreUnsafeInsert(typeof(E), query, constructor, query.Select(constructor).Select(c=>c.Parent));
                    var table = ((FieldMList)Schema.Current.Field(mListProperty)).TableMList;
                    int rows = DbQueryProvider.Single.Insert(query, constructor, table, sql => (int)sql.ExecuteScalar());

                    return tr.Commit(rows);
                }
            }
        }

        #endregion
    }

    public class MListElement<E, V> where E : IdentifiableEntity
    {
        public int RowId { get; set; }
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

        IQueryable<E> EntityQuery<E>() where E : IdentifiableEntity;
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

        public IQueryable<E> EntityQuery<E>() where E : IdentifiableEntity
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

            throw new InvalidOperationException("Impossible to convert {0} to {1}".Formato(
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

        public IQueryable<E> EntityQuery<E>() where E : IdentifiableEntity
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
}
