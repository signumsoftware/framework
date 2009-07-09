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

namespace Signum.Services
{
    public abstract class ServerBasic : IBaseServer, IQueryServer
    {
        protected T Return<T>(MethodBase mi, Func<T> function)
        {
            T t = default(T);
            Action a = () => t = function();
            Execute(mi, a);
            return t;
        }

        protected virtual void Execute(MethodBase mi, Action action)
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
            return Return(MethodInfo.GetCurrentMethod(),
             () => Database.Retrieve(type, id));
        }

        public IdentifiableEntity Save(IdentifiableEntity entidad)
        {
            return Return(MethodInfo.GetCurrentMethod(),
             () => { Database.Save(entidad); return entidad; });
        }

        public List<Lazy> RetrieveAllLazy(Type lazyType, Type[] types)
        {
            return Return(MethodInfo.GetCurrentMethod(),
             () => DynamicQueryUtils.RetriveAllLazy(lazyType, types));
        }

        public List<Lazy> FindLazyLike(Type lazyType, Type[] types, string subString, int count)
        {
            return Return(MethodInfo.GetCurrentMethod(),
             () => DynamicQueryUtils.FindLazyLike(lazyType, types, subString, count));
        }

        public Type[] FindImplementations(Type lazyType, MemberInfo[] members)
        {
            return Return(MethodInfo.GetCurrentMethod(),
             () => Schema.Current.FindImplementations(lazyType, members));
        }

        public List<IdentifiableEntity> RetrieveAll(Type type)
        {
            return Return(MethodInfo.GetCurrentMethod(),
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
        #endregion

        #region IQueryServer
        public QueryDescription GetQueryDescription(object queryName)
        {
            return Return(MethodInfo.GetCurrentMethod(),
             () => GetQueryManager().QueryDescription(queryName));
        }

        public QueryResult GetQueryResult(object queryName, List<Filter> filters, int? limit)
        {
            return Return(MethodInfo.GetCurrentMethod(),
             () => GetQueryManager().ExecuteQuery(queryName, filters, limit));
        }

        public List<object> GetQueryNames()
        {
            return Return(MethodInfo.GetCurrentMethod(),
             () => GetQueryManager().GetQueryNames());
        }
        #endregion
    }
}
