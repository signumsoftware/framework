using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Signum.Utilities;
using Signum.Web.ScriptCombiner;
using System.IO;

namespace Signum.Web
{
    public static class CombinerHtmlHelper
    {

        //useful for loading static resources such as JS and CSS files
        //from a different subdomain (real or virtual)
        public static Func<string, string> Subdomain = (s) => s;

        static string cssElement = "<link href=\"{0}\" rel='stylesheet' type='text/css' />";
        static string jsElement = "<script type='text/javascript' src=\"{0}\"></script>";

        static string version = ScriptCombiner.Common.Version;

        public static void CombinedCss(this HtmlHelper html, List<string> local, List<string> area)
        {
            string cadena = cssElement.Formato(CombinedCssUrl(html, local, area));
            html.ViewContext.HttpContext.Response.Write(cadena);
        }

        public static string CombinedCssUrl(this HtmlHelper html, List<string> local, List<string> area)
        {
            string path = "";
            bool started = false;
            if (local != null && local.Count > 0) {
                started = true;
                path += "l={0}".Formato(String.Join(",", local.ToArray()));
            }
            if (area != null && area.Count > 0) {
                if (started) path += "&";
                path += "a={0}".Formato(String.Join(",", area.ToArray()));
            }
            return Subdomain("combine/cssmixed?v={0}&{1}"
                .Formato(version,
                        path.Replace("/", "%2f")));
        }


        public static string CombinedCssUrl(params string[] files)
        {
            return Subdomain("combine/CSS?f={0}&v={1}".Formato(String.Join(",", files).Replace("/", "%2f"), version));
        }

        public static string CombinedCssUrlPath(string path, params string[] files)
        {
            return Subdomain("combine/CSS?f={0}&p={1}&v={2}".Formato(String.Join(",", files).Replace("/", "%2f"), path.Replace("/", "%2f"), version));
        }

        public static void CombinedCss(this HtmlHelper html, string path, params string[] files)
        {
#if (DEBUG)
            string content = files.ToString(f => cssElement.Formato(Path.Combine("content/", f) + "?v=" + version), "");
            html.ViewContext.HttpContext.Response.Write(content);
#else
            string cadena =  cssElement.Formato(CombinedCssUrlPath(path.Replace("/", "%2f"), files));
            html.ViewContext.HttpContext.Response.Write(cadena);
#endif
        }

        public static string CombinedJsUrlPath(string path, params string[] files)
        {
            return Subdomain("combine/js?f={0}&amp;p={1}&v={2}".Formato(String.Join(",", files).Replace("/", "%2f"), path.Replace("/", "%2f"), version));
        }
        public static string CombinedJsUrl(params string[] files)
        {
            return Subdomain("combine/js?f={0}&v={1}".Formato(String.Join(",", files).Replace("/", "%2f"), version));
        }
        public static void CombinedJs(this HtmlHelper html, string path, params string[] files)
        {
#if (DEBUG)
            string content = files.ToString(f => jsElement.Formato(path + "/" + f + "?v=" + ScriptCombiner.Common.Version), "");
            html.ViewContext.HttpContext.Response.Write(content);
#else
            string content = jsElement.Formato(CombinedJsUrlPath(path, files));
            html.ViewContext.HttpContext.Response.Write(content);
#endif

        }

        public static string IncludeAreaJs(params string[] files)
        {
#if (DEBUG)
                return files.ToString(f => jsElement.Formato(f + "?v=" + ScriptCombiner.Common.Version), "");
#else
            return jsElement.Formato(IncludeAreaJsUrl(files));
#endif        
            }

        public static string IncludeAreaJsUrl(params string[] files)
        {
            return Subdomain("combine/areajs?f={0}&amp;v={1}".Formato(String.Join(",", files).Replace("/", "%2f"), version));
        }

        public static void IncludeAreaCss(this HtmlHelper html, params string[] files)
        {
#if (DEBUG)
            string content = files.ToString(f => cssElement.Formato(f + "?v=" + version), "");
            html.ViewContext.HttpContext.Response.Write(content);
#else
            string content = cssElement.Formato(IncludeAreaCssPath(files));
            html.ViewContext.HttpContext.Response.Write(content);
#endif
        }

        static string IncludeAreaCssPath(params string[] files)
        {
            return Subdomain("combine/areacss?f={0}&amp;v={1}".Formato(String.Join(",", files).Replace("/", "%2f"), version));
        }
    }
}
