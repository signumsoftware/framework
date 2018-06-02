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


    public class SignumControllerFactory : IApplicationFeatureProvider<ControllerFeature>
    {
        public static HashSet<Type> AllowedControllers { get; private set; } = new HashSet<Type>();
        public static Dictionary<Assembly, List<string>> AllowedAreas { get; private set; } = new Dictionary<Assembly, List<string>>();
        public Assembly MainAssembly { get; set; }

        public SignumControllerFactory(Assembly mainAssembly) : base()
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

        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
        {
            var allowed = feature.Controllers.Where(ti => ti.Assembly == MainAssembly ||
            (AllowedAreas.TryGetC(ti.Assembly)?.Any(ns => ti.Namespace.StartsWith(ns)) ?? false) ||
            AllowedControllers.Contains(ti.AsType()));

            feature.Controllers.RemoveAll(ti => !allowed.Contains(ti));
        }
    }
}
