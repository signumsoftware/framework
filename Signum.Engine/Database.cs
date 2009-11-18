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

namespace Signum.Engine
{
    public static class Database
    {
        #region Save
        public static void SaveList<T>(this IEnumerable<T> entities)
            where T : class, IIdentifiable
        {
            using (new EntityCache())
            using (Transaction tr = new Transaction())
            {
                Saver.SaveAll(entities.Cast<IdentifiableEntity>().ToArray());

                tr.Commit();
            }
        }

        public static void SaveParams(params IIdentifiable[] entities)
        {
            using (new EntityCache())
            using (Transaction tr = new Transaction())
            {
                Saver.SaveAll(entities.Cast<IdentifiableEntity>().ToArray());

                tr.Commit();
            }
        }

        public static T Save<T>(this T obj)
            where T : class, IIdentifiable
        {
            using (new EntityCache())
            using (Transaction tr = new Transaction())
            {
                Saver.Save((IdentifiableEntity)(IIdentifiable)obj);

                return tr.Commit(obj);
            }
        }
        #endregion

        #region Retrieve
        public static T Retrieve<T>(this Lite<T> lite) where T : class, IIdentifiable
        {
            return lite.EntityOrNull ?? (lite.EntityOrNull = (T)(object)Retrieve(lite.RuntimeType, lite.Id));
        }

        public static T RetrieveAndForget<T>(this Lite<T> lite) where T : class, IIdentifiable
        {
            return (T)(object)Retrieve(lite.RuntimeType, lite.Id);
        }

        public static IdentifiableEntity Retrieve(Lite lite)
        {
            return lite.UntypedEntityOrNull ?? (lite.UntypedEntityOrNull = Retrieve(lite.RuntimeType, lite.Id));
        }

        public static IdentifiableEntity RetrieveAndForget(Lite lite)
        {
            return Retrieve(lite.RuntimeType, lite.Id);
        }

        public static T Retrieve<T>(int id) where T : IdentifiableEntity
        {
            return (T)Retrieve(typeof(T), id);
        }

        public static IdentifiableEntity Retrieve(Type type, int id)
        {
            using (new EntityCache())
            using (Transaction tr = new Transaction())
            {
                Retriever rec = new Retriever();
                Table table = Schema.Current.Table(type);
                IdentifiableEntity ident = rec.GetIdentifiable(table, id, true);

                rec.ProcessAll();

                return tr.Commit(ident);
            }
        }

        public static Lite RetrieveLite(Type type, Type runtimeType, int id)
        {
            using (new EntityCache())
            using (Transaction tr = new Transaction())
            {
                Retriever rec = new Retriever();
                Table table = Schema.Current.Table(runtimeType);
                Lite lite = rec.GetLite(table, type, id);

                rec.ProcessAll();

                return tr.Commit(lite);
            }
        }

        public static Lite RetrieveLite(Type type, int id)
        {
            return RetrieveLite(type, type, id); 
        }

        public static Lite<T> RetrieveLite<T>(Type runtimeType, int id) where T : class, IIdentifiable
        {
            return (Lite<T>)RetrieveLite(typeof(T), runtimeType, id);
        }

        public static Lite<T> RetrieveLite<T>(int id) where T : class, IIdentifiable
        {
            return (Lite<T>)RetrieveLite(typeof(T), typeof(T), id);
        }
        #endregion

        #region Exists
        public static bool Exists<T>(int id)
            where T : IdentifiableEntity
        {
            return Exists(typeof(T), id);
        }

        public static bool Exists(Type type, int id)
        {
            using (Transaction tr = new Transaction())
            {
                Table t = Schema.Current.Table(type);

                SqlPreCommand command = SqlBuilder.SelectCount(t.Name, id);

                int? count = (int?)Executor.ExecuteScalar(command.ToSimple());

                return tr.Commit(count == 1);
            }
        }
        #endregion

        #region Retrieve All Lists Lites
        public static List<T> RetrieveAll<T>()
            where T : IdentifiableEntity
        {
            return Database.Query<T>().ToList();
        }

        static readonly MethodInfo miRetrieveAll = ReflectionTools.GetMethodInfo(()=>RetrieveAll<TypeDN>()).GetGenericMethodDefinition();
        public static List<IdentifiableEntity> RetrieveAll(Type type)
        {
            IList list = (IList)miRetrieveAll.MakeGenericMethod(type).Invoke(null, null);
            return list.Cast<IdentifiableEntity>().ToList();
        }

