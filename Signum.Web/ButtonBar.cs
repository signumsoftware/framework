using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Web.Mvc;
using System.Web;
using Signum.Web.Properties;

namespace Signum.Web
{
    public delegate List<WebMenuItem> GetButtonBarElementDelegate(HttpContextBase httpContext, object entity, string mainControlUrl);
    public delegate List<WebMenuItem> GetButtonBarForQueryNameDelegate(HttpContextBase httpContext, object queryName, Type entityType); 

    public static class ButtonBarHelper
    {
        public static event GetButtonBarForQueryNameDelegate GetButtonBarForQueryName;
        public static event GetButtonBarElementDelegate GetButtonBarElement;

        public static string GetButtonBarElements(this HtmlHelper helper, object entity, string mainControlUrl, string prefix)
        {
            List<WebMenuItem> elements = GetButtonBarElements(helper.ViewContext.HttpContext, entity, mainControlUrl, prefix);
            return ListMenuItemsToString(helper, elements, prefix);
        }

        public static List<WebMenuItem> GetButtonBarElements(HttpContextBase httpContext, object entity, string mainControlUrl, string prefix)
        {
            List<WebMenuItem> elements = new List<WebMenuItem>();
            if (GetButtonBarElement != null)
                elements.AddRange(GetButtonBarElement.GetInvocationList()
                    .Cast<GetButtonBarElementDelegate>()
                    .Select(d => d(httpContext, entity, mainControlUrl))
                    .NotNull().SelectMany(d => d).ToList());
            return elements;
        }

        public static string GetButtonBarElementsForQuery(this HtmlHelper helper, object queryName, Type entityType, string prefix)
        {
            List<WebMenuItem> elements = new List<WebMenuItem>();
            if (GetButtonBarElement != null)
                elements.AddRange(GetButtonBarForQueryName.GetInvocationList()
                    .Cast<GetButtonBarForQueryNameDelegate>()
                    .Select(d => d(helper.ViewContext.HttpContext, queryName, entityType))
                    .NotNull().SelectMany(d => d).ToList());

            return ListMenuItemsToString(helper, elements, prefix);
        }

        private static string ListMenuItemsToString(HtmlHelper helper, List<WebMenuItem> elements, string prefix)
        {
            StringBuilder sb = new StringBuilder();

            foreach (WebMenuItem mi in elements)
                sb.AppendLine(mi.ToString(helper, prefix));
            
            return sb.ToString();
        }
    }
}
