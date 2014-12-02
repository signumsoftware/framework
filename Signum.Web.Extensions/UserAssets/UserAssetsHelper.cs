using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Signum.Entities.UserAssets;
using Signum.Utilities;

namespace Signum.Web.UserAssets
{
    public static class UserAssetsHelper
    {
        public static MvcHtmlString QueryTokenDNBuilder(this HtmlHelper helper, TypeContext<QueryTokenEntity> ctx, QueryTokenBuilderSettings settings)
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

        public static string GetTokenString(MappingContext<QueryTokenEntity> ctx)
        {
            return ctx.Inputs.Keys
                .OrderBy(k => int.Parse(k.After("ddlTokens_")))
                .Select(k => ctx.Inputs[k])
                .TakeWhile(k => k.HasText())
                .ToString(".");
        }
    }
}