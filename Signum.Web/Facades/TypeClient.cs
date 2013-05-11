using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Reflection;
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
using Signum.Engine.Basics;

namespace Signum.Web
{
    public static class TypeClient
    {
        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.AddSetting(new EntitySettings<TypeDN>() { PartialViewName = e => NavigationManager.ViewPrefix.Formato("TypeView") });
            }
        }

        public static IEnumerable<TypeDN> ViewableServerTypes()
        {
            return from t in Navigator.Manager.EntitySettings.Keys
                   where Navigator.IsViewable(t)
                   select TypeLogic.TypeToDN.TryGetC(t) into tdn
                   where tdn != null
                   select tdn;
        }

        public static IEnumerable<Lite<TypeDN>> ViewableServerTypes(string text)
        {
            return from t in Navigator.Manager.EntitySettings.Keys
                   where Navigator.IsViewable(t) && t.Name.Contains(text, StringComparison.InvariantCultureIgnoreCase) || t.NiceName().Contains(text, StringComparison.InvariantCultureIgnoreCase)
                   select TypeLogic.TypeToDN.TryGetC(t) into tdn
                   where tdn != null
                   select tdn.ToLite();
        }
    }
}
