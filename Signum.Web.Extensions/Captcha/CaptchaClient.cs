using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Reflection;

namespace Signum.Web.Captcha
{
    public static class CaptchaClient
    {
        public static string CaptchaUrl = RouteHelper.AreaView("captcha", "captcha");
        public static string CaptchaImageUrl = RouteHelper.AreaView("captchaImage", "captcha");

        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.RegisterArea(typeof(CaptchaClient));
            }
        }

        public static FontWarpFactor? fontWarpFactor { set { CaptchaImage.FontWarp = value; } }
        public static BackgroundNoiseLevel? backgroundNoiseLevel { set { CaptchaImage.BackgroundNoise = value; } }
        public static LineNoiseLevel? lineNoiseLevel { set { CaptchaImage.LineNoise = value; } }
        public static int? textLength { set { CaptchaImage.TextLength = value; } }
        public static string textChars { set { CaptchaImage.TextChars = value; } }
        public static double? cacheTimeOut { set { CaptchaImage.CacheTimeOut = value; } }
        public static string[] randomFontFamily { set { CaptchaImage.RandomFontFamily = value; } }
        public static Color[] randomColor { set { CaptchaImage.RandomColor = value; } }
    }
}
