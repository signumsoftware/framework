using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Web.Mvc;
using System.Web;
using Signum.Web.Properties;

namespace Signum.Web
{
    public class WebMenuItem
    {
        public string Id { get; set; }
        public string ImgSrc { get; set; }
        public string AltText { get; set; }
        public string OnClick { get; set; }
        /// <summary>
        /// Controller URL
        /// </summary>
        public string OnServerClickAjax { get; set; }
        /// <summary>
        /// Controller URL
        /// </summary>
        public string OnServerClickPost { get; set; }

        private string divCssClass = "OperationDiv";
        public string DivCssClass 
        { 
            get { return divCssClass; } 
            set { divCssClass = value; } 
        }

        Dictionary<string, object> htmlProps = new Dictionary<string, object>(0);
        public Dictionary<string, object> HtmlProps
        {
            get { return htmlProps; }
        }

        public virtual string ToString(HtmlHelper helper, string prefix)
        {
            string onclick = "";
            string strPrefix = (prefix != null) ? ("'" + prefix.ToString() + "'") : "''";

            //Add prefix to onclick
            if (!string.IsNullOrEmpty(OnClick))
            {
                if (!string.IsNullOrEmpty(OnServerClickAjax) || !string.IsNullOrEmpty(OnServerClickPost))
                    throw new ArgumentException(Resources.MenuItem0HasOnClickAndAnotherClickDefined.Formato(Id));

                int lastEnd = OnClick.LastIndexOf(")");
                int lastStart = OnClick.LastIndexOf("(");
                if (lastStart == lastEnd - 1)
                    onclick = OnClick.Insert(lastEnd, strPrefix);
                else
                    onclick = OnClick.Insert(lastEnd, ", " + strPrefix);
            }

            //Constructo OnServerClick
            if (!string.IsNullOrEmpty(OnServerClickAjax))
            {
                if (!string.IsNullOrEmpty(OnClick) || !string.IsNullOrEmpty(OnServerClickPost))
                    throw new ArgumentException(Resources.MenuItem0HasOnServerClickAjaxAndAnotherClickDefined.Formato(Id));
                onclick = OnServerClickAjax;
            }

            //Constructo OnServerClick
            if (!string.IsNullOrEmpty(OnServerClickPost))
            {
                if (!string.IsNullOrEmpty(OnClick) || !string.IsNullOrEmpty(OnServerClickAjax))
                    throw new ArgumentException(Resources.MenuItem0HasOnServerClickPostAndAnotherClickDefined.Formato(Id));
                onclick = "PostServer('{0}');".Formato(OnServerClickPost);
            }

            //Add cursor pointer to the htmlProps
            if (!HtmlProps.ContainsKey("style"))
                HtmlProps.Add("style", "cursor: pointer");
            else if (HtmlProps["style"].ToString().IndexOf("cursor") == -1)
                HtmlProps["style"] = "cursor:pointer; " + HtmlProps["style"].ToString();

            HtmlProps.Add("title", AltText);

            if (ImgSrc.HasText())
            {
                HtmlProps["style"] = "background:transparent url(" + ImgSrc + ")  no-repeat scroll left top; " + HtmlProps["style"].ToString();
                //sb.Append(helper.ImageButton(mi.Id, mi.ImgSrc, mi.AltText, onclick, mi.HtmlProps));
                return helper.Div(Id, "", DivCssClass, HtmlProps);
            }
            else
            {
                HtmlProps.Add("onclick", onclick);
                return helper.Div(Id, AltText, DivCssClass, HtmlProps);
            }
        }
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
                sb.AppendLine(mi.ToString(helper, prefix));
            }

            return sb.ToString();
        }
    }
}
