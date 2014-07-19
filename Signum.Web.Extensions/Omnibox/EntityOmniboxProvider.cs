using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Signum.Entities.Omnibox;
using Signum.Engine.Maps;
using System.Web.Mvc;
using Signum.Utilities;
using Signum.Entities;
using Signum.Entities.Authorization;

namespace Signum.Web.Omnibox
{
    public class EntityOmniboxProvider : OmniboxClient.OmniboxProvider<EntityOmniboxResult>
    {
        public override OmniboxResultGenerator<EntityOmniboxResult> CreateGenerator()
        {
            return new EntityOmniboxResultGenenerator();
        }

        public override MvcHtmlString RenderHtml(EntityOmniboxResult result)
        {
            MvcHtmlString html = "{0} ".FormatHtml(result.TypeMatch.ToHtml());

            if (result.Id == null && result.ToStr == null)
            {
                throw new Exception("Invalid EntityOmniboxProvider result");
            }
            else
            {
                if (result.Id != null)
                {
                    html = html.Concat("{0}: {1}".FormatHtml(result.Id.ToString(), (result.Lite == null) ? 
                        ColoredSpan(OmniboxMessage.NotFound.NiceToString(), "gray") :
                        new HtmlTag("span").InnerHtml(new MvcHtmlString(result.Lite.TryToString()))));
                }
                else
                {
                    if (result.Lite == null)
                    {
                        html = html.Concat("'{0}': {1}".FormatHtml(result.ToStr, 
                            ColoredSpan(OmniboxMessage.NotFound.NiceToString(), "gray")));
                    }
                    else
                    {
                        html = html.Concat("{0}: {1}".FormatHtml(result.Lite.Id.ToString(),
                            result.ToStrMatch.ToHtml()));
                    }
                }
            }

            html = Icon().Concat(html);

            return html;
        }

        public override string GetUrl(EntityOmniboxResult result)
        {
            if (result.Lite == null)
                return null;

            return Navigator.NavigateRoute(result.Lite);
        }

        public override MvcHtmlString Icon()
        {
            return ColoredGlyphicon("glyphicon-circle-arrow-right", "#BCDEFF");
        }
    }
}