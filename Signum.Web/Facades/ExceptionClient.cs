using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Reflection;
using Signum.Entities;
using System.Web.Mvc;
using Signum.Web.Properties;
using System.Diagnostics;
using Signum.Engine;
using Signum.Entities.Basics;
using Signum.Entities.Reflection;
using System.Linq.Expressions;
using Signum.Engine.Maps;
using System.Web.Routing;
using System.Web.Mvc.Html;

namespace Signum.Web.Exceptions
{
    public static class ExceptionClient
    {
        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.AddSetting(new EntitySettings<ExceptionDN>() { PartialViewName = e => NavigationManager.ViewPrefix.Formato("Exception") });
            }
        }
    }
}
