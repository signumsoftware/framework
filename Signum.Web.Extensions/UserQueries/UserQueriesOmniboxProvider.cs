using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Signum.Entities.UserQueries;
using Signum.Web.Omnibox;
using Signum.Entities.Omnibox;
using Signum.Engine.DynamicQuery;
using System.Web.Mvc;
using Signum.Web.Extensions.Properties;
using Signum.Utilities;

namespace Signum.Web.UserQueries
{
    public class UserQueriesOmniboxProvider : OmniboxClient.OmniboxProvider<UserQueryOmniboxResult>
    {
        public override OmniboxResultGenerator<UserQueryOmniboxResult> CreateGenerator()
        {
            return new UserQueryOmniboxResultGenerator();
        }

        public override MvcHtmlString RenderHtml(UserQueryOmniboxResult result)
        {
            MvcHtmlString html = result.ToStrMatch.ToHtml();

            html = html.Concat(Icon());

            html = new HtmlTag("a")
                .Attr("href", RouteHelper.New().Action<UserQueriesController>(uqc => uqc.View(result.UserQuery)))
                .InnerHtml(html);
                
            return html;
        }

        public override MvcHtmlString Icon()
        {
            return ColoredSpan(" ({0})".Formato(typeof(UserQueryDN).NiceName()), "dodgerblue");
        }
    }
}