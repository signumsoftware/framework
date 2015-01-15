using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Services;
using System.Reflection;
using Signum.Utilities.Reflection;
using Signum.Utilities;
using Signum.Entities.Basics;

namespace Signum.Web.Cultures
{
    public static class CultureInfoClient
    {
        public static string ViewPrefix = "~/Cultures/Views/{0}.cshtml";

        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.RegisterArea(typeof(CultureInfoClient), "Cultures");

                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<CultureInfoEntity> { PartialViewName = _ => ViewPrefix.FormatWith("CultureInfoView") },
                });
            }
        }
    }
}
