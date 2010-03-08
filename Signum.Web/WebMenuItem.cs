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
        
        private string divCssClass = "OperationDiv";
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

        public virtual string ToString(HtmlHelper helper, string prefix)
        {
            string onclick = "";
            string strPrefix = (prefix != null) ? ("'" + prefix.ToString() + "'") : "''";

            if (enabled)
            {
                //Add prefix to onclick
                if (!string.IsNullOrEmpty(OnClick))
                {
                    int lastEnd = OnClick.LastIndexOf(")");
                    int lastStart = OnClick.LastIndexOf("(");
                    if (lastStart == lastEnd - 1)
                        onclick = OnClick.Insert(lastEnd, strPrefix);
                    else
                        onclick = OnClick.Insert(lastEnd, ", " + strPrefix);
                }
            }

            HtmlProps.Add("title", AltText ?? "");

            if (ImgSrc.HasText())
            {
                HtmlProps["style"] = "background:transparent url(" + ImgSrc + ")  no-repeat scroll left top; " + HtmlProps["style"].ToString();
                return helper.Div(Id, "", DivCssClass, HtmlProps);
            }
            else
            {
                if (enabled)
                    HtmlProps.Add("onclick", onclick);
                else
                    DivCssClass = DivCssClass + " disabled"; 
                return helper.Div(Id, Text, DivCssClass, HtmlProps);
            }
        }
    }
}
