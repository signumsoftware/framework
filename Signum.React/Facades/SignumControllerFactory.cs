using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Signum.React.Facades;
using Signum.Utilities;
using System.Web.Http.Dispatcher;
using System.Web.Http.Controllers;
using System.Net.Http;
using System.Web.Http;
using System.Reflection;
using Signum.React.ApiControllers;

namespace Signum.React
{
    public class SignumControllerFactory : DefaultHttpControllerSelector
    {
        public static HashSet<Type> AllowedControllers { get; private set; } = new HashSet<Type>();
        public Assembly MainAssembly { get; set; }

        public SignumControllerFactory(HttpConfiguration configuration, Assembly mainAssembly) : base(configuration)
        {
            this.MainAssembly = mainAssembly;
        }

        public static void RegisterController<T>()
        {
            AllowedControllers.Add(typeof(T));
        }

        public static void RegisterArea(MethodBase mb)
        {
            RegisterArea(mb.DeclaringType);
        }

        public static void RegisterArea(Type type)
        {
            AllowedControllers.AddRange(type.Assembly.ExportedTypes
                .Where(c => (c.Namespace ?? "").StartsWith(type.Namespace) && typeof(ApiController).IsAssignableFrom(c)));
        }

        public override IDictionary<string, HttpControllerDescriptor> GetControllerMapping()
        {
            var dic = base.GetControllerMapping();

            var result = dic.Where(a => a.Value.ControllerType.Assembly == MainAssembly || AllowedControllers.Contains(a.Value.ControllerType)).ToDictionary();

            var removedControllers = dic.Keys.Except(result.Keys);//Just for debugging

            return result;
        }
    }
}
