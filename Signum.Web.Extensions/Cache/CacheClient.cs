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
using System.Diagnostics;
using Signum.Engine;
using Signum.Entities.Basics;
using Signum.Entities.Reflection;
using System.Linq.Expressions;
using Signum.Engine.Maps;
using System.Web.Routing;
using System.Web.Mvc.Html;
using Signum.Web.Omnibox;
using Signum.Entities.Cache;

namespace Signum.Web.Cache
{
    public static class CacheClient
    {
        public static string ViewPrefix = "~/Cache/Views/{0}.cshtml";
        public static JsModule Model = new JsModule("Extensions/Signum.Web.Extensions/Cache/Scripts/Cache");

        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.RegisterArea(typeof(CacheClient));

                SpecialOmniboxProvider.Register(new SpecialOmniboxAction("ViewCache",
                    () => CachePermission.ViewCache.IsAuthorized(),
                    uh => uh.Action((CacheController cc) => cc.View())));
            }
        }
    }
}
