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

            html = html.Concat(Icon());

            if (result.Lite != null)
                html = new HtmlTag("a")
                    .Attr("href", Navigator.NavigateRoute(result.Lite))
                    .InnerHtml(html).ToHtml();

            return html;
        }

        public override MvcHtmlString Icon()
        {
            return ColoredSpan(" ({0})".Formato(AuthMessage.View.NiceToString()), "dodgerblue");
        }
    }
}