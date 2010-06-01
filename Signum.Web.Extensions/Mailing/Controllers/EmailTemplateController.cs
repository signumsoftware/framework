#region usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Ajax;
using Signum.Utilities;
#endregion

namespace Signum.Web.Mailing
{
    public class EmailTemplateController : Controller
    {
        public ActionResult EmailTemplateView()
        {
            foreach (var key in Request.QueryString.AllKeys)
            {
                ViewData[key] = Request.QueryString[key];
            }
            foreach (var key in Request.Form.AllKeys)
            {
                ViewData[key] = HttpUtility.UrlDecode(Request.Form[key]);
            }
            return View(EmailClient.EmailTemplateViewUrl);
        }
    }
}
