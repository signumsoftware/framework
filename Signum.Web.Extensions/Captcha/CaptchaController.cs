using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Drawing;
using System.Web;
using System.Drawing.Imaging;
using Signum.Web.Captcha;

namespace Signum.Web.Controllers
{
    public class CaptchaController : Controller
    {
        [AcceptVerbs(HttpVerbs.Get)]
        public void Image(string guid)
        {
            HttpContextBase context=ControllerContext.RequestContext.HttpContext;
            CaptchaImage ci = CaptchaImage.GetCachedCaptcha(guid);

            if (String.IsNullOrEmpty(guid) || ci == null)
            {
                context.Response.StatusCode = 404;
                context.Response.StatusDescription = "Not Found";
                context.ApplicationInstance.CompleteRequest();
                return;
            }

            // write the image to the HTTP output stream as an array of bytes
            using (Bitmap b = ci.RenderImage())
            {
                b.Save(context.Response.OutputStream, ImageFormat.Gif);
            }

            context.Response.ContentType = "image/png";
            context.Response.StatusCode = 200;
            context.Response.StatusDescription = "OK";
            context.ApplicationInstance.CompleteRequest();
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public PartialViewResult Refresh()
        {
            //this.ViewData.Add(ViewDataKeys.ResourcesRoute, ConfigurationManager.AppSettings["RutaResources"]);
            return new PartialViewResult()
            {
                ViewData = this.ViewData,
                TempData = this.TempData,
                ViewName = "captcha"
            };
        }
    }
}
