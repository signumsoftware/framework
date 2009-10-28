using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace Signum.Web
{
    public delegate List<QuickLink> GetLinksDelegate(HtmlHelper helper, object entity, string partialViewName);

    public static class LinksWidgetHelper
    {
        public static event GetLinksDelegate GetLinks; 

        public static void Start()
        {
            WidgetsHelper.GetWidgetsForView += (helper, entity, partialViewName) => WidgetsHelper_GetWidgetsForView(helper, entity, partialViewName);
        }

        private static List<IWidget> WidgetsHelper_GetWidgetsForView(HtmlHelper helper, object entity, string partialViewName)
        {
            return null;
            //List<QuickLink> links = new List<QuickLink>();
            //if (GetLinks != null)
            //    links.AddRange(GetLinks.GetInvocationList()
            //        .Cast<GetButtonBarElementDelegate>()
            //        .Select(d => d(helper, entity, mainControlUrl))
            //        .NotNull().SelectMany(d => d).ToList());

            //return ListMenuItemsToString(helper, links, prefix);  
        }
    }

    public class QuickLink
    {
        public QuickLink(string label)
        {
            this.Label = label;
        }

        /// <summary>
        /// Display name of the item
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// Action to be executed on the mouseDoubleClick of the item
        /// </summary>
        public Action Action { get; set; }
    }
}
