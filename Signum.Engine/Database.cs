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
using Signum.Engine.Properties;

namespace Signum.Engine
{
    public static class Database
    {
        #region Save
        public static void SaveList<T>(this IEnumerable<T> entities)
            where T : class, IIdentifiable
        {
            if (entities == null || entities.Any(e => e == null))
                throw new ArgumentNullException("entity");

            using (new EntityCache())
            using (HeavyProfiler.Log("DB"))
            using (Transaction tr = new Transaction())
            {
                Saver.SaveAll(entities.Cast<IdentifiableEntity>().ToArray());

                tr.Commit();
            }
        }

        public static void SaveParams(params IIdentifiable[] entities)
        {
            if (entities == null || entities.Any(e => e == null))
                throw new ArgumentNullException("entity");

            using (new EntityCache())
            using (Transaction tr = new Transaction())
            {
                Saver.SaveAll(entities.Cast<IdentifiableEntity>().ToArray());

                tr.Commit();
            }
        }

        public static T Save<T>(this T entity)
            where T : class, IIdentifiable
        {
            if (entity == null)
                throw new ArgumentNullException("entity");

            using (new EntityCache())
            using (HeavyProfiler.Log("DB"))
            using (Transaction tr = new Transaction())
            {
                Saver.Save((IdentifiableEntity)(IIdentifiable)entity);

                return tr.Commit(entity);
            }
        }
        #endregion

        #region Retrieve
        public static T Retrieve<T>(this Lite<T> lite) where T : class, IIdentifiable
        {
            if (lite == null)
                throw new ArgumentNullException("lite");

            if (lite.EntityOrNull == null)
                lite.SetEntity(Retrieve(lite.RuntimeType, lite.Id));

            return lite.EntityOrNull;
        }

        public static T RetrieveAndForget<T>(this Lite<T> lite) where T : class, IIdentifiable
        {
            if (lite == null)
                throw new ArgumentNullException("lite");

            return (T)(object)Retrieve(lite.RuntimeType, lite.Id);
        }

        public static IdentifiableEntity Retrieve(Lite lite)
        {
            if (lite == null)
                throw new ArgumentNullException("lite");

            if (lite.UntypedEntityOrNull == null)
                lite.SetEntity(Retrieve(lite.RuntimeType, lite.Id));

            return lite.UntypedEntityOrNull;
        }

        public static IdentifiableEntity RetrieveAndForget(Lite lite)
        {
            if (lite == null)
                throw new ArgumentNullException("lite");

            return Retrieve(lite.RuntimeType, lite.Id);
        }

        public static T Retrieve<T>(int id) where T : IdentifiableEntity
        {
            return (T)Retrieve(typeof(T), id);
        }

        public static IdentifiableEntity Retrieve(Type type, int id)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            using (new EntityCache())
            using (HeavyProfiler.Log("DB"))
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
            if (type == null)
                throw new ArgumentNullException("type");

            using (new EntityCache())
            using (HeavyProfiler.Log("DB"))
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
            if (type == null)
                throw new ArgumentNullException("type");

            return RetrieveLite(type, type, id); 
        }

        public static Lite<T> RetrieveLite<T>(Type runtimeType, int id) where T : class, IIdentifiable
        {
            if (runtimeType == null)
                throw new ArgumentNullException("runtimeType");

            return (Lite<T>)RetrieveLite(typeof(T), runtimeType, id);
        }

        public static Lite<T> RetrieveLite<T>(int id) where T : class, IIdentifiable
        {
            return (Lite<T>)RetrieveLite(typeof(T), typeof(T), id);
        }

        public static Lite<T> FillToStr<T>(this Lite<T> lite)where T : class, IIdentifiable
        {
            return (Lite<T>)FillToStr((Lite)lite);
        }

