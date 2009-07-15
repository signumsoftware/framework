using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Signum.Web;
using Signum.Entities;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using System.Reflection;
using System.Text;
using System.Configuration;
using System.Web.Caching;

namespace Signum.Web.Captcha
{
    public static class CaptchaButtonLineHelper
    {
        public static string CaptchaImage(this HtmlHelper helper, int height, int width,string route)
        {
            CaptchaImage image = new CaptchaImage
            {
                Height = height,
                Width = width,                
            };

            HttpRuntime.Cache.Add(
                image.UniqueId,
                image,
                null,
                DateTime.Now.AddSeconds(Signum.Web.Captcha.CaptchaImage.CacheTimeOut.Value),
                Cache.NoSlidingExpiration,
                CacheItemPriority.NotRemovable,
                null);

            StringBuilder stringBuilder = new StringBuilder(256);
            stringBuilder.Append("<input type=\"hidden\" name=\"captcha-guid\" value=\"");
            stringBuilder.Append(image.UniqueId);
            stringBuilder.Append("\" />");
            stringBuilder.AppendLine();
            stringBuilder.Append("<img src=\"");
            //stringBuilder.Append(/*HttpContext.Current.Server.MapPath("~")+*/ConfigurationManager.AppSettings["RoutePrefix"] + "/View/captcha.ashx?guid=" + image.UniqueId);
            stringBuilder.Append(route + VirtualPathUtility.ToAbsolute("~/" + "Captcha.aspx/Image") + "?guid=" + image.UniqueId);
            stringBuilder.Append("\" alt=\"CAPTCHA\" width=\"");
            stringBuilder.Append(width);
            stringBuilder.Append("\" height=\"");
            stringBuilder.Append(height);
            stringBuilder.Append("\" />");

            return stringBuilder.ToString();
        }
    }
}
