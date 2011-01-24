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
    public class ToolBarMenu : ToolBarButton
    {
        public List<ToolBarButton> Items { get; set; }

        public override MvcHtmlString ToHtml(HtmlHelper helper)
        {
            HtmlStringBuilder sb = new HtmlStringBuilder();

            using (sb.Surround(new HtmlTag("ul").Class("menu-button")))
                if (Items != null)
                {
                    foreach (ToolBarButton tbb in Items)
                        sb.Add(tbb.ToHtml(helper).Surround("li"));
                }

            HtmlProps["onclick"] = "SF.Dropdowns.toggle(event, this);";
            HtmlProps["data-icon-secondary"] = "ui-icon-triangle-1-s";

            return helper.Div(Id,
                Text.EncodeHtml().Concat(sb.ToHtml()), DivCssClass + " dropdown", HtmlProps);
        }
    }

    public class ToolBarSeparator : ToolBarButton
    {
        public static string DefaultMenuSeparatorCssClass = "toolbar-menu-separator";

        public override MvcHtmlString ToHtml(HtmlHelper helper)
        {
            return helper.Div("", null, DivCssClass.HasText() ? DivCssClass : DefaultMenuSeparatorCssClass);
        }
    }
}
