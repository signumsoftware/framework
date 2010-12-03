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
            if (ImgSrc.HasText())
            {
                if (HtmlProps.ContainsKey("style"))
                    HtmlProps["style"] = "background:transparent url(" + ImgSrc + ")  no-repeat scroll -4px top; text-indent:12px; " + HtmlProps["style"].ToString();
                else
                    HtmlProps["style"] = "background:transparent url(" + ImgSrc + ")  no-repeat scroll -4px top; text-indent:12px;";
            }

            HtmlStringBuilder sb = new HtmlStringBuilder();

            using (sb.Surround(new HtmlTag("ul").Class("menu-operation")))
                if (Items != null)
                {
                    foreach (ToolBarButton tbb in Items)
                        sb.Add(tbb.ToHtml(helper).Surround("li"));
                }



            HtmlProps["onclick"] = "ToggleDropdown(this); return false;";
            return helper.Div(Id,
                Text.EncodeHtml().Concat(helper.Div(null, null, "indicator", null)).Concat(sb.ToHtml())
                , DivCssClass + " dropdown", HtmlProps);
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
