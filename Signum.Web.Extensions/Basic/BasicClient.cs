using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Services;
using System.Reflection;
using Signum.Entities.Extensions.Basics;
using Signum.Utilities.Reflection;

namespace Signum.Web.Basic
{
    public static class BasicClient
    {
        public static string ViewPrefix = "basic/Views/";

        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                AssemblyResourceManager.RegisterAreaResources(
                    new AssemblyResourceStore(typeof(BasicClient), "~/basic/", "Signum.Web.Extensions.Basic."));

                Navigator.AddSetting(new EntitySettings<DateSpanDN>(EntityType.Default) { PartialViewName = _ => ViewPrefix + "DateSpanIU" });
            }
        }
    }
}
