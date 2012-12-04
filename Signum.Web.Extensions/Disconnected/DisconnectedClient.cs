using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Entities.Authorization;
using Signum.Engine.Authorization;
using System.Reflection;
using Signum.Entities;
using System.Web.Mvc;
using Signum.Web.Properties;
using System.Diagnostics;
using Signum.Engine;
using Signum.Entities.Basics;
using Signum.Entities.Reflection;
using System.Linq.Expressions;
using Signum.Engine.Maps;
using System.Web.Routing;
using System.Web.Mvc.Html;
using Signum.Entities.Disconnected;

namespace Signum.Web.Disconnected
{
    public static class DisconnectedClient
    {
        public static string ViewPrefix = "~/Disconnected/Views/{0}.cshtml";

        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.RegisterArea(typeof(DisconnectedClient));

                Navigator.AddSettings(new List<EntitySettings>()
                {
                    new EntitySettings<DisconnectedMachineDN>(EntityType.Main) { PartialViewName = e => ViewPrefix.Formato("DisconnectedMachine") },
                    new EntitySettings<DisconnectedExportDN>(EntityType.System) { PartialViewName = e => ViewPrefix.Formato("DisconnectedExport") },
                    new EntitySettings<DisconnectedImportDN>(EntityType.System) { PartialViewName = e => ViewPrefix.Formato("DisconnectedImport") },
                });
            }
        }
    }
}
