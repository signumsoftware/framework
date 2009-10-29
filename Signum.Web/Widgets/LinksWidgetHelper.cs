using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Signum.Utilities;

namespace Signum.Web
{
    public delegate List<QuickLinkItem> GetLinksDelegate(HtmlHelper helper, object entity, string partialViewName, string prefix);

    public static class LinksWidgetHelper
    {
        public static event GetLinksDelegate GetLinks;

        public static void Start()
        {
            WidgetsHelper.GetWidgetsForView += (helper, entity, partialViewName, prefix) => WidgetsHelper_GetWidgetsForView(helper, entity, partialViewName, prefix);
        }

        private static string WidgetsHelper_GetWidgetsForView(HtmlHelper helper, object entity, string partialViewName, string prefix)
        {
            List<QuickLinkItem> links = new List<QuickLinkItem>();
            if (GetLinks != null)
                links.AddRange(GetLinks.GetInvocationList()
                    .Cast<GetLinksDelegate>()
                    .Select(d => d(helper, entity, partialViewName, prefix))
                    .NotNull()
                    .SelectMany(d => d).ToList());

            return QuickLinksToString(helper, links, prefix);
        }

        private static string QuickLinksToString(HtmlHelper helper, List<QuickLinkItem> links, string prefix)
        {
            if (links == null || links.Count == 0)
                return "";

            StringBuilder sb = new StringBuilder();

            sb.AppendLine("<div class='widgetDiv quickLinksDiv'>");
            foreach (QuickLinkItem link in links)
            {
                sb.AppendLine(link.ToString(helper, prefix));
            }
            sb.AppendLine("</div>");

            return sb.ToString();
        }
    }
}
