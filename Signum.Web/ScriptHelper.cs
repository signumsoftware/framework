using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace Signum.Web
{
    public class ResourceTracker
    {
        const string resourceKey = "__resources";

        private List<string> _resources;

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
            url = url.ToLower();
            _resources.Add(url);
        }

        public bool Contains(string url)
        {
            url = url.ToLower();
            return _resources.Contains(url);
        }

    }

    public static class ScriptExtensions
    {
        public static string ScriptInclude(this HtmlHelper helper, params string[] url)
        {
            var tracker = new ResourceTracker(helper.ViewContext.HttpContext);

            var sb = new StringBuilder();
            foreach (var item in url)
            {
                if (!tracker.Contains(item))
                {
                    tracker.Add(item);
                    sb.AppendFormat("<script type='text/javascript' src='{0}'></script>", item);
                    sb.AppendLine();
                }
            }
            return sb.ToString();
        }

        public static string DynamicCssInclude(this HtmlHelper helper, string url)
        {
            var tracker = new ResourceTracker(helper.ViewContext.HttpContext);
            if (tracker.Contains(url))
                return String.Empty;

            var sb = new StringBuilder();
            sb.AppendLine("<script type='text/javascript'>");
            sb.AppendLine("var link=document.createElement('link')");
            sb.AppendLine("link.setAttribute('rel', 'stylesheet');");
            sb.AppendLine("link.setAttribute('type', 'text/css');");
            sb.AppendFormat("link.setAttribute('href', '{0}');", url);
            sb.AppendLine();
            sb.AppendLine("var head = document.getElementsByTagName('head')[0];");
            sb.AppendLine("head.appendChild(link);");
            sb.AppendLine("</script>");
            return sb.ToString();
        }
    }
}
