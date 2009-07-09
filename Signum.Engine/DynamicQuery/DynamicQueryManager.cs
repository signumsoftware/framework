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
        internal DynamicQueryManager Parent{get; private set;}
        Dictionary<object, IQueryable> queries = new Dictionary<object, IQueryable>();

        public static event Func<object, bool> AllowQuery;

        public DynamicQueryManager(DynamicQueryManager parent)
        {
            this.Parent = parent;         
        }

        public IQueryable this[object queryName]
        {
            get
            {
                AssertQueryAllowed(queryName); 

                return TryGet(queryName).ThrowIfNullC(Resources.TheView0IsNotOnQueryManager.Formato(queryName));
            }
            set { queries[queryName] = value; }
        }

        public IQueryable TryGet(object queryName)
        {
            var result = queries.TryGetC(queryName);
            if (result != null)
                return result;

            if (Parent != null)
                return Parent[queryName];

            return null; 
        }
    
        public QueryDescription QueryDescription(object queryName)
        {
            return DynamicQueryUtils.ViewDescription(this[queryName]);
        }

        public List<object> GetQueryNames()
        {
            if (Parent == null)
                return queries.Keys.ToList();

            return queries.Keys.Union(Parent.GetQueryNames()).Where(qn=>OnAllowQuery(qn)).ToList(); 
        }

        public QueryResult ExecuteQuery(object queryName, List<Filter> filter, int? limit)
        {
            IQueryable q = this[queryName];
            Type parameter = DynamicQueryUtils.ExtractQueryType(q);

            MethodInfo mi = DynamicQueryUtils.miExecuteQueryGeneric.MakeGenericMethod(parameter);
            try
            {
                return (QueryResult)mi.Invoke(null, new object[] { q, filter, limit });
            }
            catch (TargetInvocationException te)
            {
                throw te.InnerException;
            }
        }

        static void AssertQueryAllowed(object queryName)
        {
            if (!OnAllowQuery(queryName))
                throw new ApplicationException("You have no permissions for Query: {0} ".Formato(queryName)); 
        }

        static bool OnAllowQuery(object queryName)
        {
            if (AllowQuery != null)
                return AllowQuery(queryName);
            return true;
        }
    }
}
