using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Web.Mvc;
using System.Web;

namespace Signum.Web
{
    public class ToolBarButton
    {
        public static readonly string DefaultStyle = "btn-default";
        public static readonly string PrimaryStyle = "btn-primary";
        public static readonly string SuccessStyle = "btn-success";
        public static readonly string InfoStyle = "btn-info";
        public static readonly string WarningStyle = "btn-warning";
        public static readonly string DangerStyle = "btn-danger";

        public string Id { get; set; }
        public string Text { get; set; }
        public string AltText { get; set; }
        public JsFunction OnClick { get; set; }
        public string Href { get; set; }
        public double Order { get; set; }


        private string style = DefaultStyle;
        public string Style
        {
            get { return style; }
            set { style = value; }
        }


        private string cssClass;
        public string CssClass
        {
            get { return cssClass; }
            set { cssClass = value; }
        }

        private bool enabled = true;
        public bool Enabled
        {
            get { return enabled; }
            set { enabled = value; }
        }

        Dictionary<string, object> htmlProps = new Dictionary<string, object>(0);
        public Dictionary<string, object> HtmlProps
        {
            get { return htmlProps; }
        }

        public virtual MvcHtmlString ToHtmlButton(HtmlHelper helper)
        {
            var a = new HtmlTag("a")
                .Id(Id)
                .Class("btn")
                .Class(Style)
                .Class(CssClass)
                .Attr("href", Href)
                .Attr("alt", AltText)
                .Attrs(HtmlProps)
                .SetInnerText(Text);

            if (enabled && (OnClick != null || Href.HasText()))
                a.Attr("onclick", OnClick.ToString());
            else
                a.Attr("disabled", "disabled");

            return new HtmlTag("div").Class("btn-group").InnerHtml(a);
        }

        public virtual MvcHtmlString ToHtmlMenuItem(HtmlHelper helper)
        {
            var a = new HtmlTag("a")
               .Id(Id)
               .Class("btn")
               .Class(Style)
               .Class(CssClass)
               .Attr("href", Href)
               .Attr("alt", AltText)
               .Attrs(HtmlProps)
               .SetInnerText(Text);

            if (enabled && (OnClick != null || Href.HasText()))
                a.Attr("onclick", OnClick.ToString());
            else
                a.Attr("disabled", "disabled");

            return new HtmlTag("li").InnerHtml(a);
        }
    }
}
