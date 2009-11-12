using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using System.Web.Mvc;
using Signum.Utilities;
using Signum.Engine;
using Signum.Entities;
using Signum.Engine.Maps;
using Signum.Web.Properties;
using Signum.Engine.DynamicQuery;
using Signum.Entities.Reflection;
using Signum.Entities.DynamicQuery;
using System.Reflection;
using System.Collections;
using System.Collections.Specialized;

namespace Signum.Web.Controllers
{
    public class ErrorController : Controller
    {
        public static Action<HandleErrorInfo> LogException;

        public ActionResult Http404(string url) {
            Response.StatusCode = 404;
            ViewData["url"] = url;

            if (LogException != null) {
                HandleErrorInfo hei = new HandleErrorInfo(
                    new Exception("Error 404 accessing url '" + url + "'"),
                    (string)this.RouteData.Values["controller"],
                    (string)this.RouteData.Values["action"]);
                LogException(hei);
            }
            return View();
        }
    }
}
