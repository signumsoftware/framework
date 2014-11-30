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
                    new EntitySettings<DisconnectedMachineEntity> { PartialViewName = e => ViewPrefix.FormatWith("DisconnectedMachine") },
                    new EntitySettings<DisconnectedExportEntity> { PartialViewName = e => ViewPrefix.FormatWith("DisconnectedExport") },
                    new EntitySettings<DisconnectedImportEntity> { PartialViewName = e => ViewPrefix.FormatWith("DisconnectedImport") },
                });
            }
        }
    }
}
