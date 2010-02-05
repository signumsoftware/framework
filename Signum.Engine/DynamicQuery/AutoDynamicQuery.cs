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

            this.StaticColumns = MemberEntryFactory.GenerateList<T>(MemberOptions.Properties | MemberOptions.Fields)
                .Select((e, i) => new StaticColumn(i, e.MemberInfo, metas[e.MemberInfo.Name], CreateGetter(e.MemberInfo))).ToArray();
        }

        public override ResultTable ExecuteQuery(List<UserColumn> userColumns, List<Filter> filters, List<Order> orders, int? limit)
        {
            IQueryable<Expandable<T>> result = query.SelectExpandable(userColumns).Where(filters).OrderBy(orders).TryTake(limit);

            Expandable<T>[] list = result.ToArray();

            return ToQueryResult(list, userColumns);
        }

        public override int ExecuteQueryCount(List<Filter> filters)
        {
            return query.SelectExpandable(null).Where(filters).Count();
        }

        public override Lite ExecuteUniqueEntity(List<Filter> filters, List<Order> orders, UniqueType uniqueType)
        {
            return query.SelectExpandable(null).Where(filters).OrderBy(orders).SelectEntity().Unique(uniqueType);
        }

        public override Expression Expression
        {
            get { return query.Expression; }
        }
    }
}
