using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Signum.React.Facades;
using Signum.React.PortableAreas;
using Signum.Utilities;

namespace Signum.React
{
    public static class AssemblyAreas
    {
        public static void RegisterFrameworkArea()
        {
            RegisterArea(typeof(AssemblyAreas), "signum");
            ReflectionCache.Start();
        }

        public static void RegisterArea(Type controllerType,
            string areaName = null,
            string controllerNamespace = null,
            string resourcesNamespace = null)
        {
            if (areaName == null)
                areaName = controllerType.Namespace.AfterLast('.');

            if (areaName.Start(1) == "/")
                throw new SystemException("Invalid start character / in {0}".FormatWith(areaName));

            if (controllerNamespace == null)
                controllerNamespace = controllerType.Namespace;

            if (resourcesNamespace == null)
                resourcesNamespace = controllerType.Namespace;

            var assembly = controllerType.Assembly;
            
            //SignumControllerFactory.RegisterControllersIn(assembly, controllerNamespace, areaName);

            EmbeddedFilesRepository rep = new EmbeddedFilesRepository(assembly, "~/" + areaName + "/", resourcesNamespace);
            if (!rep.IsEmpty)
                FileRepositoryManager.Register(rep);
        }

    }
}