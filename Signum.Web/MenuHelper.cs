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
    }

    public static class MenuHelper
    {
        public static void MenuLI(this HtmlHelper helper, Item menuItem)
        {
            StringBuilder sb = new StringBuilder();

            if (Navigator.IsFindable(menuItem.QueryName))
                sb.Append(
                    "<li>" +
                    "<a href='{0}' title='{1}'>{2}</a>".Formato(Navigator.FindRoute(menuItem.QueryName), menuItem.Title, menuItem.Text) +
                    "</li>\n"
                    );

            helper.ViewContext.HttpContext.Response.Write(sb.ToString());
        }

        public static void MenuLI(this HtmlHelper helper, string text, string title, List<Item> children)
        {
            if (children.Exists(mi => Navigator.IsFindable(mi.QueryName)))
            {
                helper.ViewContext.HttpContext.Response.Write(
                    "<li>\n" +
                    "<span onclick=\"$('#ul{0}').toggle();(this.className == 'active') ? this.className='' : this.className='active';\" title='{1}'>{0}</span>\n".Formato(text, title) +
                    "<ul class='submenu' id='ul{0}'>".Formato(text)
                    );

                foreach (Item mi in children)
                    helper.MenuLI(mi);

                helper.ViewContext.HttpContext.Response.Write(
                    "</ul>\n" + 
                    "</li>\n"
                    );
            }
        }
    }
}
