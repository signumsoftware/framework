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
            Entity entity = (Entity)ctx.Entity;

            var iso = entity.Isolation();

            if (iso == null)
            {
                if (IsolationLogic.GetStrategy(entity.GetType()) == IsolationStrategy.Isolated)
                    throw new InvalidOperationException("Isolation not set");
            }

            return new IsolationWidget
            {
                Isolation = iso,
                Prefix = ctx.Prefix
            };
        }

        class IsolationWidget : IWidget
        {
            public string Prefix;
            public Lite<IsolationEntity> Isolation;

            MvcHtmlString IWidget.ToHtml(HtmlHelper helper)
            {
                if (Isolation == null)
                    return MvcHtmlString.Empty;

                HtmlStringBuilder sb = new HtmlStringBuilder();
                sb.Add(helper.HiddenLite(TypeContextUtilities.Compose(Prefix, "Isolation"), Isolation));
                sb.Add(new HtmlTag("span").Class("sf-quicklinks badge").SetInnerText(Isolation.ToString()));
                //sb.Add(new MvcHtmlString("<script>" + IsolationClient.Module["addIsolationPrefilter"](Isolation.Key()) + "</script>"));
                return sb.ToHtml();
            }
        }
    }
}
