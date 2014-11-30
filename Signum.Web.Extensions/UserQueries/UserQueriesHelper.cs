using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Signum.Entities.DynamicQuery;
using Signum.Entities.UserQueries;
using Signum.Utilities;

namespace Signum.Web.UserQueries
{
    public static class UserQueriesHelper
    {
        public static MvcHtmlString SearchControl(this HtmlHelper helper, UserQueryEntity userQuery, FindOptions findOptions, Context context, Action<SearchControl> searchControl = null)
        {
            if (findOptions == null)
                throw new ArgumentNullException("findOptions");

            findOptions.ApplyUserQuery(userQuery);

            return helper.SearchControl(findOptions, context, searchControl);
        }

        public static MvcHtmlString SearchControl(this HtmlHelper helper, UserQueryEntity userQuery, Context context, Action<SearchControl> searchControl = null)
        {
            FindOptions findOptions = userQuery.ToFindOptions();

            return helper.SearchControl(userQuery, findOptions, context, searchControl);
        }

    }
}
