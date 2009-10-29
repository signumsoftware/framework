using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Signum.Utilities;
using Signum.Web.Properties;

namespace Signum.Web
{
    public class QuickLinkItem : WebMenuItem
    {
        public QuickLinkItem(object queryName, List<FilterOptions> filterOptions)
        {
            DivCssClass = "QuickLink";
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
                AltText = queryUrlName;
            
            if (!Id.HasText())
                Id = FindOptions.QueryName.ToString().Right(10, false);

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

            return "javascript:QuickLinkClickServerAjax('{0}','{1}','{2}');".Formato(
                controllerUrl,
                FindOptions.ToString(true, ""),
                prefix
                );
        }
    }
}
