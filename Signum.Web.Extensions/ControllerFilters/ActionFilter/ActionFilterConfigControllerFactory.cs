using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web.Routing;

namespace Signum.Web
{
    public class ActionFilterConfigControllerFactory : DefaultControllerFactory
    {
        public override IController CreateController(RequestContext requestContext, string controllerName)
        {
            var controller = base.CreateController(requestContext, controllerName);

            var controllerInstance = controller as Controller;

            if (controllerInstance != null)
                controllerInstance.ActionInvoker = new ActionFilerConfigControllerActionInvoker();

            return controller;
        }
    }
}
