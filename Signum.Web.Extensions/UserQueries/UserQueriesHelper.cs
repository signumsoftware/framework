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

        public static MvcHtmlString QueryTokenDNBuilder(this HtmlHelper helper, TypeContext<QueryTokenDN> ctx, QueryTokenBuilderSettings settings)
        {
            if (ctx.Value.Try(qt => qt.ParseException) != null)
            {
                HtmlStringBuilder sb = new HtmlStringBuilder();
                sb.Add(new HtmlTag("div").Class("ui-state-error").SetInnerText(ctx.Value.ParseException.Message).ToHtml());
                sb.Add(new HtmlTag("pre").SetInnerText(ctx.Value.TokenString).ToHtml());
                sb.Add(helper.QueryTokenBuilder(null, ctx, settings));
                return sb.ToHtml();
            }
            else
            {
                return helper.QueryTokenBuilder(ctx.Value.Try(ct => ct.Token), ctx, settings);
            }
        }

        public static string GetTokenString(MappingContext<QueryTokenDN> ctx)
        {
            return ctx.Inputs.Keys
                .OrderBy(k => int.Parse(k.After("ddlTokens_")))
                .Select(k => ctx.Inputs[k])
                .TakeWhile(k => k.HasText())
                .ToString(".");
        }
    }
}
