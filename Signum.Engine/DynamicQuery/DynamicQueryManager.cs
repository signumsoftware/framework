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
                return TryGet(queryName).ThrowIfNullC(Resources.TheView0IsNotOnQueryManager.Formato(queryName));
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

        public ResultTable ExecuteQuery(object queryName, List<UserColumn> userColumns, List<Filter> filters, List<Order> orders, int? limit)
        {
            return this[queryName].ExecuteQuery(userColumns, filters, orders, limit);
        }

        public int ExecuteQueryCount(object queryName, List<Filter> filters)
        {
            return this[queryName].ExecuteQueryCount(filters);
        }

        public Lite ExecuteUniqueEntity(object queryName, List<Filter> filters, List<Order> orders, UniqueType uniqueType)
        {
            return this[queryName].ExecuteUniqueEntity(filters, orders, uniqueType);
        }

        public QueryDescription QueryDescription(object queryName)
        {
            return this[queryName].GetDescription();
        }


        public event Func<object, bool> AllowQuery;

        public bool QueryAllowed(object queryName)
        {
            if (AllowQuery == null)
                return true;

            return AllowQuery(queryName);
        }

        public void AssertQueryAllowed(object queryName)
        {
            if(QueryAllowed(queryName))
                throw new UnauthorizedAccessException(Resources.AccessToQuery0IsNotAllowed.Formato(queryName));
        }

        public List<object> GetAllowedQueryNames()
        {
            return queries.Keys.Where(QueryAllowed).ToList();
        }

        public List<object> GetQueryNames()
        {
            return queries.Keys.ToList();
        }

        public Dictionary<object, IDynamicQuery> GetQueryNames(Type entityType)
        {
            return queries.Where(kvp => kvp.Value.EntityCleanType() == entityType).ToDictionary();
        }

        public string Errors(object queryName)
        {
            try
            {
                IDynamicQuery dq = this[queryName];

                Connection.CommandCount = 0;
                ResultTable result = dq.ExecuteQuery(null, null, null, 100);

                if(result.Rows.Length == 0)
                    return "Warning {0}: No results".Formato(queryName);

                if (Connection.CommandCount != 1)
                    return "Error {0}: N + 1 query problem".Formato(queryName);

                return null;
            }
            catch (Exception e)
            {
                return "Error {0}: {1}".Formato(queryName, e.Message);
            }
        }

        internal void SetSchemaImplementations(Schema schema)
        {
            foreach (var dm in queries.Values)
            {
                foreach (var col in dm.StaticColumns)
                {
                    if (col.PropertyRoute != null && col.Implementations == null)
                        col.Implementations = schema.FindImplementations(col.PropertyRoute); 
                }
            }
        }
    }
}
