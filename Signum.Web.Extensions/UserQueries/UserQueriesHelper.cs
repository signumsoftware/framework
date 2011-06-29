using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Signum.Entities.Reports;
using System.Web.Mvc;
using Signum.Entities.DynamicQuery;

namespace Signum.Web.UserQueries
{
    public static class UserQueriesHelper
    {
        public static MvcHtmlString SearchControl(this HtmlHelper helper, UserQueryDN userQuery, FindOptions findOptions, Context context)
        {
            if (findOptions == null)
                throw new ArgumentNullException("findOptions");

            findOptions.ApplyUserQuery(userQuery);
            
            return helper.SearchControl(findOptions, context);
        }

        public static MvcHtmlString SearchControl(this HtmlHelper helper, UserQueryDN userQuery, Context context)
        {
            FindOptions findOptions = userQuery.ToFindOptions();

            return helper.SearchControl(userQuery, findOptions, context);
        }

        public static MvcHtmlString CountSearchControl(this HtmlHelper helper, UserQueryDN userQuery, FindOptions findOptions, string prefix)
        {
            if (findOptions == null)
                throw new ArgumentNullException("findOptions");

            findOptions.ApplyUserQuery(userQuery); 

            return helper.CountSearchControl(findOptions, prefix);
        }

        public static MvcHtmlString CountSearchControl(this HtmlHelper helper, UserQueryDN userQuery, string prefix)
        {
            FindOptions findOptions = userQuery.ToFindOptions();

            return helper.CountSearchControl(userQuery, findOptions, prefix);
        }
    }
}
