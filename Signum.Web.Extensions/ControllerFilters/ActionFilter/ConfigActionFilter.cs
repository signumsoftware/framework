using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;

namespace Signum.Web
{
    public static class ConfigActionFilter
    {
        static Dictionary<string, IActionFilterConfig> configDic = new Dictionary<string, IActionFilterConfig>();

        public static ActionFilterConfig<T> ConfigController<T>() where T : Controller
        {
            var actionFilterConfig = new ActionFilterConfig<T>();

            var controllerType = typeof(T);
            configDic.Add(controllerType.Name, actionFilterConfig);

            return actionFilterConfig;
        }


        public static Dictionary<string, IActionFilterConfig> Config
        {
            get { return configDic; }
        }
    }
}
