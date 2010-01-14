using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.DynamicQuery;
using Signum.Utilities.Reflection;
using Signum.Entities;

namespace Signum.Engine.DynamicQuery
{
    public class ManualDynamicQuery<T> : DynamicQuery<T>
    {
        Func<List<Filter>, List<Order>, int?, IEnumerable<T>> execute;

        public ManualDynamicQuery(Func<List<Filter>, List<Order>, int?, IEnumerable<T>> execute)
        {
            if (execute == null)
                throw new ArgumentNullException("execute");

            this.execute = execute;

            this.columns = MemberEntryFactory.GenerateList<T>(MemberOptions.Fields | MemberOptions.Properties).Select((e, i) =>
                    new Column(i, e.MemberInfo, null)).ToArray();
        }

        public override ResultTable ExecuteQuery(List<Filter> filters, List<Order> orders, int? limit)
        {
            return ToQueryResult(execute(filters, orders, limit));
        }

        public override int ExecuteQueryCount(List<Filter> filters)
        {
            return execute(filters, null, null).Count();
        }

        public override Lite ExecuteUniqueEntity(List<Filter> filters, List<Order> orders, UniqueType uniqueType)
        {
            return execute(filters, orders, 1).SelectEntity().Unique(uniqueType);
        }
    }
}
