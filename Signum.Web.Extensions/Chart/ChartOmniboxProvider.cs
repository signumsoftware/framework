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
    public class ChartOmniboxProvider : OmniboxClient.OmniboxProvider<ChartOmniboxResult>
    {
        public override OmniboxResultGenerator<ChartOmniboxResult> CreateGenerator()
        {
            return new ChartOmniboxResultGenerator();
        }

        public override MvcHtmlString RenderHtml(ChartOmniboxResult result)
        {
            MvcHtmlString html = result.KeywordMatch.ToHtml();

            if (result.QueryNameMatch != null)
                html = html.Concat(" {0}".FormatHtml(result.QueryNameMatch.ToHtml()));

            html = html.Concat(Icon());
            
            if (result.QueryNameMatch != null)
                html = new HtmlTag("a")
                    .Attr("href", RouteHelper.New().Action("Index", "Chart", new { webQueryName = Navigator.ResolveWebQueryName(result.QueryName) }))
                    .InnerHtml(html);
                
            return html;
        }

        public override MvcHtmlString Icon()
        {
            return ColoredSpan(" ({0})".Formato(ChartMessage.Chart.NiceToString()), "violet");
        }
    }
}