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
using System.Diagnostics;
using Signum.Engine.Maps;
using Signum.Services;

namespace Signum.Engine.DynamicQuery
{
    public class DynamicQueryManager
    {
        public static DynamicQueryManager Current
        {
            get { return ConnectionScope.Current.DynamicQueryManager; }

        }

        Dictionary<object, IDynamicQuery> queries = new Dictionary<object, IDynamicQuery>();

        public IDynamicQuery this[object queryName]
        {
            get
            {
                AssertQueryAllowed(queryName);
                return queries.GetOrThrow(queryName, "The query {0} is not on registered");
            }
            set
            {
                queries[queryName] = value;
            }
        }

        IDynamicQuery TryGet(object queryName)
        {
            AssertQueryAllowed(queryName); 
            return queries.TryGetC(queryName);
        }

        public ResultTable ExecuteQuery(QueryRequest request)
        {
            return this[request.QueryName].ExecuteQuery(request);
        }

        public int ExecuteQueryCount(QueryCountRequest request)
        {
            return this[request.QueryName].ExecuteQueryCount(request);
        }

        public Lite ExecuteUniqueEntity(UniqueEntityRequest request)
        {
            return this[request.QueryName].ExecuteUniqueEntity(request);
        }

        public QueryDescription QueryDescription(object queryName)
        {
            return this[queryName].GetDescription(queryName);
        }

        public event Func<object, bool> AllowQuery;

        public bool QueryAllowed(object queryName)
        {
            if (AllowQuery == null)
                return true;

            return AllowQuery(queryName);
        }

        public bool QueryDefined(object queryName)
        {
            return this.queries.ContainsKey(queryName);
        }

        public bool QueryDefinedAndAllowed(object queryName)
        {
            return QueryDefined(queryName) && QueryAllowed(queryName);
        }

        public void AssertQueryAllowed(object queryName)
        {
            if(!QueryAllowed(queryName))
                throw new UnauthorizedAccessException("Access to query {0} not allowed".Formato(queryName));
        }

        public List<object> GetAllowedQueryNames()
        {
            return queries.Keys.Where(QueryAllowed).ToList();
        }

        public List<object> GetQueryNames()
        {
            return queries.Keys.ToList();
        }

        public Dictionary<object, IDynamicQuery> GetQueries(Type entityType)
        {
            return queries.Where(kvp => kvp.Value.EntityColumn().CompatibleWith(entityType)).ToDictionary();
        }

        public Dictionary<object, IDynamicQuery> GetQueries()
        {
            return queries.ToDictionary();
        }

        public string Errors(object queryName)
        {
            try
            {
                IDynamicQuery dq = this[queryName];

                Connection.CommandCount = 0;
                ResultTable result = dq.ExecuteQuery(new QueryRequest { QueryName = queryName, Limit = 100 });

                if(result.Rows.Length == 0)
                    return "Warning {0}: No Results".Formato(queryName);

                if (Connection.CommandCount != 1)
                    return "Error {0}: N + 1 query problem".Formato(queryName);

                return null;
            }
            catch (Exception e)
            {
                return "Error {0}: {1}".Formato(queryName, e.Message);
            }
        }
    }
}
