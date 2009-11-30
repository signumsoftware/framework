using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Reflection;
using Signum.Entities;
using Signum.Engine;
using Signum.Entities.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Engine.DynamicQuery;
using Signum.Entities.Basics;
using $custommessage$.Services;

namespace $custommessage$.Web
{
    public class Server$custommessage$ : IServer$custommessage$
    {
        static T Return<T>(MethodBase mi, Func<T> function)
        {
            T t = default(T);
            Action a = () => t = function();
            Execute(mi, a);
            return t;
        }

        static void Execute(MethodBase mi, Action action)
        {
            try
            {
                //Do Security, Tracing and Logging here
                action();
            }
            catch (Exception e)
            {
                throw new FaultException(e.Message);
            }
        }

        #region IServer

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

        public List<Lazy> RetrieveAllLazy(Type liteType, Type[] types)
        {
            return Return(MethodInfo.GetCurrentMethod(),
               () => DynamicQueryUtils.RetriveAllLazy(liteType, types));
        }

        public List<Lazy> FindLazyLike(Type liteType, Type[] types, string subString, int count)
        {
            return Return(MethodInfo.GetCurrentMethod(),
                () => DynamicQueryUtils.FindLazyLike(liteType, types, subString, count));
        }

        public Type[] FindImplementations(Type liteType, MemberInfo[] members)
        {
            return Return(MethodInfo.GetCurrentMethod(),
          () => Schema.Current.FindImplementations(liteType, members));
        }

        public List<IdentifiableEntity> RetrieveAll(Type type)
        {
            return Return(MethodInfo.GetCurrentMethod(),
            () => Database.RetrieveAll(type));
        }

        public List<IdentifiableEntity> SaveList(List<IdentifiableEntity> list)
        {
            Execute( MethodInfo.GetCurrentMethod(),
            () => Database.SaveList(list));
            return list;
        }

        public QueryDescription GetQueryDescription(object queryName)
        {
            return Return(MethodInfo.GetCurrentMethod(),
            () => Queries.DynamicQueryManager.QueryDescription(queryName));
        }

        public QueryResult GetQueryResult(object queryName, List<Filter> filters, int? limit)
        {
            return Return(MethodInfo.GetCurrentMethod(),
            () => Queries.DynamicQueryManager.ExecuteQuery(queryName, filters, limit));
        }

        public List<object> GetQueryNames()
        {
            return Return( MethodInfo.GetCurrentMethod(),
            () => Queries.DynamicQueryManager.GetQueryNames());
        }

        public List<Lazy<INoteDN>> RetrieveNotes(Lazy<IdentifiableEntity> lite)
        {
            return Return(MethodInfo.GetCurrentMethod(),
            () => (from n in Database.Query<NoteDN>()
                   where n.Entity == lite
                   select n.ToLazy<INoteDN>()).ToList());
        }

        #endregion
    }
}
