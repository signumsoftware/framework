using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Reflection;
using Signum.Utilities;
using System.Linq.Expressions;
using System.Diagnostics;
using Signum.Web.Controllers;

namespace Signum.Web.PortableAreas
{
    public class SignumControllerFactory : DefaultControllerFactory
    {
        #region Portable Areas
        protected override Type GetControllerType(RequestContext requestContext, string controllerName)
        {
            Type controllerType = base.GetControllerType(requestContext, controllerName);

            string areaName;
            if (!IsAllowed(controllerType, out areaName))
                return null;

            if (areaName != null)
            {
                requestContext.RouteData.DataTokens["area"] = areaName;
            }

            return controllerType;
        }

        public static Dictionary<Type, string> AllowedTypes { get; private set; }
        public static Assembly MainAssembly { get; set; }

        static SignumControllerFactory()
        {
            AllowedTypes = new Dictionary<Type, string>();
        }

        static bool IsControllerType(Type t)
        {
            return t != null
                && t.IsPublic
                && t.Name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase)
                && !t.IsAbstract
                && typeof(IController).IsAssignableFrom(t);
        }

        public static void RegisterControllersIn(Assembly assembly, string @namespace, string areaName)
        {
            var types = assembly.GetTypes().Where(IsControllerType).Where(a => a.Namespace == @namespace || a.Namespace.StartsWith(@namespace + "."));

            AllowedTypes.AddRange(types, t => t, t => areaName, "controllers");
        }

        public static void RegisterControllersLike(Type clientClassType, string areaName)
        {
            RegisterControllersIn(clientClassType.Assembly, clientClassType.Namespace, areaName);
        }

        static bool IsAllowed(Type type, out string areaName)
        {
            if (MainAssembly == null)
                throw new InvalidOperationException("PortableAreaControllers.MainAssembly is not set");

            areaName = null;

            if (type == null)
                return false;

            if (type.Assembly == MainAssembly)
                return true;

            return AllowedTypes.TryGetValue(type, out areaName);
        } 
        #endregion

        [DebuggerStepThrough]
        public override IController CreateController(RequestContext requestContext, string controllerName)
        {
            var controller = base.CreateController(requestContext, controllerName);

            var controllerInstance = controller as Controller;

            if (controllerInstance != null)
                controllerInstance.ActionInvoker = new SignumActionInvoker();

            return controller;
        }

        public static Dictionary<Type, IFilterConfig> Config = new Dictionary<Type, IFilterConfig>();

        public static ControllerFilterConfig<T> Controller<T>() where T : Controller
        {
            return (ControllerFilterConfig<T>)Config.GetOrCreate(typeof(T), () => new ControllerFilterConfig<T>());
        }

        public static ControllerFilterConfig<Controller> EveryController()
        {
            return (ControllerFilterConfig<Controller>)Config.GetOrCreate(typeof(Controller), () => new ControllerFilterConfig<Controller>());
        }

        public static void AvoidValidate(Type[] doNotValidateInput)
        {

            ValidateInputAttribute doNotValidateInputAttribute = new ValidateInputAttribute(false);
            Controller<SignumController>()
                .Action(c => c.Validate())
                .AddFilters(ctx =>
                {
                    RuntimeInfo ri = RuntimeInfo.FromFormValue(ctx.ControllerContext.HttpContext.Request.Form[EntityBaseKeys.RuntimeInfo]);
                    if (doNotValidateInput.Contains(ri.EntityType))
                        return doNotValidateInputAttribute;
                    return null;
                });

            Controller<SignumController>()
                .Action(c => c.ValidatePartial(null, null))
                .AddFilters(ctx =>
                {
                    var form = ctx.ControllerContext.HttpContext.Request.Form;
                    var prefix = form["prefix"];

                    RuntimeInfo ri = RuntimeInfo.FromFormValue(form[TypeContextUtilities.Compose(prefix, EntityBaseKeys.RuntimeInfo)]);

                    if (doNotValidateInput.Contains(ri.EntityType))
                        return doNotValidateInputAttribute;

                    if (form.AllKeys.Contains(EntityBaseKeys.RuntimeInfo))
                    {
                        RuntimeInfo riBase = RuntimeInfo.FromFormValue(form[EntityBaseKeys.RuntimeInfo]);
                        if (doNotValidateInput.Contains(riBase.EntityType))
                            return doNotValidateInputAttribute;
                    }

                    return null;
                });

            Controller<OperationController>()
                 .Action(c => c.Execute(null, true, null, null))
                 .AddFilters(ctx =>
                 {
                     var form = ctx.ControllerContext.HttpContext.Request.Form;
                     var prefix = form["prefix"];

                     RuntimeInfo ri = RuntimeInfo.FromFormValue(form[TypeContextUtilities.Compose(prefix, EntityBaseKeys.RuntimeInfo)]);
                     if (doNotValidateInput.Contains(ri.EntityType))
                         return doNotValidateInputAttribute;
                     return null;
                 });
        }
    }

    class SignumActionInvoker : ControllerActionInvoker
    {
        protected override FilterInfo GetFilters(ControllerContext controllerContext, ActionDescriptor actionDescriptor)
        {
            var filters = base.GetFilters(controllerContext, actionDescriptor);

            IFilterConfig defaultConfig = SignumControllerFactory.Config.TryGetC(typeof(Controller));
            if (defaultConfig != null)
                defaultConfig.Configure(filters, controllerContext, actionDescriptor);

            IFilterConfig config = SignumControllerFactory.Config.TryGetC(controllerContext.Controller.GetType());
            if (config != null)
                config.Configure(filters, controllerContext, actionDescriptor);

            return filters;
        }
    }

    public interface IFilterConfig
    {
        void Configure(FilterInfo filterInfo, ControllerContext controllerContext, ActionDescriptor actionDescriptor);
    }
}