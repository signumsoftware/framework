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
        IQueryable<T> query;
        Dictionary<string, Meta> metas;

        public AutoDynamicQuery(IQueryable<T> query)
        {
            if (query == null)
                throw new ArgumentNullException("query");

            this.query = query;

            metas = DynamicQuery.QueryMetadata(query);

            InitializeColumns(mi => metas[mi.Name]);
        }

        public override ResultTable ExecuteQuery(QueryRequest request)
        {
            IQueryable<Expandable<T>> result = query.SelectExpandable(request.UserColumns).Where(request.Filters).OrderBy(request.Orders).TryTake(request.Limit);

            Expandable<T>[] list = result.ToArray();

            return ToQueryResult(list, request.UserColumns);
        }

        public override int ExecuteQueryCount(QueryCountRequest request)
        {
            return query.SelectExpandable(null).Where(request.Filters).Count();
        }

        public override Lite ExecuteUniqueEntity(UniqueEntityRequest request)
        {
            return query.SelectExpandable(null).Where(request.Filters).OrderBy(request.Orders).SelectEntity().Unique(request.UniqueType);
        }

        public override Expression Expression
        {
            get { return query.Expression; }
        }
    }
}
