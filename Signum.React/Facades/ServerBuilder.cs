using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Signum.React.PortableAreas;
using Signum.Utilities;

namespace Signum.React
{
    public class ServerBuilder
    {
        public ServerBuilder()
        {
            RegisterArea(typeof(ServerBuilder), "signum");
        }

        public static void RegisterArea(Type clientType,
            string areaName = null,
            string controllerNamespace = null,
            string resourcesNamespace = null)
        {
            if (areaName == null)
                areaName = clientType.Namespace.AfterLast('.');

            if (areaName.Start(1) == "/")
                throw new SystemException("Invalid start character / in {0}".FormatWith(areaName));

            if (controllerNamespace == null)
                controllerNamespace = clientType.Namespace;

            if (resourcesNamespace == null)
                resourcesNamespace = clientType.Namespace;

            var assembly = clientType.Assembly;
            
            //SignumControllerFactory.RegisterControllersIn(assembly, controllerNamespace, areaName);

            EmbeddedFilesRepository rep = new EmbeddedFilesRepository(assembly, "~/" + areaName + "/", resourcesNamespace);
            if (!rep.IsEmpty)
                FileRepositoryManager.Register(rep);
        }

    }
}