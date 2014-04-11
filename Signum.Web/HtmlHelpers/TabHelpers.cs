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
            this.tabs.Add(new Tab(id, title, body(null)));
        }


        public void Tab(string id, MvcHtmlString title, MvcHtmlString body)
        {
            this.tabs.Add(new Tab(id, title, body));
        }

        public void Tab(string id, MvcHtmlString title, Func<object, HelperResult> body)
        {
            this.tabs.Add(new Tab(id, title, body(null)));
        }


        public void Tab(string id, Func<object, HelperResult> title, MvcHtmlString body)
        {
            this.tabs.Add(new Tab(id, title(null), body));
        }

        public void Tab(string id, Func<object, HelperResult> title, Func<object, HelperResult> body)
        {
            this.tabs.Add(new Tab(id, title(null), body(null)));
        }


        public void Dispose()
        {
            var newTabs = context.ViewOverrides == null ? tabs : 
                context.ViewOverrides.ExpandTabs(tabs, containerId, helper, context);

            if (newTabs.IsEmpty())
                return;

            TextWriter writer = helper.ViewContext.Writer;

            var first = newTabs.First();

            using (Surround(writer, new HtmlTag("ul", context.Compose(containerId)).Class("nav nav-tabs")))
                foreach (var t in newTabs)
                    using (Surround(writer, new HtmlTag("li").Class(t == first ? "active" : null)))
                    using (Surround(writer, new HtmlTag("a").Attr("href", "#" + context.Compose(t.Id)).Attr("data-toggle", "tab")))
                        t.Title.WriteTo(writer);

            using (Surround(writer, new HtmlTag("div").Class("tab-content")))
                foreach (var t in newTabs)
                    using (Surround(writer, new HtmlTag("div", context.Compose(t.Id)).Class("tab-pane fade").Class(t == first ?  "in active" : null)))
                        t.Body.WriteTo(writer);
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

        public Tab(string id, string title, MvcHtmlString body)
            :this(id, MvcHtmlString.Create(HttpUtility.HtmlEncode(title)), new HelperResult(writer => writer.Write(body)))
        {
        }

        public Tab(string id, string title, HelperResult body)
            : this(id, MvcHtmlString.Create(HttpUtility.HtmlEncode(title)), body)
        {
        }


        public Tab(string id, MvcHtmlString title, MvcHtmlString body)
            : this(id, new HelperResult(writer => writer.Write(title)), new HelperResult(writer => writer.Write(body)))
        {
        }

        public Tab(string id, MvcHtmlString title, HelperResult body)
            : this(id, new HelperResult(writer => writer.Write(title)), body)
        {
        }

        public Tab(string id, HelperResult title, MvcHtmlString body)
            : this(id, title, new HelperResult(writer => writer.Write(body)))
        {
        }

        public Tab(string id, HelperResult title, HelperResult body)
        {
            this.Id = id;
            this.Title = title;
            this.Body = body;
        }
    }
}