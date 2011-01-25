using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Reflection;
using Signum.Utilities;

namespace Signum.Web.PortableAreas
{
    //It would be be better if it would never get registered, but this way is much less code and no risk of changing behaviour
    public class PortableAreaControllerFactory : DefaultControllerFactory
    {
        protected override Type GetControllerType(RequestContext requestContext, string controllerName)
        {
            Type controllerType = base.GetControllerType(requestContext, controllerName);

            string areaName;
            if (!PortableAreaControllers.IsAllowed(controllerType, out areaName))
                return null;

            if (areaName != null)
            {
                requestContext.RouteData.DataTokens["area"] = areaName;
            }

            return controllerType;
        }
    }
    
    public static class PortableAreaControllers
    {
        public static Dictionary<Type, string> AllowedTypes { get; private set;}
        public static Assembly MainAssembly { get; set; }

        static PortableAreaControllers()
        {
            AllowedTypes = new Dictionary<Type, string>(); 
        }

        internal static bool IsControllerType(Type t)
        {
            return t != null
                && t.IsPublic
                && t.Name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase)
                && !t.IsAbstract
                && typeof(IController).IsAssignableFrom(t);
        }

        public static void RegisterControllersIn(Assembly assembly, string @namespace, string areaName)
        {
            var types = assembly.GetTypes().Where(IsControllerType).Where(a => a.Namespace == @namespace || a.Namespace.StartsWith(@namespace + "."));

            AllowedTypes.AddRange(types, t => t, t => areaName, "controllers");
        }

        public static void RegisterControllersLike(Type clientClassType, string areaName)
        {
            RegisterControllersIn(clientClassType.Assembly, clientClassType.Namespace, areaName);
        }

        public static bool IsAllowed(Type type, out string areaName)
        {
            if (MainAssembly == null)
                throw new InvalidOperationException("PortableAreaControllers.MainAssembly is not set");

            areaName = null;

            if (type == null)
                return false;

            if (type.Assembly == MainAssembly)
                return true;

            return AllowedTypes.TryGetValue(type, out areaName);
        }
    }
}