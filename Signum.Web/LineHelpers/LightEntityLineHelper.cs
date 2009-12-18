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

namespace Signum.Web
{
    public static class LightEntityLineHelper
    {
        public static void LightEntityLine(this HtmlHelper helper, Lite lite, bool admin)
        {
            if (lite == null)
                return;
            
            if (Navigator.IsNavigable(lite.RuntimeType, admin))
                helper.Write(helper.Href("", lite.ToStr, Navigator.ViewRoute(lite.RuntimeType, lite.Id), "Ver", "", null));
            else
                helper.Write(helper.Span("", lite.ToStr, ""));
        }
    }
}
