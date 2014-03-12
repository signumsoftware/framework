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

        public override MvcHtmlString ToHtmlButton(HtmlHelper helper)
        {
            HtmlStringBuilder sb = new HtmlStringBuilder();
            using(sb.Surround(new HtmlTag("div").Class("btn-group")))
            {
                var a = new HtmlTag("a")
                    .Id(Id)
                    .Class("btn")
                    .Class(Style)
                    .Class(CssClass)
                    .Class("dropdown-toggle")
                    .Attr("data-toggle", "dropdown")
                    .Attr("alt", AltText)
                    .Attrs(HtmlProps);

                if (!Enabled)
                    a.Attr("disabled", "disabled");  

                using (sb.Surround(a))
                {
                    sb.Add(new MvcHtmlString(Text));
                    sb.Add(new HtmlTag("span").Class("caret"));
                }


                using (sb.Surround(new HtmlTag("ul").Class("dropdown-menu")))
                {
                    if (Items != null)
                        foreach (var tbb in Items)
                            sb.Add(tbb.ToHtmlMenuItem(helper));
                }
            }

            return sb.ToHtml();
        }
    }

    public class ToolBarSeparator : ToolBarButton
    {
        public override MvcHtmlString ToHtmlButton(HtmlHelper helper)
        {
            return MvcHtmlString.Empty;
        }

        public virtual MvcHtmlString ToHtmlMenuItem(HtmlHelper helper)
        {
            return new HtmlTag("li").Class("divider").ToHtml();
        }
    }
}
