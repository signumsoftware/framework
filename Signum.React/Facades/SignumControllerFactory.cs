using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Signum.React.Facades;
using Signum.Utilities;
using System.Reflection;
using Signum.React.ApiControllers;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Signum.React
{
  

    public class SignumControllerFactory // : DefaultHttpControllerSelector
    {
        public static HashSet<Type> AllowedControllers { get; private set; } = new HashSet<Type>();
        public Assembly MainAssembly { get; set; }

        //public SignumControllerFactory(IApplicationBuilder appuration, Assembly mainAssembly) : base(configuration)
        //{
        //    this.MainAssembly = mainAssembly;
        //}

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

        //public override IDictionary<string, HttpControllerDescriptor> GetControllerMapping()
        //{
        //    var dic = base.GetControllerMapping();

        //    var result = dic.Where(a => a.Value.ControllerType.Assembly == MainAssembly || AllowedControllers.Contains(a.Value.ControllerType)).ToDictionary();

        //    var removedControllers = dic.Keys.Except(result.Keys);//Just for debugging

        //    return result;
        //}
    }
}
