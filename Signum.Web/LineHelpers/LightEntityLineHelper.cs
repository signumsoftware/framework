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
        public static MvcHtmlString LightEntityLine(this HtmlHelper helper, Lite<IEntity> lite, bool isSearch, bool targetBlank = true, string innerText = null)
        {
            if (lite == null)
                return MvcHtmlString.Empty;

            if (lite.ToString() == null)
                Database.FillToString(lite);

            if (!Navigator.IsNavigable(lite.EntityType, null, isSearch))
                return (innerText ?? lite.ToString()).EncodeHtml();

            Dictionary<string, object> htmlAtributes = new Dictionary<string, object>();
            if (targetBlank)
                htmlAtributes.Add("target", "_blank");

            if (!Navigator.EntitySettings(lite.EntityType).AvoidPopup)
                htmlAtributes.Add("data-entity-link", lite.Key());

            return helper.Href("",
                    innerText ?? lite.ToString(),
                    Navigator.NavigateRoute(lite),
                    HttpUtility.HtmlEncode(lite.ToString()),
                    "sf-light-entity-line", htmlAtributes);
        }
    }
}
