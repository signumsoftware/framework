using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Signum.Entities.Reflection;
using Signum.Entities;
using Signum.Utilities;
using System.Text.RegularExpressions;
using Signum.Engine;
using System.Globalization;

namespace Signum.Web.Controllers
{
    public class LiteModelBinder : DefaultModelBinder
    {
        public override object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext) 
        {
            Type cleanType = Lite.Extract(bindingContext.ModelType);
             if (cleanType != null)
             {
                 string value = controllerContext.HttpContext.Request[bindingContext.ModelName];
                 if (value == null)
                 {
                     object routeValue = controllerContext.RouteData.Values[bindingContext.ModelName];
                     if (routeValue is Lite<IdentifiableEntity>)
                         return routeValue;
                     else if(routeValue is int)
                         return Lite.Create(cleanType, (int)routeValue);
                     else
                         value = (string)routeValue;
                 }
                 int id;
                 if (int.TryParse(value, out id))
                     return Lite.Create(cleanType, id);

                 return Lite.Parse(value);
             }
             return base.BindModel(controllerContext, bindingContext);
        }
    }

    public class CurrentCultureDateModelBinder : DefaultModelBinder
    {
        public override object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
            string value = controllerContext.HttpContext.Request[bindingContext.ModelName];
            
            DateTime dt = new DateTime();
            bool success = DateTime.TryParse(value, CultureInfo.CurrentUICulture, DateTimeStyles.None, out dt);
            if (success)
                return dt;
            else
                return base.BindModel(controllerContext, bindingContext);
        }
    }   
}
