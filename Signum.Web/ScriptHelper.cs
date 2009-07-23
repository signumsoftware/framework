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
        public static string ScriptInclude(this HtmlHelper helper, params string[] url)
        {
            var tracker = new ResourceTracker(helper.ViewContext.HttpContext);

            var sb = new StringBuilder();
            bool print = (!AppSettings.ReadBoolean(AppSettingsKeys.MergeScriptsBottom,false));
            foreach (var item in url)
            {
                if (!tracker.Contains(item))
                {
                    tracker.Add(item);
                    if (print)
                    {
                        sb.AppendFormat("<script type='text/javascript' src='{0}'></script>", item);
                        sb.AppendLine();
                    }
                }
            }
            return sb.ToString();
        }

        public static string DynamicCssInclude(this HtmlHelper helper, params string[] url)
        {
            var tracker = new ResourceTracker(helper.ViewContext.HttpContext);
            //if (tracker.Contains(url))
            //    return String.Empty;

             bool print = (!AppSettings.ReadBoolean(AppSettingsKeys.MergeScriptsBottom,false));
             var sb = new StringBuilder();
             foreach (var item in url)
             {
                 if (!tracker.Contains(item))
                 {
                     tracker.Add(item);
                     if (print)
                     {
                         sb.AppendLine(helper.CssDynamic(item));
                     }
                 }
             }
             return sb.ToString();
        }
    }
}
