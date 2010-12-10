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
using Signum.Web.Properties;

namespace Signum.Web.Controllers
{
    public class LiteModelBinder : DefaultModelBinder
    {
        public override object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext) 
        {
             Type cleanType = Reflector.ExtractLite(bindingContext.ModelType);
             if (cleanType != null)
             {
                 string value = controllerContext.HttpContext.Request[bindingContext.ModelName];
                 if (value == null)
                 {
                     if (controllerContext.RouteData.Values[bindingContext.ModelName] is Lite)
                         return controllerContext.RouteData.Values[bindingContext.ModelName];
                     else
                         value = (string)controllerContext.RouteData.Values[bindingContext.ModelName];
                 }
                 int id;
                 if (int.TryParse(value, out id))
                     return Lite.Create(cleanType, id);

                 return TypeLogic.ParseLite(cleanType, value);
             }
             return base.BindModel(controllerContext, bindingContext);
        }
    }

    public class DateModelBinder : DefaultModelBinder
    {
        public override object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
            string value = controllerContext.HttpContext.Request[bindingContext.ModelName];
            Match m = Regex.Match(value, @"^(?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2})$");

            if (m == null) return base.BindModel(controllerContext, bindingContext);

            return new DateTime(
                int.Parse(m.Groups["year"].Value),
                int.Parse(m.Groups["month"].Value),
                int.Parse(m.Groups["day"].Value),
                0,0,0,                    
                TimeZoneManager.Mode == TimeZoneMode.Local ? DateTimeKind.Local : DateTimeKind.Utc);
        }
    }


    public class ImplementationsModelBinder : IModelBinder
    {
        public object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
            string value = controllerContext.HttpContext.Request[bindingContext.ModelName];

            return Parse(value); 
        }

        public static string Render(Implementations implementations)
        {
            if (implementations == null)
                return "";

            if (implementations.IsByAll)
                return "[All]";

            ImplementedByAttribute ib = (ImplementedByAttribute)implementations;
            return ib.ImplementedTypes
                .Select(t => Navigator.Manager.TypesToNames.TryGetC(t))
                .NotNull()
                .ToString(";");
        }

        public static Implementations Parse(string implementations)
        {
            if (string.IsNullOrEmpty(implementations))
                return null;
            if (implementations == "[All]")
                return new ImplementedByAllAttribute();
            else
                return new ImplementedByAttribute(implementations.Split(';').Select(tn => Navigator.Manager.NamesToTypes.TryGetC(tn)).NotNull().ToArray());
        }
    }
}
