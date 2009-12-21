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
            if (!Text.HasText())
                Text = Navigator.SearchTitle(FindOptions.QueryName);

            if (OnClick.HasText() || OnServerClickAjax.HasText() || OnServerClickPost.HasText())
                return base.ToString(helper, prefix);

            OnServerClickPost = GetOnServerClickPost(queryUrlName, prefix);

            return base.ToString(helper, prefix);
        }

        //private string GetServerClickAjax(string queryUrlName, string prefix)
        //{
        //    if (OnClick.HasText() || OnServerClickPost.HasText())
        //        return null;

        //    string controllerUrl = "Signum/PartialFind";
        //    if (OnServerClickAjax.HasText())
        //        controllerUrl = OnServerClickAjax;

        //    return "javascript:QuickLinkClickServerAjax('{0}','{1}','{2}');".Formato(
        //        controllerUrl,
        //        FindOptions.ToString(true, ""),
        //        prefix
        //        );
        //}

        private string GetOnServerClickPost(string queryUrlName, string prefix)
        {
            if (OnClick.HasText() || OnServerClickAjax.HasText())
                return null;

            string controllerUrl = "Signum/Find";
            if (OnServerClickPost.HasText())
                controllerUrl = OnServerClickPost;

            return "PostServer('" + Navigator.FindRoute(FindOptions.QueryName) + FindOptions.ToString(false, true, "?") + "');";
        }
    }
}
