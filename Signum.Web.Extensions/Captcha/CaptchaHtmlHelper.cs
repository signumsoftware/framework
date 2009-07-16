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
using Signum.Utilities;

namespace Signum.Web.Captcha
{
    public static class CaptchaButtonLineHelper
    {
        public static string CaptchaImage(this HtmlHelper helper, int height, int width, string route, Dictionary<string, object> htmlAttributes)
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

            return "<input type=\"hidden\" name=\"captcha-guid\" value=\"{0}\" />\n".Formato(image.UniqueId) +
                   "<img src=\"{0}\" alt=\"CAPTCHA\" width=\"{1}\" height=\"{2}\" {3}/>".Formato(
                       route + VirtualPathUtility.ToAbsolute("~/Captcha.ascx/Image") + "?guid=" + image.UniqueId,
                       width,
                       height,
                       htmlAttributes.ToString(kv => kv.Key + "=" + kv.Value.ToString().Quote(), " ")
                       );
        }
    }
}
