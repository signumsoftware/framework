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
    public class ToolBarButton
    {
        public string Id { get; set; }
        public string ImgSrc { get; set; }
        public string Text { get; set; }
        public string AltText { get; set; }
        public string OnClick { get; set; }

        public static string DefaultEntityDivCssClass = "entity-button";
        public static string DefaultQueryCssClass = "query-button";

        private string divCssClass;
        public string DivCssClass
        {
            get { return divCssClass; }
            set { divCssClass = value; }
        }

        private bool enabled = true;
        public bool Enabled
        {
            get { return enabled; }
            set { enabled = value; }
        }

        Dictionary<string, object> htmlProps = new Dictionary<string, object>(0);
        public Dictionary<string, object> HtmlProps
        {
            get { return htmlProps; }
        }

        public virtual MvcHtmlString ToHtml(HtmlHelper helper)
        {
            if (ImgSrc.HasText())
            {
                if (HtmlProps.ContainsKey("style"))
                    HtmlProps["style"] = "background:transparent url(" + ImgSrc + ")  no-repeat scroll -4px top; text-indent:12px; " + HtmlProps["style"].ToString();
                else
                    HtmlProps["style"] = "background:transparent url(" + ImgSrc + ")  no-repeat scroll -4px top; text-indent:12px;";
            }

            if (enabled)
            {
                HtmlProps.Add("onclick", OnClick);
            }
            else
                DivCssClass = DivCssClass + " disabled";


            return helper.Href(Id, Text, "#", AltText ?? "", DivCssClass, HtmlProps);
        }
    }
}
