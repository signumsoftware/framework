using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Collections.Concurrent;
using Signum.Utilities;
using System.Web.Mvc;
using System.Web.Hosting;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Reflection;
using System.Web.Routing;
using Signum.Web.Controllers;
using Signum.Web.PortableAreas;
using Signum.Engine.Engine;


namespace Signum.Web.Combine
{
    public class ScriptRequest : IEquatable<ScriptRequest>
    {
        public readonly ScriptType ScriptType;
        public readonly string[] VirtualFiles;
        readonly string toString;

        public ScriptRequest(string[] files, ScriptType scriptType, string version)
        {
            this.VirtualFiles = files;
            this.ScriptType = scriptType;
            this.toString = string.Join(",", VirtualFiles) + "," + version;
        }

        public bool Equals(ScriptRequest other)
        {
            return other.toString == toString;
        }

        public override string ToString()
        {
            return toString;
        }

        public override int GetHashCode()
        {
            return toString.GetHashCode();
        }
    }

    public static class CombineClient
    {
        class ScriptElement
        {
            public ScriptRequest Request;
            public StaticContentResult Content;
            public string Key;
        }

        static ConcurrentDictionary<string, ScriptElement> ElementsByKey = new ConcurrentDictionary<string, ScriptElement>();

        static ConcurrentDictionary<ScriptRequest, ScriptElement> Elements = new ConcurrentDictionary<ScriptRequest, ScriptElement>();


        public static void Clear()
        {
            ElementsByKey.Clear();
            Elements.Clear();
        }

        public static string GetKey(ScriptRequest request)
        {
            return Elements.GetOrAdd(request, r =>
            {
                var elem = new ScriptElement
                {
                    Request = r,
                    Content = null,
                    Key = StringHashEncoder.Codify(request.ToString())
                };

                return ElementsByKey.GetOrAdd(elem.Key, elem);
            }
            ).Key;
        }

        public static StaticContentResult GetContent(string key)
        {
            ScriptElement elem;
            if (!ElementsByKey.TryGetValue(key, out elem))
                throw new KeyNotFoundException("Script '{0}' not found".Formato(key));

            if (elem.Content != null)
                return elem.Content;

            elem.Content = Generate(elem.Request);

            return elem.Content;
        }

        private static StaticContentResult Generate(ScriptRequest scriptRequest)
        {
            switch (scriptRequest.ScriptType)
            {
                case ScriptType.Css: return CssCombiner.Combine(scriptRequest.VirtualFiles);
                case ScriptType.Javascript: return JavascriptCombiner.Combine(scriptRequest.VirtualFiles);
                default: throw new InvalidOperationException();
            }
        }

        public static string ReadVirtualFile(string virtualPath)
        {
            VirtualFile vf = HostingEnvironment.VirtualPathProvider.GetFile(virtualPath);
            using (Stream str = vf.Open())
            using (StreamReader reader = new StreamReader(str))
                return reader.ReadToEnd();

        }

        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                ScriptHtmlHelper.Manager = new CombinerScriptHtmlManager();

                Navigator.RegisterArea(typeof(CombineClient));


                Route routeCss = new Route("combine/css/{key}", new MvcRouteHandler());
                routeCss.Defaults = new RouteValueDictionary(new { controller = "Combine", action = "CSS", key = "" });

                RouteTable.Routes.Insert(0, routeCss);

                Route routeJs = new Route("combine/js/{key}", new MvcRouteHandler());
                routeJs.Defaults = new RouteValueDictionary(new { controller = "Combine", action = "JS", key = "" });

                RouteTable.Routes.Insert(0, routeJs);
            }
        }
    }
}