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
    public interface IWidget
    {
        MvcHtmlString ToHtml(HtmlHelper helper);
    }

    public class Widget : IWidget
    {
        public Widget()
        {
            Visible = true;
        }

        public string Id { get; set; }
        public bool Visible { get; set; }
        public string Title { get; set; }
        public string IconClass { get; set; }
        public string Class { get; set; }
        public string Text { get; set; }
        public bool Active { get; set; }
        public MvcHtmlString Html { get; set; }
        public List<IMenuItem> Items { get; set; }

        public MvcHtmlString ToHtml(HtmlHelper helper)
        {
            HtmlStringBuilder sb = new HtmlStringBuilder();
            using (sb.SurroundLine("li"))
            {
                using (sb.SurroundLine(new HtmlTag("a", Id)
                    .Class("badge").Class(Class).Class(Active? "sf-widget-active" : null)
                    .Attr("title", Title)
                    .Attr("role", "button")
                    .Attr("href", "#")
                    .Attr("data-toggle", "dropdown")))
                {

                    if (IconClass.HasText())
                        sb.AddLine(new HtmlTag("span").Class(IconClass));

                    if (Text.HasText())
                        sb.AddLine(new HtmlTag("span").SetInnerText(Text));

                    if (Html != null)
                        sb.AddLine(Html);
                }

                using (sb.SurroundLine(new HtmlTag("ul")
                    .Class("dropdown-menu dropdown-menu-right")
                    .Attr("role", "menu")
                    .Attr("aria-labelledby", Id)))
                {
                    foreach (var mi in Items)
                    {
                        sb.AddLine(mi.ToHtml());
                    }
                }
            }

            return sb.ToHtml();
        }
    }

    public class WidgetContext
    {
        public TypeContext TypeContext;
        public ModifiableEntity Entity { get { return (ModifiableEntity)TypeContext.UntypedValue; } }
        public string Prefix { get { return TypeContext.Prefix; } }
        public string PartialViewName;
        public UrlHelper Url;
    }

    public interface IEmbeddedWidget
    {
        MvcHtmlString ToHtml(HtmlHelper helper);
        EmbeddedWidgetPostion Position { get;  }
    }

    public enum EmbeddedWidgetPostion 
    {
        Top,
        Bottom,
    }

    public static class WidgetsHelper
    {
        public static event Func<WidgetContext, IWidget> GetWidget;

        public static MvcHtmlString RenderWidgets(this HtmlHelper helper, WidgetContext ctx)
        {
            if (GetWidget == null)
                return MvcHtmlString.Empty;

            List<IWidget> widgets = GetWidget.GetInvocationListTyped()
                .Select(d => d(ctx))
                .NotNull()
                .ToList();

            if (widgets.IsNullOrEmpty())
                return MvcHtmlString.Empty;

            HtmlStringBuilder sb = new HtmlStringBuilder();
            using (sb.SurroundLine(new HtmlTag("ul").Class("sf-widgets")))
            {
                foreach (IWidget widget in widgets)
                {
                    sb.AddLine(widget.ToHtml(helper));
                }
            }
            return sb.ToHtml();
        }

        public static event Func<WidgetContext, IEmbeddedWidget> GetEmbeddedWidget;

        public static IDisposable RenderEmbeddedWidget(this HtmlHelper helper, WidgetContext ctx)
        {
            if (GetEmbeddedWidget == null)
                return null;

            List<IEmbeddedWidget> widgets = GetEmbeddedWidget.GetInvocationListTyped()
                .Select(d => d(ctx))
                .NotNull()
                .ToList();

            if (widgets.IsNullOrEmpty())
                return null;


            foreach (var item in widgets.Where(a=>a.Position == EmbeddedWidgetPostion.Top))
            {
                helper.ViewContext.Writer.Write(item.ToHtml(helper).ToString());
            }


            return new Disposable(() =>
            {
                foreach (var item in widgets.Where(a => a.Position == EmbeddedWidgetPostion.Bottom))
                {
                    helper.ViewContext.Writer.Write(item.ToHtml(helper).ToString());
                }
            }); 
        }


    }

}
