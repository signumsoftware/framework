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
        public Func<QueryRequest, IEnumerable<Expandable<T>>> Execute { get; private set; }

        public ManualDynamicQuery(Func<QueryRequest, IEnumerable<Expandable<T>>> execute)
        {
            if (execute == null)
                throw new ArgumentNullException("execute");

            this.Execute = execute;

            InitializeColumns(mi => null);
        }

        public override ResultTable ExecuteQuery(QueryRequest request)
        {
            Expandable<T>[] list = Execute(request).ToArray();

            return ToQueryResult(list, request.UserColumns);
        }

        public override int ExecuteQueryCount(QueryCountRequest request)
        {
            var req = new QueryRequest
            {
                QueryName = request.QueryName,
                Filters = request.Filters,
            };

            return Execute(req).Count();
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

            return Execute(req).SelectEntity().Unique(request.UniqueType);
        }
    }
}
