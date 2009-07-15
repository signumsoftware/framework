using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace Signum.Web
{
    public class AppSettings
    {
        private static Dictionary<string, object> dictionary;
        public static AppSettings() {
            dictionary = new Dictionary<string, object>();
        }
        public static string Read(string key) {
            if (!dictionary.ContainsKey(key))
                dictionary.Add(key,ConfigurationSettings.AppSettings[key]);
            return dictionary[key];
        }

        public static bool ReadBoolean (string key, bool defaultValue){
            if (!dictionary.ContainsKey(key))
            {
                bool? value = ConfigurationSettings.AppSettings[key];
                dictionary.Add(key,(value == null) ? defaultValue : Convert.ToBoolean(value));
            }
            return (bool)dictionary[key];
        }
    }
}
