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
    public delegate WidgetItem GetWidgetDelegate(ModifiableEntity entity, string partialViewName, string prefix);

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
        public MvcHtmlString Label { get; set; }

        /// <summary>
        /// The different widgets
        /// </summary>
        public MvcHtmlString Content { get; set; }

        public static string CssClassHighlighted = "ui-state-highlight ui-corner-all";
    }

    public static class WidgetsHelper
    {
        public static event GetWidgetDelegate GetWidgetsForView;

        public static List<WidgetItem> GetWidgetsForEntity(ModifiableEntity entity, string partialViewName, string prefix)
        {
            List<WidgetItem> widgets = new List<WidgetItem>();
            if (entity != null)
            {
                if (GetWidgetsForView != null)
                    widgets.AddRange(GetWidgetsForView.GetInvocationList()
                        .Cast<GetWidgetDelegate>()
                        .Select(d => d(entity, partialViewName, prefix))
                        .NotNull().ToList());
            }
            return widgets;
        }

        public static MvcHtmlString RenderWidgetsForEntity(this HtmlHelper helper, ModifiableEntity entity, string partialViewName, string prefix)
        {
            List<WidgetItem> widgets = GetWidgetsForEntity(entity, partialViewName, prefix);

            if (widgets == null)
                return MvcHtmlString.Empty;

            HtmlStringBuilder sb = new HtmlStringBuilder();
            using (sb.Surround(new HtmlTag("ul").Class("sf-widgets-container")))
            {
                foreach (WidgetItem widget in widgets)
                {
                    using (sb.Surround(new HtmlTag("li").IdName(widget.Id).Class("sf-dropdown sf-widget")))
                    {
                        sb.AddLine(widget.Label);
                        sb.AddLine(widget.Content);
                    }
                }
            }
            return sb.ToHtml();
        }
    }
}
