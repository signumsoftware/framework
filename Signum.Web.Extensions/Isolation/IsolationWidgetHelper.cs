using System;
using System.Collections.Generic;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Utilities;
using System.Web.Mvc;
using Signum.Web.Controllers;
using Signum.Entities.DynamicQuery;
using Signum.Engine.Isolation;
using Signum.Entities.Isolation;
using System.Web.Mvc.Html;

namespace Signum.Web.Isolation
{
    public static class IsolationWidgetHelper
    {
        public static IWidget CreateWidget(WidgetContext ctx)
        {
            IdentifiableEntity entity = (IdentifiableEntity)ctx.Entity;

            var iso = entity.Isolation();

            if (iso == null)
                throw new InvalidOperationException("Isolation not set");

            return new IsolationWidget
            {
                Isolation = iso,
                Prefix = ctx.Prefix
            };
        }

        class IsolationWidget : IWidget
        {
            public string Prefix;
            public Lite<IsolationDN> Isolation;

            MvcHtmlString IWidget.ToHtml(HtmlHelper helper)
            {
                HtmlStringBuilder sb = new HtmlStringBuilder();
                sb.Add(helper.HiddenLite(TypeContextUtilities.Compose(Prefix, "Isolation"), Isolation));
                sb.Add(new MvcHtmlString("<script>" + IsolationClient.Module["addIsolationPrefilter"](Isolation.Key()) + "</script>"));
                sb.Add(new HtmlTag("span").SetInnerText(Isolation.ToString()));
                return sb.ToHtml();
            }
        }
    }
}
