using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;

namespace Signum.Web
{
    public static class AjaxExtensions
    {
        private static string _microsoftAjaxLibraryUrl = "/Scripts/MicrosoftAjax.js";
        private static string _toolkitFolderUrl = "/Scripts/AjaxControlToolkit/3.0.20820.16598/3.0.20820.0/";

        public static string MicrosoftAjaxLibraryInclude(this HtmlHelper helper)
        {
            return ScriptExtensions.ScriptInclude(helper, _microsoftAjaxLibraryUrl);
        }

        public static string ToolkitInclude(this HtmlHelper helper, params string[] fileName)
        {
            var sb = new StringBuilder();
            foreach (string item in fileName)
            {
                var fullUrl = _toolkitFolderUrl + item;
                sb.AppendLine(ScriptExtensions.ScriptInclude(helper, fullUrl));
            }
            return sb.ToString();
        }

        public static string DynamicToolkitCssInclude(this HtmlHelper helper, string fileName)
        {
            var fullUrl = _toolkitFolderUrl + fileName;
            return helper.DynamicCssInclude(fullUrl);
        }

        public static string Create(this HtmlHelper helper, string clientType, string props, string elementId)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<script type='text/javascript'>");
            sb.AppendLine("Sys.Application.add_init(function(){");
            sb.AppendFormat("$create({0},{1},null,null,$get('{2}'))", clientType, (string.IsNullOrEmpty(props)) ? "null" : props, elementId);
            sb.AppendLine("});");
            sb.AppendLine("</script>");
            return sb.ToString();
        }

    }
}
