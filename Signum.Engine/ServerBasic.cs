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
    public abstract class ServerBasic : IBaseServer, IQueryServer
    {
        protected T Return<T>(MethodBase mi, Func<T> function)
        {
            return Return(mi, mi.Name, function);
        }
        
        protected virtual T Return<T>(MethodBase mi, string description, Func<T> function)
        {
            try
            {
                return function();
            }
            catch (Exception e)
            {
                throw new FaultException(e.Message);
            }
        }

        protected void Execute(MethodBase mi, Action action)
        {
            Return(mi, mi.Name, () => { action(); return true; });
        }

        protected void Execute(MethodBase mi, string description, Action action)
        {
            Return(mi, description, () => { action(); return true; });
        }

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

        public List<Lite> RetrieveAllLite(Type liteType, Implementations implementations)
        {
            return Return(MethodInfo.GetCurrentMethod(), "RetrieveAllLite {0}".Formato(liteType),
                 () => AutoCompleteUtils.RetriveAllLite(liteType, implementations));
        }

        public List<Lite> FindLiteLike(Type liteType, Implementations implementations, string subString, int count)
        {
            return Return(MethodInfo.GetCurrentMethod(), "FindLiteLike {0}".Formato(liteType),
                () => AutoCompleteUtils.FindLiteLike(liteType, implementations, subString, count));
        }

        public Implementations FindImplementations(PropertyRoute entityPath)
        {
            return Return(MethodInfo.GetCurrentMethod(),
                () => Schema.Current.FindImplementations(entityPath));
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

        public List<Lite<TypeDN>> TypesAssignableFrom(Type type)
        {
            return Return(MethodInfo.GetCurrentMethod(),
             () => TypeLogic.TypesAssignableFrom(type));
        }
        #endregion

        #region IQueryServer
        public QueryDescription GetQueryDescription(object queryName)
        {
            return Return(MethodInfo.GetCurrentMethod(),
             () => DynamicQueryManager.Current.QueryDescription(queryName));
        }

        public ResultTable GetQueryResult(object queryName, List<UserColumn> userColumns, List<Filter> filters, List<Order> orders, int? limit)
        {
            return Return(MethodInfo.GetCurrentMethod(), "GetQueryResult {0}".Formato(queryName),
             () => DynamicQueryManager.Current.ExecuteQuery(queryName, userColumns, filters, orders, limit));
        }

        public int GetQueryCount(object queryName, List<Filter> filters)
        {
            return Return(MethodInfo.GetCurrentMethod(), "GetQueryCount {0}".Formato(queryName),
                () => DynamicQueryManager.Current.ExecuteQueryCount(queryName, filters));
        }

        public Lite GetUniqueEntity(object queryName, List<Filter> filters, List<Order> orders, UniqueType uniqueType)
        {
            return Return(MethodInfo.GetCurrentMethod(), "GetQueryEntity {0}".Formato(queryName),
                () => DynamicQueryManager.Current.ExecuteUniqueEntity(queryName, filters, orders, uniqueType));
        }

        public List<object> GetQueryNames()
        {
            return Return(MethodInfo.GetCurrentMethod(),
             () => DynamicQueryManager.Current.GetQueryNames());
        }
        #endregion
    }
}
