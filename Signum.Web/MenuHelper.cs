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

                throw new InvalidOperationException(Resources.TextNotSetForMenuItem0.Formato(Link.ToString()));
            }
            set { text = value; }
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
                sb.AppendLine("<li class='l{0}'>".Formato(depth));

            string fullClass = Class != null? " class='{1}'".Formato(Class): "";

            if (Children.Any())
            {
                if (depth == 0)
                    sb.AppendLine("<ul class='{0}'>".Formato(rootClass));
                else
                {
                    sb.AppendLine("<span title='{0}'{1}>{2}</span>".Formato(Title, fullClass, Text));
                    sb.AppendLine("<ul class='submenu'>");
                }

                foreach (WebMenuItem menu in children)
                {
                    menu.Write(sb, currentUrl, rootClass, depth + 1);
                }
                sb.Append("</ul>");
            }
            else
            {
                string link = Link.ToString(); 

                if (link.HasText() && currentUrl.EndsWith(link)) { sb.Append("<b>"); }
                if(ManualA == null)
                    sb.Append("<a href='{0}' title='{1}' {2}>{3}</a>".Formato(link, Title, fullClass, Text)); 
                else
                    sb.Append(ManualA(link, title, fullClass));
                if (link.HasText() && currentUrl.EndsWith(link)) { sb.Append("</b>"); }
            }

            if (depth != 0)
                sb.Append("</li>");
        }
    }
}
