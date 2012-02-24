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
    public static class ProcessesClient
    {
        public static string ViewPrefix = "~/processes/Views/{0}.cshtml";

        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.RegisterArea(typeof(ProcessesClient));

                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<ProcessExecutionDN>(EntityType.NotSaving){ PartialViewName = e => ViewPrefix.Formato("ProcessExecution"), },
                    new EntitySettings<ProcessDN>(EntityType.Default){ PartialViewName = e => ViewPrefix.Formato("Process")},
                    new EntitySettings<PackageDN>(EntityType.Default){ PartialViewName = e => ViewPrefix.Formato("Package")},
                    new EntitySettings<PackageLineDN>(EntityType.Default){ PartialViewName = e => ViewPrefix.Formato("PackageLine")},
                });
            }
        }

       
    }
}