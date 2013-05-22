using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Web.Mvc;
using System.Web;

namespace Signum.Web
{
    public class ToolBarMenu : ToolBarButton
    {
        public List<ToolBarButton> Items { get; set; }

        public override MvcHtmlString ToHtml(HtmlHelper helper)
        {
            HtmlStringBuilder sb = new HtmlStringBuilder();

            using (sb.Surround(new HtmlTag("ul").Class("sf-menu-button")))
                if (Items != null)
                {
                    foreach (ToolBarButton tbb in Items)
                        sb.Add(tbb.ToHtml(helper).Surround("li"));
                }

            HtmlProps["onclick"] = "SF.Dropdowns.toggle(event, this);";
            HtmlProps["data-icon-secondary"] = "ui-icon-triangle-1-s";

            var title = new HtmlTag("div").InnerHtml(Text.EncodeHtml()).Class(DivCssClass)
                .Attr("onclick", "SF.Dropdowns.toggle(event, this);")
                .Attr("data-icon-secondary", "ui-icon-triangle-1-s").ToHtml();

            return helper.Div(Id, title.Concat(sb.ToHtml()), "sf-dropdown");
        }
    }

    public class ToolBarSeparator : ToolBarButton
    {
        public override MvcHtmlString ToHtml(HtmlHelper helper)
        {
            return helper.Div("", null, DivCssClass != "not-set" ? DivCssClass : "sf-toolbar-menu-separator");
        }
    }
}
