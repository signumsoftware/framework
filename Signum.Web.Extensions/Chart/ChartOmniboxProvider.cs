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
            else
                html = html.Concat(this.ColoredSpan(OmniboxMessage.Omnibox_Query.NiceToString() + "...", "lightgray"));

            html = Icon().Concat(html);
            
            return html;
        }

        public override string GetUrl(ChartOmniboxResult result)
        {
            if (result.QueryNameMatch != null)
                return RouteHelper.New().Action("Index", "Chart", new { webQueryName = Finder.ResolveWebQueryName(result.QueryName) });

            return null;
        }

        public override MvcHtmlString Icon()
        {
            return ColoredGlyphicon("glyphicon-stats", "violet");
        }
    }
}