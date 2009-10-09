using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.DynamicQuery;
using Signum.Entities;
using Signum.Engine.DynamicQuery;
using System.Reflection;
using System.ServiceModel;
using Signum.Engine;
using Signum.Engine.Maps;
using Signum.Entities.Basics;
using Signum.Utilities;

namespace Signum.Services
{
    public abstract class ServerBasic : IBaseServer, IQueryServer, INotesServer, IAlertsServer
    {
        protected T Return<T>(MethodBase mi, string description, Func<T> function)
        {
            T t = default(T);
            Action a = () => t = function();
            Execute(mi, description, a);
            return t;
        }

        protected T Return<T>(MethodBase mi, Func<T> function)
        {
            T t = default(T);
            Action a = () => t = function();
            Execute(mi, mi.Name, a);
            return t;
        }

        protected void Execute(MethodBase mi, Action action)
        {
            Execute(mi, mi.Name, action);
        }

        protected virtual void Execute(MethodBase mi, string description, Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                throw new FaultException(e.Message);
            }
        }

        protected abstract DynamicQueryManager GetQueryManager();

        #region IBaseServer
        public IdentifiableEntity Retrieve(Type type, int id)
        {
            return Return(MethodInfo.GetCurrentMethod(), "Retrieve {0}".Formato(type.Name),
             () => Database.Retrieve(type, id));
        }

        public IdentifiableEntity Save(IdentifiableEntity entidad)
        {
            return Return(MethodInfo.GetCurrentMethod(), "Save {0}".Formato(entidad.GetType()),
             () => { Database.Save(entidad); return entidad; });
        }

        public List<Lazy> RetrieveAllLazy(Type lazyType, Type[] types)
        {
            return Return(MethodInfo.GetCurrentMethod(), "RetrieveAllLazy {0}".Formato(lazyType),
             () => AutoCompleteUtils.RetriveAllLazy(lazyType, types));
        }

        public List<Lazy> FindLazyLike(Type lazyType, Type[] types, string subString, int count)
        {
            return Return(MethodInfo.GetCurrentMethod(), "FindLazyLike {0}".Formato(lazyType),
             () => AutoCompleteUtils.FindLazyLike(lazyType, types, subString, count));
        }

        public Type[] FindImplementations(Type lazyType, MemberInfo[] members)
        {
            return Return(MethodInfo.GetCurrentMethod(),
             () => Schema.Current.FindImplementations(lazyType, members));
        }

        public List<IdentifiableEntity> RetrieveAll(Type type)
        {
            return Return(MethodInfo.GetCurrentMethod(), "RetrieveAll {0}".Formato(type),
            () => Database.RetrieveAll(type));
        }

        public List<IdentifiableEntity> SaveList(List<IdentifiableEntity> list)
        {
            Execute(MethodInfo.GetCurrentMethod(),
            () => Database.SaveList(list));
            return list;
        }

        public Dictionary<Type, TypeDN> ServerTypes()
        {
            return Return(MethodInfo.GetCurrentMethod(),
                () => TypeLogic.TypeToDN);
        }

        public DateTime ServerNow()
        {
            return Return(MethodInfo.GetCurrentMethod(),
                () => DateTime.Now);
        }

        public List<Lazy<TypeDN>> TypesAssignableFrom(Type type)
        {
            return Return(MethodInfo.GetCurrentMethod(),
             () => TypeLogic.TypesAssignableFrom(type));
        }
        #endregion

        #region IQueryServer
        public QueryDescription GetQueryDescription(object queryName)
        {
            return Return(MethodInfo.GetCurrentMethod(),
             () => GetQueryManager().QueryDescription(queryName));
        }

        public QueryResult GetQueryResult(object queryName, List<Filter> filters, int? limit)
        {
            return Return(MethodInfo.GetCurrentMethod(), "GetQueryResult {0}".Formato(queryName),
             () => GetQueryManager().ExecuteQuery(queryName, filters, limit));
        }

        public List<object> GetQueryNames()
        {
            return Return(MethodInfo.GetCurrentMethod(),
             () => GetQueryManager().GetQueryNames());
        }
        #endregion

        #region INotesServer Members
        public virtual List<Lazy<INoteDN>> RetrieveNotes(Lazy<IdentifiableEntity> lazy)
        {
            return Return(MethodInfo.GetCurrentMethod(),
             () => (from n in Database.Query<NoteDN>()
                    where n.Entity == lazy
                    select n.ToLazy<INoteDN>()).ToList());
        }
        #endregion

        #region IAlertsServer Members

        public virtual List<Lazy<IAlert>> RetrieveAlerts(Lazy<IdentifiableEntity> lazy)
        {
            return Return(MethodInfo.GetCurrentMethod(),
             () => (from n in Database.Query<Alert>()
                    where n.Entity == lazy
                    select n.ToLazy<IAlert>()).ToList());
        }

        #endregion
    }
}
