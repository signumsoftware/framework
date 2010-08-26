using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities.Reflection;
using Signum.Entities.DynamicQuery;
using Signum.Entities;
using System.Linq.Expressions;
using System.Reflection;

namespace Signum.Engine.DynamicQuery
{
    public class AutoDynamicQuery<T> : DynamicQuery<T>
    {
        public IQueryable<T> Query { get; private set; }
        Dictionary<string, Meta> metas;

        public AutoDynamicQuery(IQueryable<T> query)
        {
            if (query == null)
                throw new ArgumentNullException("query");

            this.Query = query;

            metas = DynamicQuery.QueryMetadata(query);

            InitializeColumns(mi => metas[mi.Name]);
        }

        public override ResultTable ExecuteQuery(QueryRequest request)
        {
            IQueryable<Expandable<T>> result = Query.Where(request.Filters).SelectExpandable(request.UserColumns).OrderBy(request.Orders).TryTake(request.Limit);

            Expandable<T>[] list = result.ToArray();

            return ToQueryResult(list, request.UserColumns);
        }

        public override int ExecuteQueryCount(QueryCountRequest request)
        {
            return Query.Where(request.Filters).Count();
        }

        public override Lite ExecuteUniqueEntity(UniqueEntityRequest request)
        {
            return Query.Where(request.Filters).SelectExpandable(null).OrderBy(request.Orders).SelectEntity().Unique(request.UniqueType);
        }

        public override Expression Expression
        {
            get { return Query.Expression; }
        }
    }
}
