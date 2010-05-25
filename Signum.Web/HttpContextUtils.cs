using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Signum.Utilities;

namespace Signum.Web
{
    public class HttpContextUtils
    {
        public static string FullyQualifiedApplicationPath
        {
            get
            {
                HttpContext context = HttpContext.Current;
                if (context == null)
                    return null;

                string appPath = "{0}://{1}{2}{3}".Formato(
                      context.Request.Url.Scheme,
                      context.Request.Url.Host,
                      context.Request.Url.Port == 80 ? string.Empty : ":" + context.Request.Url.Port,
                      context.Request.ApplicationPath);

                if (!appPath.EndsWith("/"))
                    appPath += "/";

                return appPath;
            }
        }
    }
}
