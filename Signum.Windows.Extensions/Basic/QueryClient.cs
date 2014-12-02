using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Reflection;
using Signum.Services;
using Signum.Entities.Basics;
using Signum.Entities.DynamicQuery;
using Signum.Entities.UserQueries;

namespace Signum.Windows
{
    public class QueryClient
    {
        public static Dictionary<string, object> queryNames;

        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                queryNames = Server.Return((IDynamicQueryServer s) => s.GetQueryNames()).ToDictionary(a => QueryUtils.GetQueryUniqueKey(a));
            }
        }

        public static object GetQueryName(string uniqueQueryName)
        {
            if (queryNames == null)
                throw new InvalidOperationException("QueryClient not initialized, call QueryClient.Start");

            return queryNames.GetOrThrow(uniqueQueryName, "Query with name '{0}' is not found");
        }

        public static QueryEntity GetQuery(object queryName)
        {
            return Server.Return((IQueryServer s) => s.GetQuery(queryName));
        }
    }   
}
