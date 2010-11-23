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
        
        public override string ToString(HtmlHelper helper)
        {
            StringBuilder sb = new StringBuilder();

            if (ImgSrc.HasText())
            {
                if (HtmlProps.ContainsKey("style"))
                    HtmlProps["style"] = "background:transparent url(" + ImgSrc + ")  no-repeat scroll -4px top; text-indent:12px; " + HtmlProps["style"].ToString();
                else
                    HtmlProps["style"] = "background:transparent url(" + ImgSrc + ")  no-repeat scroll -4px top; text-indent:12px;";
            }

            sb.Append("<ul class='menu-operation'>");

            if (Items != null)
            {
                foreach (ToolBarButton tbb in Items)
                    sb.Append("<li>" + tbb.ToString(helper) + "</li>");
            }

            sb.Append("</ul>");

            HtmlProps["onclick"] = "ToggleDropdown(this); return false;";
            return helper.Div(Id,
                HttpUtility.HtmlEncode(Text)
                + helper.Div(null, null, "indicator", null)
                + sb.ToString(), DivCssClass + " dropdown", HtmlProps);
        }
    }

    public class ToolBarSeparator : ToolBarButton
    {
        public static string DefaultMenuSeparatorCssClass = "toolbar-menu-separator";

        public override string ToString(HtmlHelper helper)
        {
            return helper.Div("", "", DivCssClass.HasText() ? DivCssClass : DefaultMenuSeparatorCssClass);
        }
    }
}
