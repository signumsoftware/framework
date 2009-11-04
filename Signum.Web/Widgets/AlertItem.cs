using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Signum.Utilities;
using Signum.Web.Properties;

namespace Signum.Web
{
    public class AlertItem : WebMenuItem
    {
        public AlertItem(object queryName, List<FilterOptions> filterOptions)
        {
            DivCssClass = "Alert";
            FindOptions = new FindOptions 
            { 
                QueryName = queryName,
                FilterOptions = filterOptions,
                AllowMultiple = false,
                FilterMode = FilterMode.Hidden,
                SearchOnLoad = true,
                Create = false,
            };
        }

        public FindOptions FindOptions { get; set; }

        public override string ToString(HtmlHelper helper, string prefix)
        {
            string queryUrlName = Navigator.Manager.QuerySettings[FindOptions.QueryName].UrlName;
            if (!AltText.HasText())
                AltText = Navigator.SearchTitle(FindOptions.QueryName);
                        
            if (OnClick.HasText() || OnServerClickAjax.HasText() || OnServerClickPost.HasText())
                return base.ToString(helper, prefix);

            OnServerClickAjax = GetServerClickAjax(queryUrlName, prefix);

            return base.ToString(helper, prefix);
        }

        private string GetServerClickAjax(string queryUrlName, string prefix)
        {
            if (OnClick.HasText() || OnServerClickPost.HasText())
                return null;

            string controllerUrl = "Signum.aspx/PartialFind";
            if (OnServerClickAjax.HasText())
                controllerUrl = OnServerClickAjax;

            return "javascript:AlertClickServerAjax('{0}','{1}','{2}');".Formato(
                controllerUrl,
                FindOptions.ToString(true, ""),
                prefix
                );
        }
    }
}
