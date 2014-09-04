using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.WebPages;
using Signum.Utilities;

namespace Signum.Web
{
    public class TabContainer : IDisposable
    {
        List<Tab> tabs = new List<Tab>();

        TypeContext context;
        HtmlHelper helper;
        string containerId;

        public TabContainer(HtmlHelper helper, TypeContext context, string containerId)
        {
            this.helper = helper;
            this.context = context;
            this.containerId = containerId; 
        }

        public void Tab(Tab tab)
        {
            this.tabs.Add(tab);
        }


        public void Tab(string id, string title, MvcHtmlString body)
        {
            this.tabs.Add(new Tab(id, title, body));
        }

        public void Tab(string id, string title, Func<object, HelperResult> body)
        {
            this.tabs.Add(new Tab(id, title, body));
        }


        public void Tab(string id, MvcHtmlString title, MvcHtmlString body)
        {
            this.tabs.Add(new Tab(id, title, body));
        }

        public void Tab(string id, MvcHtmlString title, Func<object, HelperResult> body)
        {
            this.tabs.Add(new Tab(id, title, body));
        }


        public void Tab(string id, Func<object, HelperResult> title, MvcHtmlString body)
        {
            this.tabs.Add(new Tab(id, title, body));
        }

        public void Tab(string id, Func<object, HelperResult> title, Func<object, HelperResult> body)
        {
            this.tabs.Add(new Tab(id, title, body));
        }


        public void Dispose()
        {
            var newTabs = context.ViewOverrides == null ? tabs : 
                context.ViewOverrides.ExpandTabs(tabs, containerId, helper, context);

            if (newTabs.IsEmpty())
                return;

            TextWriter writer = helper.ViewContext.Writer;

            var first = newTabs.FirstOrDefault(a => a.Active) ?? newTabs.FirstOrDefault();

            using (Surround(writer, new HtmlTag("ul", context.Compose(containerId)).Class("nav nav-tabs")))
                foreach (var t in newTabs)
                    t.WriteHeader(writer, first, context);

            using (Surround(writer, new HtmlTag("div").Class("tab-content")))
                foreach (var t in newTabs)
                    t.WriteBody(writer, first, context);
        }

       

        public static IDisposable Surround(TextWriter writer, HtmlTag div)
        {
            writer.WriteLine(div.ToHtml(TagRenderMode.StartTag).ToString());

            return new Disposable(() => writer.WriteLine(div.ToHtml(TagRenderMode.EndTag)));
        }
  
    }

    public class Tab
    {
        public readonly string Id;
        public HelperResult Title;
        public HelperResult Body;
        public bool Active;
        public string ToolTip;

        public Tab(string id, string title, MvcHtmlString body)
            : this(id, new HelperResult(writer => writer.Write(HttpUtility.HtmlEncode(title))), new HelperResult(writer => writer.Write(body)))
        {
        }

        public Tab(string id, string title, Func<object, HelperResult> body)
            : this(id, new HelperResult(writer => writer.Write(HttpUtility.HtmlEncode(title))), body(null))
        {
        }


        public Tab(string id, MvcHtmlString title, MvcHtmlString body)
            : this(id, new HelperResult(writer => writer.Write(title)), new HelperResult(writer => writer.Write(body)))
        {
        }

        public Tab(string id, MvcHtmlString title, Func<object, HelperResult> body)
            : this(id, new HelperResult(writer => writer.Write(title)), body(null))
        {
        }

        public Tab(string id, Func<object, HelperResult> title, MvcHtmlString body)
            : this(id, title(null), new HelperResult(writer => writer.Write(body)))
        {
        }

        public Tab(string id, Func<object, HelperResult> title, Func<object, HelperResult> body)
            : this(id, title(null), body(null))
        {
        }

        public Tab(string id, HelperResult title, HelperResult body)
        {
            this.Id = id;
            this.Title = title;
            this.Body = body;
        }

        public virtual void WriteHeader(TextWriter writer, Tab first, TypeContext context)
        {
            using (TabContainer.Surround(writer, new HtmlTag("li").Class(this == first ? "active" : null)))
            using (TabContainer.Surround(writer, new HtmlTag("a").Attr("href", "#" + context.Compose(this.Id)).Attr("data-toggle", "tab").Attr("title", this.ToolTip)))
                this.Title.WriteTo(writer);
        }

        public virtual void WriteBody(TextWriter writer, Tab first, TypeContext context)
        {
            using (TabContainer.Surround(writer, new HtmlTag("div", context.Compose(this.Id)).Class("tab-pane fade").Class(this == first ? "in active" : null)))
                this.Body.WriteTo(writer);
        }
    }
}