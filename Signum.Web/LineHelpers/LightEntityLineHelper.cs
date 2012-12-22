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
        public static MvcHtmlString LightEntityLine(this HtmlHelper helper, Lite<IIdentifiable> lite, bool isSearchEntity)
        {
            if (lite == null)
                return MvcHtmlString.Empty;

            if (string.IsNullOrEmpty(lite.ToString()))
                Database.FillToString(lite);
            MvcHtmlString result = Navigator.IsNavigable(lite.EntityType, isSearchEntity) ?
                helper.Href("",
                    lite.ToString(),
                    Navigator.NavigateRoute(lite),
                    HttpUtility.HtmlEncode(Resources.View),
                    "", null) :
                lite.ToString().EncodeHtml();

            return result;
        }
    }
}
