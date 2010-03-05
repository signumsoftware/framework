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

namespace Signum.Web
{
    public class Item
    {
        public FindOptions FindOptions { get; set; }
        public string ManualA {get; set;} //Specify all the tag string <a href='http://www.signumframework.com'>Signum</a>
        public string ManualHref {get; set;} //Specify the href element to be inserted in an A tag with the specified Title and Text
        public string Class { get; set; }
        /// <summary>
        /// Shortcut for "FindOptions = new FindOptions {QueryName = value}"
        /// </summary>
        public object QueryName 
        { 
            set 
            { 
                FindOptions = new FindOptions { QueryName = value }; 
            } 
        }
        
        string text;
        public string Text
        {
            get
            { 
                return text ??
                       ((FindOptions != null && FindOptions.QueryName != null) ?
                            (Navigator.Manager.QuerySettings[FindOptions.QueryName].TryCC(qs => qs.UrlName) ?? "") :
                            ""); 
            }
            set { text = value; }
        }

        string title;
        public string Title
        {
            get { return title ?? Text; }
            set { title = value; }
        }

        public List<Item> SubItems;

        public bool IsVisible()
        {
            if (SubItems != null && SubItems.Count > 0)
                return SubItems.Exists(mi => mi.IsVisible());

            if (FindOptions != null)
                return Navigator.IsFindable(FindOptions.QueryName);

            return ManualHref.HasText() || ManualA.HasText();
        }

        public bool CurrentPage(string link, string currentUrl)
        {
            if (!link.HasText() || !currentUrl.HasText())
                return false;
            return currentUrl.EndsWith(link);
        }

        public string ToString(string currentUrl)
        {
            StringBuilder sb = new StringBuilder();
            if (IsVisible())
            {
                sb.AppendLine("<li>");
                if (ManualHref.HasText())
                {
                    sb.AppendLine("<a href=\"{0}\" title=\"{1}\" class=\"{2}\">{3}</a>".Formato(
                        ManualHref,
                        Title,
                        CurrentPage(ManualHref, currentUrl) ? Class + " active": Class,
                        Text));
                }
                else if (ManualA.HasText())
                {
                    sb.AppendLine(ManualA);
                }
                else
                {
                    sb.AppendLine("<a href=\"{0}\" title=\"{1}\" class=\"{2}\">{3}</a>".Formato(
                        Navigator.FindRoute(FindOptions.QueryName) + FindOptions.ToString(false, true, "?"),
                        Title,
                        CurrentPage(Navigator.FindRoute(FindOptions.QueryName) + FindOptions.ToString(false, true, "?"), currentUrl) ? Class + " active" : Class,
                        Text));
                }
            }
            else
            {
                if (FindOptions == null && ManualA == null && ManualHref == null)
                    sb.AppendLine("<li>{1}".Formato(Text));
            }

            return sb.ToString();
        }
    }


    public class OrderedMenu {
        string currentUrl;
        string @class;
        public OrderedMenu() {
        }
        public string ToString(string currentUrl)
        {
            return this.ToString(currentUrl, "");
        }

        public string ToString(string currentUrl, string @class) {
            StringBuilder sb = new StringBuilder();
            this.currentUrl = currentUrl;
            this.@class = @class;
            sb.Append(this.ToString(0));
            return sb.ToString();
        }

        private string ToString(int i) {
            StringBuilder sb = new StringBuilder();
            if (node != null)
                sb.Append(NodeToString(i, children == null ? 0 : children.Count)); // + NodeToString(i, children.Count)
           /* else {
                sb.Append("<li>Unknown</li>");
            }*/
            if (children != null && children.Count > 0)
            {
                if (i == 0)
                    sb.AppendLine("<ul class='{0}'>".Formato(this.@class));
                else
                    sb.AppendLine("<ul{0}>".Formato((i > 0) ? " class='submenu'" : ""));
                foreach (OrderedMenu menu in children)
                {
                    sb.Append(menu.ToString(i+1));
                }
                sb.Append("</ul>");
            }
            if (i>0)
                sb.Append("</li>");
            return sb.ToString();
        }

