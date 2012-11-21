using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Signum.Entities.Reports;
using System.Web.Mvc;
using Signum.Entities.DynamicQuery;
using Signum.Entities.UserQueries;
using Signum.Utilities;

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

        public static MvcHtmlString CountSearchControl(this HtmlHelper helper, UserQueryDN userQuery, FindOptions findOptions, Action<CountSearchControl> settinsModifier)
        {
            if (findOptions == null)
                throw new ArgumentNullException("findOptions");

            findOptions.ApplyUserQuery(userQuery);

            return helper.CountSearchControl(findOptions, settinsModifier);
        }

        public static MvcHtmlString QueryTokenDNBuilder(this HtmlHelper helper, QueryTokenDN queryToken, Context context, QueryDescription desc)
        {
            return helper.QueryTokenDNBuilder(queryToken, context, desc.QueryName, qt => QueryUtils.SubTokens(qt, desc.Columns));
        }

        public static MvcHtmlString QueryTokenDNBuilder(this HtmlHelper helper, QueryTokenDN queryToken, Context context, object queryName, Func<QueryToken, List<QueryToken>> subTokens)
        {
            if (queryToken.TryCC(qt => qt.ParseException) != null)
            {
                HtmlStringBuilder sb = new HtmlStringBuilder();
                sb.Add(new HtmlTag("div").Class("ui-state-error").SetInnerText(queryToken.ParseException.Message).ToHtml());
                sb.Add(new HtmlTag("pre").SetInnerText(queryToken.TokenString).ToHtml());
                sb.Add(helper.QueryTokenBuilder(null, context, queryName, subTokens));
                return sb.ToHtml();
            }
            else
            {
                return helper.QueryTokenBuilder(queryToken.TryCC(ct => ct.Token), context, queryName, subTokens);
            }
        }
    }
}
