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
using System.Web.Mvc.Html;
using Signum.Entities.Logging;

namespace Signum.Web.Logging
{
    public static class LoggingClient
    {
        public static string ViewPrefix = "~/Logging/Views/{0}.cshtml";

        public static void Start(bool deployment, bool exceptions)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.RegisterArea(typeof(LoggingClient));

                if (deployment)
                    Navigator.AddSetting(new EntitySettings<ExceptionDN>(EntityType.ServerOnly) 
                    { PartialViewName = e => ViewPrefix.Formato("ExceptionLog") });
                if (exceptions)
                    Navigator.AddSetting(new EntitySettings<DeploymentLogDN>(EntityType.ServerOnly) 
                    { PartialViewName = e => ViewPrefix.Formato("DeploymentLog") });
            }
        }
    }
}
