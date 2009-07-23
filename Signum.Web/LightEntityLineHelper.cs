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
        public static void LightEntityLine(this HtmlHelper helper, Lazy lazy, bool admin)
        {
            if (lazy == null)
                return;
            
            if (Navigator.IsViewable(lazy.RuntimeType, admin))
                helper.Write(helper.Href("", lazy.ToStr, Navigator.ViewRoute(lazy.RuntimeType, lazy.Id), "Ver", "", null));
            else
                helper.Write(helper.Span("", lazy.ToStr, ""));
        }
    }
}
