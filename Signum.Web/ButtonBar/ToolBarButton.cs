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
        public string Text { get; set; }
        public string AltText { get; set; }
        public string OnClick { get; set; }

        public static string DefaultEntityDivCssClass = "entity-button";
        public static string DefaultQueryCssClass = "query-button";

        private string divCssClass = "not-set";
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
            if (enabled)
                HtmlProps.Add("onclick", OnClick);
            else
                DivCssClass = DivCssClass + " disabled";

            return helper.Href(Id, Text, "", AltText ?? "", DivCssClass, HtmlProps);
        }
    }
}
