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
using Signum.Entities.Deployment;

namespace Signum.Web.Deployment
{
    public static class DeploymentClient
    {
        public static string ViewPrefix = "~/Deployment/Views/{0}.cshtml";

        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.RegisterArea(typeof(DeploymentClient));

                Navigator.AddSetting(new EntitySettings<DeploymentLogDN>(EntityType.System) { PartialViewName = e => ViewPrefix.Formato("DeploymentLog") });
            }
        }
    }
}
