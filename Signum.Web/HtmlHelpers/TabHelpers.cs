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
            this.tabs.Add(new Tab(id, title) { Body = new HelperResult(writer => writer.Write(body)) });
        }

        public void Tab(string id, string title, Func<object, HelperResult> body)
        {
            this.tabs.Add(new Tab(id, title) { Body = body(null) });
        }

        public void Tab(string id, MvcHtmlString title, MvcHtmlString body)
        {
            this.tabs.Add(new Tab(id, title) { Body = new HelperResult(writer => writer.Write(body)) });
        }

        public void Tab(string id, MvcHtmlString title, Func<object, HelperResult> body)
        {
            this.tabs.Add(new Tab(id, title) { Body = body(null) });
        }

        public void Tab(string id, Func<object, HelperResult> title, Func<object, HelperResult> body)
        {
            this.tabs.Add(new Tab(id) { Title = title(null), Body = body(null) });
        }

        public void Dispose()
        {
            var newTabs = context.ViewOverrides == null ? tabs : 
                context.ViewOverrides.ExpandTabs(tabs, containerId, helper, context);

            TextWriter writer = helper.ViewContext.Writer;

            using (Surround(writer, new HtmlTag("ul", context.Compose(containerId)).Class("nav nav-tabs")))
                foreach (var t in newTabs)
                    using (Surround(writer, new HtmlTag("li")))
                        using (Surround(writer, new HtmlTag("a").Attr("href", "#" + context.Compose(t.Id))))
                            t.Title.WriteTo(writer);

            using (Surround(writer, new HtmlTag("div").Class("tab-content")))
                foreach (var t in newTabs)
                    using (Surround(writer, new HtmlTag("div", context.Compose(t.Id))))
                        t.Body.WriteTo(writer);

            //using (Surround(writer, new HtmlTag("script")))
            //{
            //    writer.WriteLine("$(function () { $('#myTab a:last').tab('show')  })");
            //}
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

        public Tab(string id) 
        {
            this.Id = id;
        }

        public Tab(string id, MvcHtmlString title) : this(id) 
        {
            this.Title = new HelperResult(writer => writer.Write(title));
        }

        public Tab(string id, string title)
            : this(id, new MvcHtmlString(HttpUtility.HtmlEncode(title)))
        {
        }
    }
}