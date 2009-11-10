using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Signum.Utilities;

namespace Signum.Web
{
    public delegate List<QuickLinkItem> GetQuickLinksDelegate(HtmlHelper helper, object entity, string partialViewName, string prefix);

    public static class QuickLinkWidgetHelper
    {
        public static event GetQuickLinksDelegate GetQuickLinks;

        public static void Start()
        {
            WidgetsHelper.GetWidgetsForView += (helper, entity, partialViewName, prefix) => WidgetsHelper_GetWidgetsForView(helper, entity, partialViewName, prefix);
        }

        private static WidgetNode WidgetsHelper_GetWidgetsForView(HtmlHelper helper, object entity, string partialViewName, string prefix)
        {
            List<QuickLinkItem> links = new List<QuickLinkItem>();
            if (GetQuickLinks != null)
                links.AddRange(GetQuickLinks.GetInvocationList()
                    .Cast<GetQuickLinksDelegate>()
                    .Select(d => d(helper, entity, partialViewName, prefix))
                    .NotNull()
                    .SelectMany(d => d).ToList());

            return new WidgetNode
            {
                Content = QuickLinksToString(helper, links, prefix),
                Count = links.Count.ToString(),
                Label = "Quicklinks",
                Id = "Quicklinks",
                Show = links.Count > 0
            };
        }

        private static string QuickLinksToString(HtmlHelper helper, List<QuickLinkItem> links, string prefix)
        {
            if (links == null || links.Count == 0)
                return "";

            StringBuilder sb = new StringBuilder();

            sb.AppendLine("<div class='widget quickLinks'>");
            foreach (QuickLinkItem link in links)
            {
                sb.AppendLine(link.ToString(helper, prefix));
            }
            sb.AppendLine("</div>");

            return sb.ToString();
        }
    }
}
