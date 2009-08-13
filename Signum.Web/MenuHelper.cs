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
        public string ManualA {get; set;} //Specify all the tag string <a href='http://www.signumframework.com'>Signum</a>
        public string ManualHref {get; set;} //Specify the href element to be inserted in an A tag with the specified Title and Text
        public object QueryName {get; set;}
        
        string text;
        public string Text
        {
            get
            { 
                return text ??
                       ((QueryName != null) ?
                            (Navigator.Manager.QuerySettings[QueryName].TryCC(qs => qs.UrlName) ?? "") :
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
            if (QueryName != null)
                return Navigator.IsFindable(QueryName);

            return ManualHref.HasText() || ManualA.HasText();
        }
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

            if (children.Exists(mi => (mi.QueryName == null && mi.ManualHref.HasText()) || Navigator.IsFindable(mi.QueryName)))
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
                        sb.AppendLine("<a href='{0}' title='{1}'>{2}</a>".Formato(Navigator.FindRoute(menuItem.QueryName), menuItem.Title, menuItem.Text));
                    sb.AppendLine("</li>");
                }
            }

            return sb.ToString();
        }
    }
}
