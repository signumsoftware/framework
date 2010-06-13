using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.DynamicQuery;
using Signum.Utilities.Reflection;
using Signum.Entities;
using System.Linq.Expressions;
using System.Reflection;

namespace Signum.Engine.DynamicQuery
{
    public class ManualDynamicQuery<T> : DynamicQuery<T>
    {
        Func<QueryRequest, IEnumerable<Expandable<T>>> execute;

        public ManualDynamicQuery(Func<QueryRequest, IEnumerable<Expandable<T>>> execute)
        {
            if (execute == null)
                throw new ArgumentNullException("execute");

            this.execute = execute;

            InitializeColumns(mi => null);
        }

        public override ResultTable ExecuteQuery(QueryRequest request)
        {
            Expandable<T>[] list = execute(request).ToArray();

            return ToQueryResult(list, request.UserColumns);
        }

        public override int ExecuteQueryCount(QueryCountRequest request)
        {
            var req = new QueryRequest
            {
                QueryName = request.QueryName,
                Filters = request.Filters,
            };

            return execute(req).Count();
        }

        public override Lite ExecuteUniqueEntity(UniqueEntityRequest request)
        {
            var req = new QueryRequest
            {
                QueryName = request.QueryName,
                Filters = request.Filters,
                Limit = 1,
                Orders = request.Orders,
            };

            return execute(req).SelectEntity().Unique(request.UniqueType);
        }
    }
}
