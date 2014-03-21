using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Linq.Expressions;
using Signum.Utilities;
using System.Web.Mvc.Html;
using Signum.Entities;
using Signum.Entities.Reflection;
using Signum.Entities.DynamicQuery;
using System.Web;

namespace Signum.Web
{
    public delegate MvcHtmlString WriteAHref(string href, string title, string text);

    public static class WebMenuHelper
    {
        public static MvcHtmlString WebMenu(this HtmlHelper helper, params  WebMenuItem[] menuItems)
        {
            return helper.WebMenu((IEnumerable<WebMenuItem>)menuItems);
        }
        public static MvcHtmlString WebMenu(this HtmlHelper helper, IEnumerable<WebMenuItem> menuITems)
        {
            var currentUrl = helper.ViewContext.RequestContext.HttpContext.Request.RawUrl; 
            HtmlStringBuilder sb = new HtmlStringBuilder();
            foreach (WebMenuItem menu in menuITems)
            {
                menu.Write(sb, currentUrl, 0);
            }
            return sb.ToHtml();
        }
    }

    public class WebMenuItem
    {
        List<WebMenuItem> children;
        public List<WebMenuItem> Children
        {
            get { return children ?? (children  = new List<WebMenuItem>()); }
            set { children = value; }
        }
      
        public WriteAHref ManualA { get; set; } //Specify all the tag string (href, title, text)=>"<a href='{0}' title={1}>{2}</a>".Formato(href,title,text); 

        public object Link { get; set; }

        public string Id { get; set; }

        string text;
        public string Text
        {
            get
            {
                if (text.HasText())
                    return text;

                FindOptions findOptions = Link as FindOptions;
                if (findOptions != null)
                    return QueryUtils.GetNiceName(findOptions.QueryName);

                throw new InvalidOperationException("Text not set for menu item {0}".Formato(Link.ToString()));
            }
            set { text = value; }
        }

        public MvcHtmlString Html {get; set; }

        string title;
        public string Title
        {
            get { return title ?? Text; }
            set { title = value; }
        }

        bool? visible = null;
        public bool Visible
        {
            get
            {
                if (visible.HasValue)
                    return visible.Value;

                if (children.HasItems())
                    return Children.Any(a => a.Visible);

                FindOptions findOptions = Link as FindOptions;
                if (findOptions != null)
                    return Navigator.IsFindable(findOptions.QueryName);

                return true;
            }

            set { visible = value; }
        }

        public string Class { get; set; }           //is applied to link

     
        public void Write(HtmlStringBuilder sb, string currentUrl, int depth)
        {
            if (!Visible)
                return;

            bool isActive = this.Link != null && this.Link.ToString() == currentUrl ||
                children.HasItems() && children.Any(a => a.Link != null && a.Link.ToString() == currentUrl);

            using (sb.Surround(new HtmlTag("li").Class(isActive ? "active" : null).Class(this.children.HasItems() ? "dropdown" : null)))
            {
                if (Link != null)
                {
                    string link = Link.ToString();

                    if (ManualA != null)
                        sb.AddLine(ManualA(link, title, " ".CombineIfNotEmpty(Class, "selected")));
                    else
                    {
                        HtmlTag tbA = new HtmlTag("a")
                                .Attrs(new { href = link, title = Title, id = Id })
                                .Class(Class);

                        if (Html != null)
                            tbA.InnerHtml(Html);
                        else
                            tbA.SetInnerText(Text);

                        sb.AddLine(tbA.ToHtml());
                    }
                }
                else if (this.children.HasItems())
                {
                    using (sb.Surround(new HtmlTag("a").Attr("href", "#")
                        .Class("dropdown-toggle")
                        .Attr("data-toggle", "dropdown")))
                    {
                        if (Html != null)
                            sb.Add(Html);
                        else 
                            sb.Add(new HtmlTag("span").SetInnerText(Text));

                        sb.Add(new HtmlTag("b").Class("caret"));
                    }
                }
                else if (Html != null)
                    sb.AddLine(Html);
                else
                    sb.AddLine(new HtmlTag("span").SetInnerText(Text));

                if (this.children.HasItems())
                {
                    using (sb.Surround(new HtmlTag("ul").Class("dropdown-menu")))
                    {
                        foreach (WebMenuItem menu in this.children)
                        {
                            menu.Write(sb, currentUrl, depth + 1);
                        }
                    }
                }
            }
        }
    }
}
