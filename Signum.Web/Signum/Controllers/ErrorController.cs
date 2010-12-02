using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Signum.Web
{
    public class ErrorController : Controller
    {
        public ActionResult Error()
        {
            Exception ex = HttpContext.Session[HandleExceptionAttribute.ErrorSessionKey] as Exception;
            HttpContext.Application.Remove(Request.UserHostAddress);
            ViewData.Model = ex;
            return View(Navigator.Manager.ErrorPageUrl);
        }

    }
}
