using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Web.Mvc;

namespace Signum.Web
{
    public class JsFindOptions : JsRenderer
    {
        public string Prefix { get; set; }
        /// <summary>
        /// Used to allow multiple search controls in one page
        /// </summary>
        public string Suffix { get; set; }
        public FindOptions FindOptions { get; set; }
        public int? Top { get; set; }
        /// <summary>
        /// To be called to open a Find window
        /// </summary>
        public string NavigatorControllerUrl { get; set; }
        /// <summary>
        /// To be called when clicking the search button
        /// </summary>
        public string SearchControllerUrl { get; set; }
        /// <summary>
        /// To be called when closing the popup (if exists) with the Ok button
        /// </summary>
        public string OnOk { get; set; }
        public string OnCancelled { get; set; }
        public bool? Async { get; set; }

        public JsFindOptions()
        {
            renderer = () =>
            {
                StringBuilder sb = new StringBuilder();

                if (Prefix.HasText())
                    sb.Append("prefix:'{0}',".Formato(Prefix));

                if (Suffix.HasText())
                    sb.Append("suffix:'{0}',".Formato(Suffix));

                if (Top.HasValue)
                    sb.Append("top:'{0}',".Formato(Top.Value));

                if (NavigatorControllerUrl.HasText())
                    sb.Append("navigatorControllerUrl:'{0}',".Formato(NavigatorControllerUrl));

                if (SearchControllerUrl.HasText())
                    sb.Append("searchControllerUrl:'{0}',".Formato(SearchControllerUrl));

                if (OnOk.HasText())
                    sb.Append("onOk:{0},".Formato(OnOk));

                if (OnCancelled.HasText())
                    sb.Append("onCancelled:{0},".Formato(OnCancelled));

                if (Async == true)
                    sb.Append("async:'{0}',".Formato(true));

                if (FindOptions != null)
                { 
                    if (FindOptions.QueryName != null)
                        sb.Append("queryUrlName:'{0}',".Formato(Navigator.Manager.QuerySettings[FindOptions.QueryName].UrlName));
                    
                    if (FindOptions.SearchOnLoad)
                        sb.Append("searchOnLoad:'{0}',".Formato(FindOptions.SearchOnLoad));

                    if (FindOptions.FilterMode != FilterMode.Visible)
                        sb.Append("filterMode:'{0}',".Formato(FindOptions.FilterMode.ToString()));

                    if (FindOptions.Create == false)
                        sb.Append("create:'{0}',".Formato(FindOptions.Create));

                    if (FindOptions.AllowMultiple.HasValue)
                        sb.Append("allowMultiple:'{0}',".Formato(FindOptions.AllowMultiple.Value));

                    if (FindOptions.FilterOptions != null && FindOptions.FilterOptions.Count > 0)
                    {
                        string filters = "";
                        for (int i = 0; i < FindOptions.FilterOptions.Count; i++)
                            filters += FindOptions.FilterOptions[i].ToString(i);
                        sb.Append("filters:'{0}',".Formato(filters));
                    }
                }

                string result = sb.ToString();

                return result.HasText() ? 
                    "{" + result.Substring(0, result.Length - 1) + "}" :
                    null; //Instead of "" so we can use Combine string extension
            };
        }
    }
}
