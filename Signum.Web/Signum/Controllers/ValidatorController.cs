using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Signum.Engine.Basics;
using Signum.Entities;
using Signum.Utilities;

namespace Signum.Web.Controllers
{
    public class ValidatorController : Controller
    {
        [HttpPost, ActionSplitter("rootType")]
        public JsonNetResult Validate(string rootType = null, string propertyRoute = null)
        {
            ModifiableEntity mod = this.UntypedExtractEntity();
            
            PropertyRoute route = (rootType.HasText() || propertyRoute.HasText()) ? PropertyRoute.Parse(TypeLogic.GetType(rootType), propertyRoute) : PropertyRoute.Root(mod.GetType());

            MappingContext context = mod.UntypedApplyChanges(this, route: route).UntypedValidateGlobal();

            IIdentifiable ident = context.UntypedValue as IIdentifiable;
            string newLink = ident != null && ident.IdOrNull != null ? Navigator.NavigateRoute(ident) : null;
            string newToStr = context.UntypedValue.ToString();

            return context.ToJsonModelState(newToStr, newLink);
        }
    }
}
