using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Signum.Utilities;
using System.IO;
using System.Reflection;

namespace Signum.Web.ScriptCombiner
{
    public class CombinerScriptHtmlManager : BasicScriptHtmlManager
    {
        protected static string controllerCssRoute = "~/combine/css/{0}";
        protected static string controllerJsRoute = "~/combine/js/{0}";

        public override string CombinedScript(HtmlHelper html, string[] files, ScriptType scriptType)
        {
            if (files.Empty())
                return null;

            string contollerScriptRoute = scriptType == ScriptType.Css ? controllerCssRoute : controllerJsRoute;
            string scriptElement = scriptType == ScriptType.Css ? cssElement : jsElement;

            var urlHelper = new UrlHelper(html.ViewContext.RequestContext);

            string url = contollerScriptRoute.Formato(Combiner.GetKey(new ScriptRequest(files.Select(urlHelper.Content).ToArray(), scriptType, Version)));
            return scriptElement.Formato(Subdomain(url));
        }

        public override string[] GerUrlsFor(string[] files, ScriptType scriptType)
        {
            if (files.Empty())
                return null;

            string route = scriptType == ScriptType.Css ? controllerCssRoute : controllerJsRoute;
            return new string[] { Subdomain(route.Formato(Combiner.GetKey(new ScriptRequest(files.Select(Subdomain).ToArray(), scriptType, Version)))) };
        }
    }
}
