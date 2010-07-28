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

            sb.Append("<div class='toolbar-menu'>");

            if (ImgSrc.HasText())
            {
                if (HtmlProps.ContainsKey("style"))
                    HtmlProps["style"] = "background:transparent url(" + ImgSrc + ")  no-repeat scroll left 11px; text-indent:10px; " + HtmlProps["style"].ToString();
                else
                    HtmlProps["style"] = "background:transparent url(" + ImgSrc + ")  no-repeat scroll left 11px; text-indent:10px;";

                sb.Append(helper.Href(Id, Text, "#", AltText ?? "", DivCssClass, HtmlProps));
            }
            else
            {
                sb.Append(helper.Href(Id, Text, "#", AltText ?? "", DivCssClass, HtmlProps));
            }

            sb.Append("<ul class='menu-operation-ul'>");
            
            foreach (ToolBarButton tbb in Items)
                sb.Append("<li>" + tbb.ToString(helper) + "</li>");  

            sb.Append("</u>");

            sb.Append("</div>");

            return sb.ToString();
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
