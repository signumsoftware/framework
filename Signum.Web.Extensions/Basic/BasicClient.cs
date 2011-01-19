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
        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.RegisterArea(typeof(BasicClient));

                Navigator.AddSetting(new EmbeddedEntitySettings<DateSpanDN>() { PartialViewName = _ => RouteHelper.AreaView("DateSpan", "basic") });
            }
        }
    }
}
