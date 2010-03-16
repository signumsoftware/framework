using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Signum.Entities.Reflection;
using Signum.Entities;
using Signum.Utilities;
using System.Text.RegularExpressions;

namespace Signum.Web.Controllers
{
    public class LiteModelBinder : DefaultModelBinder
    {
        public override object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext) 
        {
             Type cleanType = Reflector.ExtractLite(bindingContext.ModelType);
             if (cleanType != null)
             {
                 string value = controllerContext.HttpContext.Request[bindingContext.ModelName] ?? (string)controllerContext.RouteData.Values[bindingContext.ModelName];

                 int id;
                 if (int.TryParse(value, out id))
                     return Lite.Create(cleanType, id);

                 return ParseLite(cleanType, value);
             }
             return base.BindModel(controllerContext, bindingContext);
        }

        public static string WriteLite(Lite lite, bool forceRuntimeType)
        {
            if(lite == null)
                return null;

            if (lite.RuntimeType == Reflector.ExtractLite(lite.GetType()) && !forceRuntimeType)
                return lite.Id.ToString();

            return lite.Key(rt=>Navigator.TypesToNames.GetOrThrow(rt, "The type {0} is not registered in navigator"));
        }


        public static Lite ParseLite(Type staticType, string value)
        {
            if (string.IsNullOrEmpty(value))
                return null;

            return Lite.ParseLite(staticType, value, typeName => Navigator.NamesToTypes.GetOrThrow(typeName, "The name {0} does not correspond to any type in navigator"));
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
                .Select(t => Navigator.Manager.TypesToURLNames.TryGetC(t))
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
                return new ImplementedByAttribute(implementations.Split(';').Select(tn => Navigator.Manager.URLNamesToTypes.TryGetC(tn)).NotNull().ToArray());
        }
    }
}
