using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Reflection;
using Signum.Web;
using Signum.Utilities;
using $custommessage$.Entities;

namespace $custommessage$.Web
{
    public static class $custommessage$Client
    {
        public static string ViewPrefix = "~/Views/$custommessage$/{0}.cshtml";

        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<MyEntityDN>(EntityType.Default) { PartialViewName = e => ViewPrefix.Formato("MyEntity") },
                });
            }
        }
    }
}