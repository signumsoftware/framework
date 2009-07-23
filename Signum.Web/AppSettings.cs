using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace Signum.Web
{
    public class AppSettings
    {
        public static string Read(string key)
        {
            return (String)ConfigurationManager.AppSettings[key];
        }

        public static bool ReadBoolean(string key, bool defaultValue)
        {
            string value = ConfigurationManager.AppSettings[key];
            return (value == null) ? defaultValue : value == "1";
        }
    }

    public static class AppSettingsKeys
    {
        public const string UseCaptcha = "sfUseCaptcha";    //"1" if we wish to use captcha
        public const string Development = "sfDevelopment";  //"1" if development scenario
        public const string MergeScriptsBottom = "sfMergeScriptsBottom"; //"1" if we want to merge the scripts included by controls and partial views merged at bottom
    }
}
