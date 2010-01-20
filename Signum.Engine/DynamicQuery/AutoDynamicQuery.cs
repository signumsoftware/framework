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

            columns = MemberEntryFactory.GenerateList<T>(MemberOptions.Properties | MemberOptions.Fields)
                .Select((e, i) => new Column(i, e.MemberInfo, metas[e.MemberInfo.Name])).ToArray();
        }

        public override ResultTable ExecuteQuery(List<Filter> filters, List<Order> orders, int? limit)
        {
            IQueryable<T> result = query.WhereFilters(filters).OrderBy(orders).TryTake(limit);

            List<T> list = result.ToList(); 

            return ToQueryResult(list);
        }

        public override int ExecuteQueryCount(List<Filter> filters)
        {
            return query.WhereFilters(filters).Count();
        }

        public override Lite ExecuteUniqueEntity(List<Filter> filters, List<Order> orders, UniqueType uniqueType)
        {
            return query.WhereFilters(filters).OrderBy(orders).SelectEntity().Unique(uniqueType);
        }

        public override Expression Expression
        {
            get { return query.Expression; }
        }
    }
}
