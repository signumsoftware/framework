using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Signum.Utilities;
using System.IO;
using System.Reflection;
using System.Web;

namespace Signum.Web.ScriptCombiner
{
    public class CombinerScriptHtmlManager : BasicScriptHtmlManager
    {
        protected static string controllerCssRoute = "~/combine/css/{0}";
        protected static string controllerJsRoute = "~/combine/js/{0}";

        public override MvcHtmlString CombinedScript(HtmlHelper html, string[] files, ScriptType scriptType)
        {
            if (files.Empty())
                return null;

            var key = GetKey(files, scriptType); 

            string url = (scriptType == ScriptType.Css ? controllerCssRoute : controllerJsRoute).Formato(key);
            return MvcHtmlString.Create((scriptType == ScriptType.Css ? cssElement : jsElement).Formato(Subdomain(url)));
        }

        private string GetKey(string[] files, ScriptType scriptType)
        {   
            return Combiner.GetKey(new ScriptRequest(files.Select(VirtualPathUtility.ToAbsolute).ToArray(), scriptType, Version));
        }

        public override string[] GerUrlsFor(string[] files, ScriptType scriptType)
        {
            if (files.Empty())
                return null;

            string key = GetKey(files, scriptType);

            string route = (scriptType == ScriptType.Css ? controllerCssRoute : controllerJsRoute).Formato(key);
            
            return new string[] { Subdomain(route) };
        }
    }
}