        public static Lite FillToStr(Lite lite)
        {
            if (lite == null)
                return null;

            using (Transaction tr = new Transaction())
            {
                Table t = Schema.Current.Table(lite.RuntimeType);

                SqlPreCommand command = SqlBuilder.SelectToStr(t.Name, lite.Id);

                object val = Executor.ExecuteScalar(command.ToSimple());

                lite.ToStr = DBNull.Value == val ? null : (string)val;

                return tr.Commit(lite);
            }
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
            using (HeavyProfiler.Log("DB"))
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

        static readonly GenericInvoker miRetrieveAll = GenericInvoker.Create(() => RetrieveAll<TypeDN>());
        public static List<IdentifiableEntity> RetrieveAll(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            IList list = (IList)miRetrieveAll.GetInvoker(type)();
            return list.Cast<IdentifiableEntity>().ToList();
        }

        public static List<Lite<T>> RetrieveAllLite<T>()
            where T : IdentifiableEntity 
        {
            return Database.Query<T>().Select(e => e.ToLite()).ToList(); 
        }

        static readonly GenericInvoker miRetrieveAllLite = GenericInvoker.Create(() => Database.RetrieveAllLite<TypeDN>());
        public static List<Lite> RetrieveAllLite(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            IList list = (IList)miRetrieveAllLite.GetInvoker(type)();
            return list.Cast<Lite>().ToList();
        }

        public static List<T> RetrieveList<T>(List<int> ids)
            where T : class, IIdentifiable
        {
            if (ids == null)
                throw new ArgumentNullException("ids");

            return RetrieveList(typeof(T), ids).Cast<T>().ToList();
        }

        public static List<IdentifiableEntity> RetrieveList(Type type, List<int> ids)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            if (ids == null)
                throw new ArgumentNullException("ids");

            using (new EntityCache())
            using (HeavyProfiler.Log("DB"))
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
            if (ids == null)
                throw new ArgumentNullException("ids");

            return RetrieveListLite(typeof(T), ids).Cast<Lite<T>>().ToList();
        }

        public static List<Lite> RetrieveListLite(Type type, List<int> ids)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            if (ids == null)
                throw new ArgumentNullException("ids");

