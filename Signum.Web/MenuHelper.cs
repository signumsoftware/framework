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
        
        string text;
        public string Text
        {
            get
            {
                if (text.HasText())
                    return text;

                FindOptions findOptions = Link as FindOptions;
                if (findOptions != null)
                    return QueryUtils.GetNiceQueryName(findOptions.QueryName);

                throw new InvalidOperationException("Text not set for menu item {0}".Formato(Link.ToString()));
            }
            set { text = value; }
        }

        string html;
        public string Html
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

        public string Class { get; set; }
        
        public string ToString(string currentUrl, string rootClass) 
        {
            StringBuilder sb = new StringBuilder();
            this.Write(sb, currentUrl, rootClass, 0);
            return sb.ToString();
        }

        void Write(StringBuilder sb, string currentUrl,  string rootClass, int depth)
        {
            if(!Visible)
                return;

            if (depth != 0)
                sb.AppendLine("<li class=\"l{0}\">".Formato(depth));

            if (Children.Any())
            {
                if (depth == 0)
                    sb.AppendLine("<ul class=\"{0}\">".Formato(rootClass));
                else
                {
                    sb.AppendLine(new FluentTagBuilder("span")
                                     .MergeAttributes(new {onmouseover = "", title = Title})
                                     .AddCssClass(Class)
                                     .SetInnerText(Text)
                                     .ToString(TagRenderMode.Normal));

                    sb.AppendLine("<ul class=\"submenu\">");
                }

                foreach (WebMenuItem menu in children)
                {
                    menu.Write(sb, currentUrl, rootClass, depth + 1);
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
                    FluentTagBuilder tbA = /*active ? new FluentTagBuilder("span").AddCssClass("selected") :*/ new FluentTagBuilder("a")
                            .MergeAttributes(new {href = link, title = Title})
                            .AddCssClass(Class);

                    if (!string.IsNullOrEmpty(html))
                        tbA.InnerHtml(html);
                    else
                        tbA.SetInnerText(Text);
 
                    sb.Append(tbA.ToString(TagRenderMode.Normal));
                }
                else
                    sb.Append(ManualA(link, title, " ".CombineIfNotEmpty(Class, "selected")));
            }

            if (depth != 0)
                sb.Append("</li>");
        }
    }
}
