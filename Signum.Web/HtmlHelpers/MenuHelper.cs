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
using Signum.Web.Properties;
using Signum.Entities.DynamicQuery;

namespace Signum.Web
{
    public delegate string WriteAHref(string href, string title, string text);

    public class WebMenuItem 
    {
        List<WebMenuItem> children = new List<WebMenuItem>();
        public List<WebMenuItem> Children 
        {
            get { return children; }
            set { children = value; }
        }

        public WriteAHref ManualA { get; set; } //Specify all the tag string (href, title, text)=>"<a href='{0}' title={1}>{2}</a>".Formato(href,title,text); 
        
        public object Link {get; set; }

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

        MvcHtmlString html;
        public MvcHtmlString Html
        {
            get { return html; }
            set { html = value; }
        }

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

                if (Children.Count != 0)
                    return Children.Any(a => a.Visible);

                FindOptions findOptions = Link as FindOptions;
                if (findOptions != null)
                    return Navigator.IsFindable(findOptions.QueryName);

                return true;
            }

            set { visible = value; }
        }

        public string Class { get; set; }           //is applied to link
        public string ListItemClass { get; set; }   //is applied to list item
        
        public string ToString(string currentUrl, string rootClass) 
        {
            StringBuilder sb = new StringBuilder();
            this.Write(sb, currentUrl, rootClass, 0, "");
            return sb.ToString();
        }

        public string ToString(string currentUrl, string rootClass, string selectedRoute)
        {
            StringBuilder sb = new StringBuilder();
            this.Write(sb, currentUrl, rootClass, 0, selectedRoute);
            return sb.ToString();
        }

        void Write(StringBuilder sb, string currentUrl,  string rootClass, int depth, string selectedRoute)
        {
            if(!Visible)
                return;

            if (depth != 0)
                sb.AppendLine(("<li class=\"l{0}" + (string.IsNullOrEmpty(ListItemClass) ? "" : (" " + ListItemClass)) + "\">").Formato(depth));

            bool selectedSubmenu = false;
            if (Id != null && selectedRoute != null && selectedRoute.Split(' ').Contains(Id))
            {
                Class += " selected";
                selectedSubmenu = true;
            }

            if (Children.Any())
            {
                if (depth == 0)
                    sb.AppendLine("<ul class=\"{0}\">".Formato(rootClass));
                else
                {
                    //if the element is a link, write an A element
                    //otherwise, a span

                    if (Link != null)
                    {
                        sb.AppendLine(new HtmlTag("a")
                                         .Attrs(new { onmouseover = "", title = Title, href = Link.ToString(), id = Id })
                                         .Class(Class)
                                         .SetInnerText(Text)
                                         .ToHtml().ToHtmlString());
                    }
                    else
                    {
                        sb.AppendLine(new HtmlTag("span")
                                         .Attrs(new { onmouseover = "", title = Title, id = Id })
                                         .Class(Class)
                                         .SetInnerText(Text)
                                         .ToHtml().ToHtmlString());
                    }

                    if (selectedSubmenu)
                        sb.AppendLine("<ul class=\"submenu\" style=\"display:block\">");
                    else
                        sb.AppendLine("<ul class=\"submenu\">");
                }

                foreach (WebMenuItem menu in children)
                {
                    menu.Write(sb, currentUrl, rootClass, depth + 1, selectedRoute);
                }
                sb.Append("</ul>");
            }
            else if (Link != null)
            {
                string link = Link.ToString();
                
           /*     bool active = false;

                if (link.HasText() && currentUrl.EndsWith(link)) { active = true; }*/
                if (ManualA == null)
                {
                    HtmlTag tbA = /*active ? new FluentTagBuilder("span").AddCssClass("selected") :*/ new HtmlTag("a")
                            .Attrs(new {href = link, title = Title, id = Id})
                            .Class(Class);

                    if (!MvcHtmlString.IsNullOrEmpty(html))
                        tbA.InnerHtml(html);
                    else
                        tbA.SetInnerText(Text);
 
                    sb.Append(tbA.ToHtml().ToHtmlString());
                }
                else
                    sb.Append(ManualA(link, title, " ".CombineIfNotEmpty(Class, "selected")));
            }

            if (depth != 0)
                sb.Append("</li>");
        }
    }
}
