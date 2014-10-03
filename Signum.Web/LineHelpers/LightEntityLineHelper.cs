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
using System.Web;
using Signum.Engine;

namespace Signum.Web
{
    public static class LightEntityLineHelper
    {
        public static MvcHtmlString LightEntityLine(this HtmlHelper helper, Lite<IEntity> lite, bool isSearch)
        {
            if (lite == null)
                return MvcHtmlString.Empty;

            if (lite.ToString() == null)
                Database.FillToString(lite);

            MvcHtmlString result = Navigator.IsNavigable(lite.EntityType, null, isSearch) ?
                helper.Href("",
                    lite.ToString(),
                    Navigator.NavigateRoute(lite),
                    HttpUtility.HtmlEncode(EntityControlMessage.View.NiceToString()),
                    "", null) :
                lite.ToString().EncodeHtml();

            return result;
        }
    }
}
