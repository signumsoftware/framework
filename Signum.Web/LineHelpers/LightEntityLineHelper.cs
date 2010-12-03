using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Linq.Expressions;
using Signum.Utilities;
using System.Web.Mvc.Html;
using Signum.Entities;
using Signum.Entities.Reflection;
using Signum.Web.Properties;
using System.Web;

namespace Signum.Web
{
    public static class LightEntityLineHelper
    {
        public static MvcHtmlString LightEntityLine(this HtmlHelper helper, Lite lite, bool admin)
        {
            if (lite == null)
                return null;

            bool isNavigable = Navigator.IsNavigable(lite.RuntimeType, admin);
            MvcHtmlString link = helper.Href("",
                lite.ToStr,
                isNavigable ? Navigator.ViewRoute(lite.RuntimeType, lite.Id) : "",
                HttpUtility.HtmlEncode(Resources.View),
                "",
                isNavigable ? null : new Dictionary<string, object>() { { "style", "display:none" } });

            if (isNavigable)
                return link;
            else
                return link.Concat(lite.ToStr.EncodeHtml());
        }
    }
}
