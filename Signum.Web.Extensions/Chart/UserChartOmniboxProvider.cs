using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Signum.Entities.Chart;
using Signum.Web.Omnibox;
using Signum.Entities.Omnibox;
using Signum.Engine.DynamicQuery;
using System.Web.Mvc;
using Signum.Utilities;

namespace Signum.Web.Chart
{
    public class UserChartOmniboxProvider : OmniboxClient.OmniboxProvider<UserChartOmniboxResult>
    {
        public override OmniboxResultGenerator<UserChartOmniboxResult> CreateGenerator()
        {
            return new UserChartOmniboxResultGenerator();
        }

        public override MvcHtmlString RenderHtml(UserChartOmniboxResult result)
        {
            MvcHtmlString html = result.ToStrMatch.ToHtml();

            html = html.Concat(Icon());

            html = new HtmlTag("a")
                .Attr("href", RouteHelper.New().Action<ChartController>(cc => cc.ViewUserChart(result.UserChart)))
                .InnerHtml(html);
                
            return html;
        }

        public override MvcHtmlString Icon()
        {
            return ColoredSpan(" ({0})".Formato(typeof(UserChartDN).NiceName()), "darkviolet");
        }
    }
}