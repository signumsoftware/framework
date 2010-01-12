using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web;
using Signum.Utilities;
using Signum.Entities;

namespace Signum.Web
{
    public delegate WidgetItem GetWidgetDelegate(HtmlHelper helper, ModifiableEntity entity, string partialViewName, string prefix);

    public class WidgetItem
    {
        public WidgetItem()
        {
            Show = true;
        }

        public string Id { get; set; }

        /// <summary>
        /// Indicates wheter the widget will be shown
        /// </summary>
        public bool Show { get; set; }

        /// <summary>
        /// Text that will be shown as a header
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// The different widgets
        /// </summary>
        public string Content { get; set; }
    }

    public static class WidgetsHelper
    {
        public static event GetWidgetDelegate GetWidgetsForView;

        public static List<WidgetItem> GetWidgetsListForViewName(this HtmlHelper helper, ModifiableEntity entity, string partialViewName, string prefix)
        {
            List<WidgetItem> widgets = new List<WidgetItem>();
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
