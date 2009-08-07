using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Web.Mvc;
using System.Web;

namespace Signum.Web
{
    public class WebMenuItem
    {
        public string Id;
        public string ImgSrc;
        public string AltText;
        public string OnClick;
        /// <summary>
        /// Controller URL
        /// </summary>
        public string OnServerClickAjax;
        /// <summary>
        /// Controller URL
        /// </summary>
        public string OnServerClickPost;

        public readonly Dictionary<string, object> HtmlProps = new Dictionary<string, object>(0);
    }

    public delegate List<WebMenuItem> GetButtonBarElementDelegate(HttpContextBase httpContext, object entity, string mainControlUrl);
    public delegate List<WebMenuItem> GetButtonBarForQueryNameDelegate(HttpContextBase httpContext, object queryName, Type entityType); 

    public static class ButtonBarHelper
    {
        public static event GetButtonBarForQueryNameDelegate GetButtonBarForQueryName;
        public static event GetButtonBarElementDelegate GetButtonBarElement;

        public static string GetButtonBarElements(this HtmlHelper helper, object entity, string mainControlUrl, string prefix)
        {
            List<WebMenuItem> elements = new List<WebMenuItem>();
            if (GetButtonBarElement != null)
                elements.AddRange(GetButtonBarElement.GetInvocationList()
                    .Cast<GetButtonBarElementDelegate>()
                    .Select(d => d(helper.ViewContext.HttpContext, entity, mainControlUrl))
                    .NotNull().SelectMany(d => d).ToList());

            return ListMenuItemsToString(helper, elements, prefix);
        }

        public static string GetButtonBarElementsForQuery(this HtmlHelper helper, object queryName, Type entityType, string prefix)
        {
            List<WebMenuItem> elements = new List<WebMenuItem>();
            if (GetButtonBarElement != null)
                elements.AddRange(GetButtonBarForQueryName.GetInvocationList()
                    .Cast<GetButtonBarForQueryNameDelegate>()
                    .Select(d => d(helper.ViewContext.HttpContext, queryName, entityType))
                    .NotNull().SelectMany(d => d).ToList());

            return ListMenuItemsToString(helper, elements, prefix);
        }

        private static string ListMenuItemsToString(HtmlHelper helper, List<WebMenuItem> elements, string prefix)
        {
            StringBuilder sb = new StringBuilder();

            foreach (WebMenuItem mi in elements)
            {
                string onclick = "";
                string strPrefix = (prefix != null) ? ("'" + prefix.ToString() + "'") : "''";

                //Add prefix to onclick
                if (!string.IsNullOrEmpty(mi.OnClick))
                {
                    if (!string.IsNullOrEmpty(mi.OnServerClickAjax) || !string.IsNullOrEmpty(mi.OnServerClickPost))
                        throw new ArgumentException("The custom Menu Item {0} cannot have OnClick and another Click defined".Formato(mi.Id));

                    int lastEnd = mi.OnClick.LastIndexOf(")");
                    int lastStart = mi.OnClick.LastIndexOf("(");
                    if (lastStart == lastEnd - 1)
                        onclick = mi.OnClick.Insert(lastEnd, strPrefix);
                    else
                        onclick = mi.OnClick.Insert(lastEnd, ", " + strPrefix);
                }

                //Constructo OnServerClick
                if (!string.IsNullOrEmpty(mi.OnServerClickAjax))
                {
                    if (!string.IsNullOrEmpty(mi.OnClick) || !string.IsNullOrEmpty(mi.OnServerClickPost))
                        throw new ArgumentException("The custom Menu Item {0} cannot have both OnServerClickAjax and another Click defined".Formato(mi.Id));
                    onclick = mi.OnServerClickAjax;
                }

                //Constructo OnServerClick
                if (!string.IsNullOrEmpty(mi.OnServerClickPost))
                {
                    if (!string.IsNullOrEmpty(mi.OnClick) || !string.IsNullOrEmpty(mi.OnServerClickAjax))
                        throw new ArgumentException("The custom Menu Item {0} cannot have both OnServerClickPost and another Click defined".Formato(mi.Id));
                    onclick = "PostServer('{0}',{1});".Formato(mi.OnServerClickPost, strPrefix);
                }

                //Add cursor pointer to the htmlProps
                if (!mi.HtmlProps.ContainsKey("style"))
                    mi.HtmlProps.Add("style", "cursor: pointer");
                else if (mi.HtmlProps["style"].ToString().IndexOf("cursor") == -1)
                    mi.HtmlProps["style"] = "cursor:pointer; " + mi.HtmlProps["style"].ToString();

                mi.HtmlProps.Add("title", mi.AltText);

                if (mi.ImgSrc.HasText())
                {
                    mi.HtmlProps["style"] = "background:transparent url(" + mi.ImgSrc + ")  no-repeat scroll left top; " + mi.HtmlProps["style"].ToString();
                    //sb.Append(helper.ImageButton(mi.Id, mi.ImgSrc, mi.AltText, onclick, mi.HtmlProps));
                    sb.Append(helper.Div(mi.Id, "", "OperationDiv", mi.HtmlProps));
                }
                else
                {
                    mi.HtmlProps.Add("onclick", onclick);
                    sb.Append(helper.Div(mi.Id, mi.AltText, "OperationDiv", mi.HtmlProps));
                }
            }

            return sb.ToString();
        }
    }
}
