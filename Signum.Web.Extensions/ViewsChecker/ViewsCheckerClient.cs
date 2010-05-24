using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Services;
using System.Reflection;
using Signum.Entities.Extensions.Basics;
using Signum.Utilities.Reflection;

namespace Signum.Web.ViewsChecker
{
    public static class ViewsCheckerClient
    {
        public static string ViewPrefix = "viewsChecker/Views/";

        public static void Start()
        {
            #if (DEBUG)
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                AssemblyResourceManager.RegisterAreaResources(
                    new AssemblyResourceStore(typeof(ViewsCheckerClient), "/viewsChecker/", "Signum.Web.Extensions.ViewsChecker."));
            }
            #endif
        }
    }
}
