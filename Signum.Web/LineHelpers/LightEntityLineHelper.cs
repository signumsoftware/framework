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
using Signum.Engine;

namespace Signum.Web
{
    public static class LightEntityLineHelper
    {
        public static MvcHtmlString LightEntityLine(this HtmlHelper helper, Lite lite, bool admin)
        {
            if (lite == null)
                return MvcHtmlString.Empty;

            if (string.IsNullOrEmpty(lite.ToString()))
                Database.FillToString(lite);
            MvcHtmlString result = Navigator.IsViewable(lite.RuntimeType, admin ? EntitySettingsContext.Admin : EntitySettingsContext.Default) ?
                helper.Href("",
                    lite.ToString(),
                    Navigator.ViewRoute(lite),
                    HttpUtility.HtmlEncode(Resources.View),
                    "", null) :
                lite.ToString().EncodeHtml();

            return result;
        }
    }
}
