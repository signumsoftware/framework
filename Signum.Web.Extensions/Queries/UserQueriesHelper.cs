using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Signum.Entities.Reports;
using System.Web.Mvc;
using Signum.Entities.DynamicQuery;

namespace Signum.Web.Extensions.Queries
{
    public static class UserQueriesHelper
    {
        public static void SearchControl(this HtmlHelper helper, UserQueryDN userQuery, FindOptions findOptions, Context context)
        {
            if (findOptions == null)
                throw new ArgumentNullException("findOptions");

            findOptions.FilterOptions = userQuery.Filters.Select(qf => new FilterOption { ColumnName = qf.TokenString, Token = qf.Token, Operation = qf.Operation, Value = qf.Value }).ToList();
            findOptions.UserColumnOptions = userQuery.Columns.Select((qc, index) => new UserColumnOption { DisplayName = qc.DisplayName, UserColumn = new UserColumn(index, qc.Token) }).ToList();
            findOptions.OrderOptions = userQuery.Orders.Select(qo => new OrderOption { Token = qo.Token, ColumnName = qo.TokenString, Type = qo.OrderType }).ToList();
            findOptions.Top = userQuery.MaxItems;
            
            helper.SearchControl(findOptions, context);
        }

        public static void SearchControl(this HtmlHelper helper, UserQueryDN userQuery, Context context)
        {
            helper.SearchControl(userQuery, new FindOptions(Navigator.ResolveQueryFromToStr(userQuery.Query.Key)), context);
        }
    }
}