        public static List<Lite<T>> RetrieveAllLite<T>()
            where T : IdentifiableEntity 
        {
            return Database.Query<T>().Select(e => e.ToLite()).ToList(); 
        }

        static readonly MethodInfo miRetrieveAllLite = ReflectionTools.GetMethodInfo(() => Database.RetrieveAllLite<TypeDN>()).GetGenericMethodDefinition();
        public static List<Lite> RetrieveAllLite(Type type)
        {
            IList list = (IList)miRetrieveAllLite.MakeGenericMethod(type).Invoke(null, null);
            return list.Cast<Lite>().ToList();
        }

        public static List<T> RetrieveList<T>(List<int> ids)
            where T : class, IIdentifiable
        {
            return RetrieveList(typeof(T), ids).Cast<T>().ToList();
        }

        public static List<IdentifiableEntity> RetrieveList(Type type, List<int> ids)
        {
            using (new EntityCache())
            using (Transaction tr = new Transaction())
            {
                Retriever rec = new Retriever();
                Table table = Schema.Current.Table(type);

                List<IdentifiableEntity> ident = ids.Select(id => rec.GetIdentifiable(table, id, true)).ToList();

                rec.ProcessAll();

                return tr.Commit(ident);
            }
        }

        public static List<Lite<T>> RetrieveListLite<T>(List<int> ids)
            where T : class, IIdentifiable
        {
            return RetrieveListLite(typeof(T), ids).Cast<Lite<T>>().ToList();
        }

        public static List<Lite> RetrieveListLite(Type type, List<int> ids)
        {
            using (new EntityCache())
            using (Transaction tr = new Transaction())
            {
                Retriever rec = new Retriever();
                Table table = Schema.Current.Table(type);

                List<Lite> ident = ids.Select(id => rec.GetLite(table, type, id)).ToList();

                rec.ProcessAll();

                return tr.Commit(ident);
            }
        }

        public static List<T> RetrieveFromListOfLite<T>(List<Lite<T>> lites)
         where T : class, IIdentifiable
        {
            using (new EntityCache())
            using (Transaction tr = new Transaction())
            {
                Retriever rec = new Retriever();

                List<T> ident = lites.Select(l => (T)(IIdentifiable)rec.GetIdentifiable(l, true)).ToList();

                rec.ProcessAll();

                return tr.Commit(ident);
            }
        }

        public static List<IdentifiableEntity> RetrieveFromListOfLite(List<Lite> lites)
        {
            using (new EntityCache())
            using (Transaction tr = new Transaction())
            {
                Retriever rec = new Retriever();

                List<IdentifiableEntity> ident = lites.Select(l => rec.GetIdentifiable(l, true)).ToList();

                rec.ProcessAll(); 

                return tr.Commit(ident);
            }
        }

        #endregion

        #region Delete
        public static void Delete(Type type, int id)
        {
            using (Transaction tr = new Transaction())
            {
                Deleter.Delete(type, id);

                tr.Commit();
            }
        }

        public static void Delete(Type type, IEnumerable<int> ids)
        {
            using (Transaction tr = new Transaction())
            {
                Deleter.Delete(type, ids.ToList());

                tr.Commit();
            }
        }

        public static void Delete<T>(this T ident)
            where T : IdentifiableEntity
        {
            Delete(ident.GetType(), ident.Id);
        }

        public static void Delete<T>(IEnumerable<T> collection)
            where T : IdentifiableEntity
        {
            Delete(collection.Select(a => a.GetType()).Single(), collection.Select(i => i.Id));
        }
        #endregion

        #region Query
        public static IQueryable<T> Query<T>()
            where T : IdentifiableEntity
        {
            return new Query<T>(DbQueryProvider.Single);
        }

        public static IQueryable<T> View<T>()
            where T : IView
        {
            return new Query<T>(DbQueryProvider.Single);
        }
        #endregion

        public static int MaxParameters { get { return SqlBuilder.MaxParametersInSQL; } }

        public static int UnsafeDelete<T>(this IQueryable<T> query)
              where T : IdentifiableEntity
        {
            if (query == null)
                throw new ArgumentNullException("query");

            return DbQueryProvider.Single.Delete<T>(query);
        }

        public static int UnsafeUpdate<T>(this IQueryable<T> query, Expression<Func<T, T>> update)
            where T : IdentifiableEntity
        {
            if (query == null)
                throw new ArgumentNullException("query");

            return DbQueryProvider.Single.Update<T>(query, update);
        }
    }
}
