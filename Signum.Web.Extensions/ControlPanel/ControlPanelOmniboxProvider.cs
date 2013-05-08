using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Signum.Entities.ControlPanel;
using Signum.Web.Omnibox;
using Signum.Entities.Omnibox;
using Signum.Engine.DynamicQuery;
using System.Web.Mvc;
using Signum.Utilities;

namespace Signum.Web.ControlPanel
{
    public class ControlPanelOmniboxProvider : OmniboxClient.OmniboxProvider<ControlPanelOmniboxResult>
    {
        public override OmniboxResultGenerator<ControlPanelOmniboxResult> CreateGenerator()
        {
            return new ControlPanelOmniboxResultGenerator();
        }

        public override MvcHtmlString RenderHtml(ControlPanelOmniboxResult result)
        {
            MvcHtmlString html = result.ToStrMatch.ToHtml();

            html = html.Concat(Icon());

            html = new HtmlTag("a")
                .Attr("href", RouteHelper.New().Action<ControlPanelController>(cpc => cpc.View(result.ControlPanel)))
                .InnerHtml(html);
                
            return html;
        }

        public override MvcHtmlString Icon()
        {
            return ColoredSpan(" ({0})".Formato(typeof(ControlPanelDN).NiceName()), "darkslateblue");
        }
    }
}