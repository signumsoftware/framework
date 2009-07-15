using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace Signum.Web
{
    public class AppSettings
    {
        static Dictionary<string, object> dictionary = new Dictionary<string, object>();
       /* static AppSettings() {
            dictionary = new Dictionary<string, object>();
        }*/
        public static string Read(string key) {
            if (!dictionary.ContainsKey(key))
                dictionary.Add(key,ConfigurationSettings.AppSettings[key]);
            return dictionary[key].ToString();
        }

        public static bool ReadBoolean (string key, bool defaultValue){
            if (!dictionary.ContainsKey(key))
            {
                string value = ConfigurationSettings.AppSettings[key];
                dictionary.Add(key,(value == null) ? defaultValue : value == "1");
            }
            return (bool)dictionary[key];
        }
    }

    public static class AppSettingsKeys
    {
        public const string UseCaptcha = "sfUseCaptcha";    //"1" if we wish to use captcha
        public const string Development = "sfDevelopment";  //"1" if development scenario
    }
}