        private string NodeToString(int i, int children)
        {
            StringBuilder sb = new StringBuilder();
            if (node.IsVisible() && children>0) {
                sb.AppendLine("<li class='l{0}'>".Formato(i));
                sb.AppendLine("<span title='{0}' class=\"{1}\">{2}</span>".Formato(node.Title, node.Class, node.Text));
              //  sb.AppendLine("</li>");
            }
            else
            {
                if (node.IsVisible())
                {
                    sb.AppendLine("<li class='l{0}'>".Formato(i));
                    if (node.ManualHref.HasText())
                    {
                        if (node.ManualHref == currentUrl) { sb.Append("<b>"); }
                        sb.AppendLine("<a href='{0}' title='{1}'>{2}</a>".Formato(node.ManualHref, node.Title, node.Text));
                        if (node.ManualHref == currentUrl) { sb.Append("/<b>"); }
                    }
                    else if (node.ManualA.HasText())
                    {
                        if (node.ManualHref == currentUrl) { sb.Append("<b>"); }
                        sb.AppendLine(node.ManualA);
                        if (node.ManualHref == currentUrl) { sb.Append("</b>"); }
                    }
                    else
                    {
                        if (Navigator.FindRoute(node.FindOptions.QueryName) + node.FindOptions.ToString(false, true, "?") == currentUrl) { sb.Append("<b>"); }
                        sb.AppendLine("<a href='{0}' title='{1}'>{2}</a>".Formato(Navigator.FindRoute(node.FindOptions.QueryName) + node.FindOptions.ToString(false, true, "?"), node.Title, node.Text));
                        if (Navigator.FindRoute(node.FindOptions.QueryName) + node.FindOptions.ToString(false, true, "?") == currentUrl) { sb.Append("<b>"); }
                    }
                }
                else {
                    if (node.FindOptions == null && node.ManualA == null && node.ManualHref == null)
                        sb.AppendLine("<li class='{0}'>{1}".Formato(i, node.Text));
                }
            }

            return sb.ToString();
        }

        private List<OrderedMenu> children; //Bug: Doesn't update the real item subItems so they are not rendered;
        public List<OrderedMenu> Children //Made as property as a quick fix for the bug. TODO Anto: Revisar esto
        {
            get { return children; }
            set 
            {
                children = value;
                if (node != null)
                    node.SubItems = children.Select(om => om.node).ToList(); 
            }
        }
        public Item node;
    }

    public static class MenuHelper
    {
        public static void MenuLI(this HtmlHelper helper, Item menuItem)
        {
            helper.ViewContext.HttpContext.Response.Write(MenuItemToString(menuItem));
        }

        public static void MenuLI(this HtmlHelper helper, string text, List<Item> children)
        {
            StringBuilder sb = new StringBuilder();

            if (children.Exists(mi => (mi.FindOptions == null && mi.ManualHref.HasText()) || Navigator.IsFindable(mi.FindOptions.QueryName)))
            {
                sb.AppendLine("<li>");
                sb.AppendLine("<span title='{0}'>{0}</span>".Formato(text));
                sb.AppendLine("<ul class='submenu'>");
                foreach (Item mi in children)
                    sb.Append(MenuItemToString(mi));
                sb.AppendLine("</ul>");
                sb.AppendLine("</li>");
            }

            helper.ViewContext.HttpContext.Response.Write(sb.ToString());
        }

        private static string MenuItemToString(Item menuItem)
        {
            StringBuilder sb = new StringBuilder();

            if (menuItem.SubItems != null && menuItem.SubItems.Count > 0 && menuItem.SubItems.Exists(mi => mi.IsVisible()))
            {
                sb.AppendLine("<li>");
                sb.AppendLine("<span title='{0}'>{1}</span>".Formato(menuItem.Title, menuItem.Text));
                sb.AppendLine("<ul class='submenu'>");
                foreach (Item mi in menuItem.SubItems)
                    sb.Append(MenuItemToString(mi));
                sb.AppendLine("</ul>");
                sb.AppendLine("</li>");
            }
            else
            {
                if (menuItem.IsVisible())
                {
                    sb.AppendLine("<li>");
                    if (menuItem.ManualHref.HasText())
                        sb.AppendLine("<a href='{0}' title='{1}'>{2}</a>".Formato(menuItem.ManualHref, menuItem.Title, menuItem.Text));
                    else if (menuItem.ManualA.HasText())
                        sb.AppendLine(menuItem.ManualA);
                    else
                        sb.AppendLine("<a href='{0}' title='{1}'>{2}</a>".Formato(Navigator.FindRoute(menuItem.FindOptions.QueryName) + menuItem.FindOptions.ToString(false, true, "?"), menuItem.Title, menuItem.Text));
                    sb.AppendLine("</li>");
                }
            }

            return sb.ToString();
        }
    }
}
