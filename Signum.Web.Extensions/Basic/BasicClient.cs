using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Services;
using System.Reflection;
using Signum.Entities.Extensions.Basics;
using Signum.Utilities.Reflection;

namespace Signum.Web.Processes
{
    public static class BasicClient
    {
        public static string ViewPrefix = "~/Plugin/Signum.Web.Extensions.dll/Signum.Web.Extensions.Basic.";

        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.AddSetting(new EntitySettings<DateSpanDN>(EntityType.Default) { PartialViewName = _ => ViewPrefix + "DateSpanIU.ascx" });
            }
        }
    }
}
