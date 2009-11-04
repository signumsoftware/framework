using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web;
using Signum.Utilities;

namespace Signum.Web
{
    public delegate WidgetNode GetWidgetDelegate(HtmlHelper helper, object entity, string partialViewName, string prefix);

    public class WidgetNode
    {
        public WidgetNode() {
            Show = true;
        }

        /// <summary>
        /// Indicates wheter the widget will be shown
        /// </summary>
        public bool Show { get; set; }

        /// <summary>
        /// Text that will be shown as a header
        /// </summary>
        public string Label { get; set; }


        public string Count { get; set; }

        /// <summary>
        /// The different widgets
        /// </summary>
        public string Content { get; set; }

        public string Id { get; set; }

        public string Href { get; set; }
    }

    public static class WidgetsHelper
    {
        public static event GetWidgetDelegate GetWidgetsForView;

        public static List<WidgetNode> GetWidgetsListForViewName(this HtmlHelper helper, object entity, string partialViewName, string prefix)
        {
            List<WidgetNode> widgets = new List<WidgetNode>();
            if (entity != null)
            {
                if (GetWidgetsForView != null)
                    widgets.AddRange(GetWidgetsForView.GetInvocationList()
                        .Cast<GetWidgetDelegate>()
                        .Select(d => d(helper, entity, partialViewName, prefix))
                        .NotNull().ToList());
            }
            return widgets;
        }

        private static string WidgetsToString(HtmlHelper helper, List<string> widgets)
        {
            if (widgets == null || widgets.Count == 0)
                return "";

            StringBuilder sb = new StringBuilder();

            foreach (string widget in widgets)
            {
                if (widget != "")
                    sb.AppendLine(widget);
            }

            return sb.ToString();
        }
    }
}
