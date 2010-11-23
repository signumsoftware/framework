#region usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
#endregion

namespace Signum.Web
{
    public class ResourceTracker
    {
        const string resourceKey = "__resources";

        public List<string> _resources;

        public ResourceTracker(HttpContextBase context)
        {
            _resources = (List<string>)context.Items[resourceKey];
            if (_resources == null)
            {
                _resources = new List<string>();
                context.Items[resourceKey] = _resources;
            }
        }

        public void Add(string url)
        {
            _resources.Add(url.ToLower());
        }

        internal bool Contains(string url)
        {
            return _resources.Contains(url.ToLower());
        }
    }

    public static class ScriptExtensions
    {
        public static string DynamicScriptInclude(this HtmlHelper helper, params string[] url)
        {
            var tracker = new ResourceTracker(helper.ViewContext.HttpContext);
            
            var notLoadedUrls = new List<string>();
            foreach (var item in url)
            {
                if (!tracker.Contains(item))
                {
                    tracker.Add(item);
                    notLoadedUrls.Add(item);
                }
            }
            return helper.RegisterScripts(notLoadedUrls.ToArray());
        }

        public static string RegisterScripts(this HtmlHelper html, params string[] scriptUrls)
        {
            //TODO: Make non-blocking request keeping calls to library after having load
            //maybe through a stub
            StringBuilder sb = new StringBuilder();
            foreach (string url in scriptUrls)
            {
                sb.AppendFormat("<script type=\"text/javascript\" src=\"{0}\"></script>", url);
            }
            return sb.ToString();
        }

        public static void RegisterScriptInDll(this HtmlHelper html, Type type, string fullName)
        {
            string url = new ScriptManager().Page.ClientScript.GetWebResourceUrl(type, fullName);
            html.Write(html.RegisterScripts(url));
        }

        public static string DynamicCssInclude(this HtmlHelper helper, params string[] url)
        {
            var tracker = new ResourceTracker(helper.ViewContext.HttpContext);
            
            var notLoadedUrls = new List<string>();
            foreach (var item in url)
            {
                if (!tracker.Contains(item))
                {
                    tracker.Add(item);
                    notLoadedUrls.Add(item);
                }
            }
            return helper.RegisterCss(notLoadedUrls.ToArray());
        }

        public static string RegisterCss(this HtmlHelper html, params string[] cssUrls)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<script type='text/javascript'>");
            foreach (string url in cssUrls)
            {
                sb.AppendLine("var link=document.createElement('link');");
                sb.AppendLine("link.setAttribute('rel', 'stylesheet');");
                sb.AppendLine("link.setAttribute('type', 'text/css');");
                sb.AppendFormat("link.setAttribute('href', '{0}');", url);
                sb.AppendLine("var head = document.getElementsByTagName('head')[0];");
                sb.AppendLine("head.appendChild(link);");
            }
            sb.AppendLine("</script>");
            return sb.ToString();
        }
    }

    public class ScriptManager : Page
    {

    }
}
