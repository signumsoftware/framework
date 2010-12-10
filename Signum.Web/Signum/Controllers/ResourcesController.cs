using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Signum.Web;
using System.Reflection;
using Signum.Utilities;
using System.IO;
using System.Web.Hosting;
using System.Web.UI;
using System.Web;
using System.IO.Compression;
using Signum.Web.PortableAreas;

namespace Signum.Web.Controllers
{
    public class ResourcesController : Controller
    {
        public ActionResult Index(string area, string resourcesFolder, string resourceName)
        {
            string contentType = GetContentType(resourceName);

            var file = HostingEnvironment.VirtualPathProvider.GetFile("~/{0}/{1}/{2}".Formato(area, resourcesFolder, resourceName));
            using (var str = file.Open())
            {
                return new ScriptContentResult(str.ReadAllBytes(), contentType);
            }
        }

        private static string GetContentType(string resourceName)
        {
            var extention = resourceName.Substring(resourceName.LastIndexOf('.')).ToLower();
            switch (extention)
            {
                case ".gif":
                    return "image/gif";
                case ".js":
                    return "text/javascript";
                case ".css":
                    return "text/css";
                default:
                    return "text/html";
            }
        }
    }

    public static class ResourcesUrlHelperExtensions
    {
        public static string Resource(this UrlHelper urlHelper, string resourceName)
        {
            var areaName = (string)urlHelper.RequestContext.RouteData.DataTokens["area"];
            return urlHelper.Action("Index", "Resource", new { resourceName = resourceName, area = areaName });
        }
    }
}
