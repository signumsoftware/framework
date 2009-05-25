using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Data;
using System.Reflection;
using Signum.Entities.DynamicQuery;
using Signum.Engine.Properties;
using Signum.Entities;

namespace Signum.Engine.DynamicQuery
{
    public class DynamicQueryManager
    {
        public Dictionary<object, IQueryable> Queries = new Dictionary<object, IQueryable>();

        public IQueryable this[object type]
        {
            get { return Queries[type]; }
            set { Queries[type] = value; }
        }
    
        public QueryDescription QueryDescription(object queryName)
        {
            IQueryable q = Queries.GetOrThrow(queryName, Resources.TheView0IsNotOnQueryManager);

            return ViewDescription(q);
        }

        public static QueryDescription ViewDescription(IQueryable q)
        {
            Type parameter = ExtractQueryType(q);

            return DynamicQueryUtils.GetViewDescription(parameter);
        }

        public List<object> GetQueryNames()
        {
            return Queries.Keys.ToList();
        }

        private static Type ExtractQueryType(IQueryable q)
        {
            Type parameter = q.GetType().GetInterfaces()
                .Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQueryable<>))
                .GetGenericArguments()[0];
            return parameter;
        }

        static MethodInfo miExecuteQueryGeneric = typeof(DynamicQueryManager).GetMethod("ExecuteQueryGeneric");

        public QueryResult ExecuteQuery(object queryName, List<Filter> filter, int? limit)
        {
            IQueryable q = Queries.GetOrThrow(queryName, Resources.TheView0IsNotOnQueryManager);
            Type parameter = ExtractQueryType(q);

            MethodInfo mi = miExecuteQueryGeneric.MakeGenericMethod(parameter);
            try
            {
                return (QueryResult)mi.Invoke(null, new object[] { q, filter, limit });
            }
            catch (TargetInvocationException te)
            {
                throw te.InnerException;
            }
        }

        public static QueryResult ExecuteQueryGeneric<T>(IQueryable<T> query, List<Filter> filter, int? limit)
        {
            var f = DynamicQueryUtils.GetWhereExpression<T>(filter);
            if (f != null)
                query = query.Where(f);

            if (limit != null)
                query = query.Take(limit.Value); 

            return query.ToView();
        }
    }
}
