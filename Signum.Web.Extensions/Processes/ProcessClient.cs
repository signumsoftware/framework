#region usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Entities.Authorization;
using Signum.Engine.Authorization;
using System.Reflection;
using Signum.Web.Operations;
using Signum.Entities;
using System.Web.Mvc;
using Signum.Web.Properties;
using System.Diagnostics;
using Signum.Engine;
using Signum.Entities.Basics;
using Signum.Entities.Reflection;
using Signum.Entities.Operations;
using System.Linq.Expressions;
using Signum.Engine.Maps;
using System.Web.Routing;
using Signum.Entities.Processes;
#endregion

namespace Signum.Web.Processes
{
    public static class ProcessClient
    {
        public static string ViewPrefix = "process/Views/";
         
        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                AssemblyResourceManager.RegisterAreaResources(
                    new AssemblyResourceStore(typeof(ProcessClient), "~/process/", "Signum.Web.Extensions.Processes."));
                
                RouteTable.Routes.InsertRouteAt0("process/{resourcesFolder}/{*resourceName}",
                    new { controller = "Resources", action = "Index", area = "process" },
                    new { resourcesFolder = new InArray(new string[] { "Scripts", "Content", "Images" }) });

                Navigator.AddSettings(new List<EntitySettings>{
                    new EntitySettings<ProcessExecutionDN>(EntityType.Default){ PartialViewName = e => ViewPrefix + "ProcessExecution", },
                    new EntitySettings<ProcessDN>(EntityType.Default){ PartialViewName = e => ViewPrefix + "Process"},
                    new EntitySettings<PackageDN>(EntityType.Default){ PartialViewName = e => ViewPrefix + "Package"},
                    new EntitySettings<PackageLineDN>(EntityType.Default){ PartialViewName = e => ViewPrefix + "PackageLine"},
               });
            }
        }
    }
}