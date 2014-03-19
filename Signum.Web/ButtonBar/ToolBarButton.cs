using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Web.Mvc;
using System.Web;

namespace Signum.Web
{
    public enum BootstrapStyle 
    {
        Default,
        Primary,
        Success,
        Info,
        Warning,
        Danger,
    }

    public class ToolBarButton
    {
        public string Id { get; set; }
        public string Text { get; set; }
        public string AltText { get; set; }
        public JsFunction OnClick { get; set; }
        public string Href { get; set; }
        public double Order { get; set; }
        public BootstrapStyle Style { get; set; }
        public string CssClass { get; set; }
        public bool Enabled { get; set; }
        public Dictionary<string, object> HtmlProps { get; private set; }

        public ToolBarButton()
        {
            Enabled = true;
            HtmlProps = new Dictionary<string, object>(0);
        }

        public virtual MvcHtmlString ToHtml(HtmlHelper helper)
        {
            var a = new HtmlTag("a")
                .Id(Id)
                .Class("btn")
                .Class("btn-" + Style.ToString().ToLower())
                .Class(CssClass)
                .Attrs(HtmlProps)
                .SetInnerText(Text);

            if (Href.HasText())
                a.Attr("href", Href);

            if (AltText.HasText())
                a.Attr("alt", AltText);

            if (Enabled && (OnClick != null || Href.HasText()))
                a.Attr("onclick", OnClick.ToString());
            else
                a.Attr("disabled", "disabled");

            return new HtmlTag("div").Class("btn-group").InnerHtml(a);
        }

        public IMenuItem ToMenuItem()
        {
            var result = new MenuItem
            {
                Id = Id,
                Text = Text,
                AltText = AltText,
                OnClick = OnClick,
                Href = Href,
                Order = Order,
                Style = Style,
                CssClass = CssClass,
                Enabled = Enabled,
            };

            result.HtmlProps.AddRange(HtmlProps);

            return result;
        }
    }

    public interface IMenuItem 
    {
        MvcHtmlString ToHtml(HtmlHelper helper);
    }

    public class MenuItem : IMenuItem
    {
        public string Id { get; set; }
        public string Text { get; set; }
        public string AltText { get; set; }
        public JsFunction OnClick { get; set; }
        public string Href { get; set; }
        public double Order { get; set; }
        public BootstrapStyle Style { get; set; }
        public string CssClass { get; set; }
        public bool Enabled { get; set; }
        public Dictionary<string, object> HtmlProps { get; private set; }

        public MenuItem()
        {
            Enabled = true;
            HtmlProps = new Dictionary<string, object>(0);
        }

        public MvcHtmlString ToHtml(HtmlHelper helper)
        {
            var a = new HtmlTag("a")
               .Id(Id)
               .Class("bg-" + Style.ToString().ToLower())
               .Class(CssClass)
               .Attrs(HtmlProps)
               .SetInnerText(Text);

            if (Href.HasText())
                a.Attr("href", Href);
            if (AltText.HasText())
                a.Attr("alt", AltText);

            if (Enabled && (OnClick != null || Href.HasText()))
                a.Attr("onclick", OnClick.ToString());
            else
                a.Attr("disabled", "disabled");

            return new HtmlTag("li").InnerHtml(a);
        }
    }
}
