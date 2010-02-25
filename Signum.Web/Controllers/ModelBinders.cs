using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Signum.Entities.Reflection;
using Signum.Entities;
using Signum.Utilities;

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

                 if (string.IsNullOrEmpty(value))
                     return null;

                 int separatorIndex = value.IndexOf(";");
                 string id = (separatorIndex < 0) ? value : value.Substring(separatorIndex + 1, value.Length - separatorIndex - 1);

                 if (string.IsNullOrEmpty(id))
                     return null;

                 return Lite.Create(cleanType, int.Parse(id));
             }
             return base.BindModel(controllerContext, bindingContext);
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
