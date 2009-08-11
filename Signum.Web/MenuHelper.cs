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
        public object QueryName;
        public string Title;
        public string Text;
        public List<Item> SubItems;
    }

    public static class MenuHelper
    {
        public static string MenuLI(this HtmlHelper helper, Item menuItem, string prefix)
        {
            StringBuilder sb = new StringBuilder();

            string idUL = prefix + "_" + "ul" + menuItem.Text.Replace(" ", "");
            string idLI = prefix + "_" + "li" + menuItem.Text.Replace(" ", "");
            if (menuItem.SubItems != null && menuItem.SubItems.Count > 0 && menuItem.SubItems.Exists(mi => Navigator.IsFindable(mi.QueryName)))
            {
                sb.AppendLine("<li id='{0}'>".Formato(idLI));
                sb.AppendLine("<span title='{1}'>{2}</span>".Formato(idUL, menuItem.Title, menuItem.Text));
                sb.AppendLine("<ul id='{0}'>".Formato(idUL));
                foreach (Item mi in menuItem.SubItems)
                    sb.Append(helper.MenuLI(mi, idUL));
                sb.AppendLine("</ul>");
                sb.AppendLine("</li>");
            }
            else
            {
                if (Navigator.IsFindable(menuItem.QueryName))
                    sb.Append(
                        "<li id='{0}'>".Formato(idLI) +
                        "<a href='{0}' title='{1}'>{2}</a>".Formato(Navigator.FindRoute(menuItem.QueryName), menuItem.Title, menuItem.Text) +
                        "</li>\n"
                        );
            }

            return sb.ToString();
        }

        public static void MenuLI(this HtmlHelper helper, string text, string title, List<Item> children)
        {
            StringBuilder sb = new StringBuilder();

            if (children.Exists(mi => Navigator.IsFindable(mi.QueryName)))
            {
                string idUL = "ul" + text.Replace(" ", "");

                sb.Append(
                    "<li id='{0}'>\n".Formato("li" + text.Replace(" ", "")) +
                    "<span title='{1}'>{2}</span>\n".Formato(idUL, title, text) +
                    "<ul class='submenu' id='{0}'>".Formato(idUL)
                    );

                foreach (Item mi in children)
                    sb.Append(helper.MenuLI(mi, idUL));

                sb.Append(
                    "</ul>\n" + 
                    "</li>\n"
                    );
            }

            helper.ViewContext.HttpContext.Response.Write(sb.ToString());
        }
    }
}
