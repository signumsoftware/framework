using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using Signum.Utilities;
using System.Reflection;
using System.IO;
namespace Signum.Web
{
    public static class AreaResourceHelper
    {
        public static Func<string[], string> InternalAreaJs
           = files => (files.ToString(f => "<script type='text/javascript' src=\"{0}\"></script>\n".Formato(f), ""));

        public static void IncludeAreaJs(this HtmlHelper html, params string[] files)
        {
            string include = InternalAreaJs(files);
            html.ViewContext.HttpContext.Response.Write(include);
        }
    }
}