            using (new EntityCache())
            using (HeavyProfiler.Log("DB"))
            using (Transaction tr = new Transaction())
            {
                Retriever rec = new Retriever();
                Table table = Schema.Current.Table(type);

                List<Lite> ident = ids.Select(id => rec.GetLite(table, type, id)).ToList();

                rec.ProcessAll();

                return tr.Commit(ident);
            }
        }

        public static List<T> RetrieveFromListOfLite<T>(this IEnumerable<Lite<T>> lites)
         where T : class, IIdentifiable
        {
            if (lites == null)
                throw new ArgumentNullException("lites");

            using (new EntityCache())
            using (HeavyProfiler.Log("DB"))
            using (Transaction tr = new Transaction())
            {
                Retriever rec = new Retriever();

                List<T> ident = lites.Select(l => (T)(IIdentifiable)rec.GetIdentifiable(l, true)).ToList();

                rec.ProcessAll();

                return tr.Commit(ident);
            }
        }

        public static List<IdentifiableEntity> RetrieveFromListOfLite(IEnumerable<Lite> lites)
        {
            if (lites == null)
                throw new ArgumentNullException("lites");

            using (new EntityCache())
            using (HeavyProfiler.Log("DB"))
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
            if (type == null)
                throw new ArgumentNullException("type");

            using (HeavyProfiler.Log("DB"))
            using (Transaction tr = new Transaction())
            {
                Deleter.Delete(type, id);

                tr.Commit();
            }
        }

        public static void Delete(Type type, IEnumerable<int> ids)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            if (ids == null)
                throw new ArgumentNullException("ids");

            using (HeavyProfiler.Log("DB"))
            using (Transaction tr = new Transaction())
            {
                Deleter.Delete(type, ids.ToList());

                tr.Commit();
            }
        }


        public static void Delete<T>(int id)
            where T : IdentifiableEntity
        {
            Delete(typeof(T), id);
        }


        public static void Delete<T>(Lite<T> lite)
            where T : class, IIdentifiable
        {
            if (lite == null)
                throw new ArgumentNullException("lite");

            Delete(lite.RuntimeType, lite.Id);
        }

        public static void Delete<T>(this T ident)
            where T : IdentifiableEntity
        {
            if (ident == null)
                throw new ArgumentNullException("ident");

            Delete(ident.GetType(), ident.Id);
        }

        public static void DeleteList<T>(IEnumerable<T> collection)
            where T : IdentifiableEntity
        {
            if (collection == null)
                throw new ArgumentNullException("collection");

            if (collection.Empty()) return;

            Delete(
                collection.Select(a => a.GetType()).Distinct().Single("There are entities of different types"), 
                collection.Select(i => i.Id));
        }

        public static void DeleteList<T>(IEnumerable<Lite<T>> collection)
            where T : class, IIdentifiable
        {
            if (collection == null)
                throw new ArgumentNullException("collection");

            if (collection.Empty()) return;

            Delete(
                collection.Select(a => a.RuntimeType).Distinct().Single("There are entities of different types"),
                collection.Select(i => i.Id));
        }
        #endregion

        #region Query
        public static IQueryable<T> Query<T>()
            where T : IdentifiableEntity
        {
            return new Query<T>(DbQueryProvider.Single);
        }

        public static IQueryable<S> InDB<S>(this S entity)
            where S: IIdentifiable
        {
            if (entity == null)
                throw new ArgumentNullException("entity");

            return (IQueryable<S>)miInDB.GetInvoker(typeof(S), entity.GetType()).Invoke(entity);
        }

        static GenericInvoker miInDB = GenericInvoker.Create(() => InDB<IdentifiableEntity, IdentifiableEntity>((IdentifiableEntity)null));

        static IQueryable<S> InDB<S, RT>(S entity)
            where S : class, IIdentifiable
            where RT : IdentifiableEntity, S
        {
            if (entity == null)
                throw new ArgumentNullException("entity");

            return Database.Query<RT>().Where(rt => rt == entity).Select(rt => (S)rt);
        }

        public static IQueryable<S> InDB<S>(this Lite<S> lite)
           where S : class, IIdentifiable
        {
            if (lite == null)
                throw new ArgumentNullException("lite");

            return (IQueryable<S>)miInDBLite.GetInvoker(typeof(S), lite.RuntimeType).Invoke(lite);
        }

        static GenericInvoker miInDBLite = GenericInvoker.Create(() => InDB<IdentifiableEntity, IdentifiableEntity>((Lite<IdentifiableEntity>)null));

        static IQueryable<S> InDB<S, RT>(Lite<S> lite)
            where S : class, IIdentifiable
            where RT : IdentifiableEntity, S
        {
            if (lite == null)
                throw new ArgumentNullException("lite");

            return Database.Query<RT>().Where(rt => rt.ToLite() == lite.ToLite<RT>()).Select(rt => (S)rt);
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

            int rows = DbQueryProvider.Single.Delete<T>(query);
            return rows;
        }


        /// <param name="update">Use a object initializer to make the update (no entity will be created)</param>
        public static int UnsafeUpdate<T>(this IQueryable<T> query, Expression<Func<T, T>> update)
            where T : IdentifiableEntity
        {
            if (query == null)
                throw new ArgumentNullException("query");

            int rows = DbQueryProvider.Single.Update<T>(query, update);
            return rows;
        }


        [ThreadStatic]
        static bool userInterface;

        public static bool UserInterface { get { return userInterface; } }

        public static IDisposable ForUserInterface()
        {
            var oldValue = UserInterface;
            userInterface = true;
            return new Disposable(() => userInterface = oldValue);
        }

        public static void SetUserInterface(bool value)
        {
            userInterface = value; 
        }
    }
}
