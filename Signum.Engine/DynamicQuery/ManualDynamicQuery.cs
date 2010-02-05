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
        Func<List<UserColumn>, List<Filter>, List<Order>, int?, IEnumerable<Expandable<T>>> execute;

        public ManualDynamicQuery(Func<List<UserColumn>, List<Filter>, List<Order>, int?, IEnumerable<Expandable<T>>> execute)
        {
            if (execute == null)
                throw new ArgumentNullException("execute");

            this.execute = execute;

            InitializeColumns(mi => null);
        }

        public override ResultTable ExecuteQuery(List<UserColumn> userColumns, List<Filter> filters, List<Order> orders, int? limit)
        {
            Expandable<T>[] list = execute(userColumns, filters, orders, limit).ToArray();

            return ToQueryResult(list, userColumns);
        }

        public override int ExecuteQueryCount(List<Filter> filters)
        {
            return execute(null, filters, null, null).Count();
        }

        public override Lite ExecuteUniqueEntity(List<Filter> filters, List<Order> orders, UniqueType uniqueType)
        {
            return execute(null, filters, orders, 1).SelectEntity().Unique(uniqueType);
        }
    }
}
