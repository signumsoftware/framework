using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Signum.Utilities;

namespace Signum.Web.ScriptCombiner
{
    public static class CombinerHtmlHelper
    {
        public static string CombinedCssUrl(this HtmlHelper html, params string[] files)
        {
            return "Combine.aspx/CSS?f={0}".Formato(String.Join(",", files).Replace("/", "%2f"));
        }

        public static string CombinedCssUrlPath(this HtmlHelper html, string path, params string[] files)
        {
            return "Combine.aspx/CSS?f={0}&p={1}".Formato(String.Join(",", files).Replace("/", "%2f"), path.Replace("/", "%2f"));
        }

        public static void CombinedCss(this HtmlHelper html, params string[] files)
        {
            string cadena = "<link href=\"{0}\" rel='stylesheet' type='text/css' />".Formato(CombinedCssUrl(html, files));
            html.ViewContext.HttpContext.Response.Write(cadena);
        }

        public static string CombinedJsUrlPath(this HtmlHelper html, string path, params string[] files)
        {
            return "Combine.aspx/JS?f={0}&p={1}".Formato(String.Join(",", files).Replace("/", "%2f"), path.Replace("/", "%2f"));
        }
        public static string CombinedJsUrl(this HtmlHelper html, params string[] files)
        {
            return "Combine.aspx/JS?f={0}".Formato(String.Join(",", files).Replace("/", "%2f"));
        }
        public static void CombinedJs(this HtmlHelper html, params string[] files)
        {
            string cadena = "<script type='text/javascript' src=\"{0}\"></script>".Formato(CombinedJsUrl(html, files));
            html.ViewContext.HttpContext.Response.Write(cadena);
        }
    }
}
