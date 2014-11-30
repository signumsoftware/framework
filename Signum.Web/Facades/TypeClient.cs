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
                Navigator.AddSetting(new EntitySettings<TypeEntity>() { PartialViewName = e => NavigationManager.ViewPrefix.FormatWith("TypeView") });
            }
        }

        public static IEnumerable<TypeEntity> ViewableServerTypes()
        {
            return from t in Navigator.Manager.EntitySettings.Keys
                   let tdn = TypeLogic.TypeToEntity.TryGetC(t)
                   where tdn != null && Navigator.IsViewable(t, null)
                   select tdn;
        }
    }
}
