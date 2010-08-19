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
    public enum CssMediaType { Screen, Print };
    public static class CombinerHtmlHelper
    {
        public static void CombinedCss(this HtmlHelper html, List<string> local, List<string> area)
        {

            string cadena = "<link href=\"{0}\" rel='stylesheet' type='text/css' />\n"
                .Formato(CombinedCssUrl(html, local, area));
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
            return "combine/cssmixed?" + path.Replace("/", "%2f");
        }


        public static string CombinedCssUrl(this HtmlHelper html, params string[] files)
        {
            return "combine/CSS?f={0}".Formato(String.Join(",", files).Replace("/", "%2f"));
        }

        public static string CombinedCssUrlPath(this HtmlHelper html, string path, params string[] files)
        {
            return "combine/CSS?f={0}&p={1}".Formato(String.Join(",", files).Replace("/", "%2f"), path.Replace("/", "%2f"));
        }

        public static void CombinedCss(this HtmlHelper html, string path, params string[] files)
        {
            string content = "";
#if (DEBUG)
            content = files.ToString(f => "<link href=\"{0}\" rel='stylesheet' type='text/css' />\n"
                .Formato(Path.Combine("content/", f)), "");
            html.ViewContext.HttpContext.Response.Write(content);
            return;
#endif
            string cadena = "<link href=\"{0}\" rel='stylesheet' type='text/css' />\n".Formato(CombinedCssUrlPath(html, path.Replace("/", "%2f"), files));
            html.ViewContext.HttpContext.Response.Write(cadena);
        }

        public static void CombinedCss(this HtmlHelper html, CssMediaType media, params string[] files)
        {
            string cadena = "<link href=\"{0}\" rel='stylesheet' type='text/css' media='{1}' />\n".Formato(CombinedCssUrl(html, files), media.ToString().ToLower());
            html.ViewContext.HttpContext.Response.Write(cadena);
        }

        public static string CombinedJsUrlPath(this HtmlHelper html, string path, params string[] files)
        {
            return "combine/js?f={0}&amp;p={1}".Formato(String.Join(",", files).Replace("/", "%2f"), path.Replace("/", "%2f"));
        }
        public static string CombinedJsUrl(this HtmlHelper html, params string[] files)
        {
            return "combine/js?f={0}".Formato(String.Join(",", files).Replace("/", "%2f"));
        }
        public static void CombinedJs(this HtmlHelper html, string path, params string[] files)
        {
            string content = "";
            #if (DEBUG)
                content = files.ToString(f => "<script type='text/javascript' src=\"{0}\"></script>\n".Formato(path + "/" + f), "");
                html.ViewContext.HttpContext.Response.Write(content);
                return;
            #endif
            content = "<script type='text/javascript' src=\"{0}\"></script>\n".Formato(CombinedJsUrlPath(html, path, files));
            html.ViewContext.HttpContext.Response.Write(content);
        }

        public static string IncludeAreaJs(params string[] files)
        {

            string content = "";

            content = "<script type='text/javascript' src=\"{0}\"></script>\n".Formato(IncludeAreaJsPath(files));
            return content;
        }

        static string IncludeAreaJsPath(params string[] files)
        {
            return "combine/areajs?f={0}&amp;v={1}".Formato(String.Join(",", files).Replace("/", "%2f"), ScriptCombiner.Common.Version);
        }

        public static void IncludeAreaCss(this HtmlHelper html, params string[] files)
        {
            string content = "";
#if (DEBUG)
            content = files.ToString(f => "<link href=\"{0}\" rel='stylesheet' type='text/css' />\n".Formato(f), "");
            html.ViewContext.HttpContext.Response.Write(content);
            return;
#endif
            content = "<link href=\"{0}\" rel='stylesheet' type='text/css' />\n".Formato(IncludeAreaCssPath(files));
            html.ViewContext.HttpContext.Response.Write(content);
        }

        static string IncludeAreaCssPath(params string[] files)
        {
            return "combine/areacss?f={0}&amp;v={1}".Formato(String.Join(",", files).Replace("/", "%2f"), ScriptCombiner.Common.Version);
        }
    }
}
