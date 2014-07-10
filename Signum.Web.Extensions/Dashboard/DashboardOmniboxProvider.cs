using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Signum.Entities.Dashboard;
using Signum.Web.Omnibox;
using Signum.Entities.Omnibox;
using Signum.Engine.DynamicQuery;
using System.Web.Mvc;
using Signum.Utilities;
using Signum.Engine.Dashboard;

namespace Signum.Web.Dashboard
{
    public class DashboardOmniboxProvider : OmniboxClient.OmniboxProvider<DashboardOmniboxResult>
    {
        public override OmniboxResultGenerator<DashboardOmniboxResult> CreateGenerator()
        {
            return new DashboardOmniboxResultGenerator(DashboardLogic.Autocomplete);
        }

        public override MvcHtmlString RenderHtml(DashboardOmniboxResult result)
        {
            MvcHtmlString html = result.ToStrMatch.ToHtml();

            html = Icon().Concat(html);


                
            return html;
        }

        public override string GetUrl(DashboardOmniboxResult result)
        {
            return RouteHelper.New().Action<DashboardController>(cpc => cpc.View(result.Dashboard, null));
        }

        public override MvcHtmlString Icon()
        {
            return ColoredGlyphicon("glyphicon-th-large", "darkslateblue");
        }
    }
}