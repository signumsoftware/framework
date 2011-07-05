using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Routing;
using System.Web.Mvc;
using System.Linq.Expressions;

namespace Signum.Web
{
    /// <summary>
    /// Route collection extension methods class.
    /// </summary>
    public static class RouteCollectionExtensions
    {

        public static void MapRouteSubdomain<TController>(
            this RouteCollection routes,
            string routeName,
            string url,
            Expression<Func<TController, ActionResult>> action)
            where TController : IController
        {
            if (routes == null)
            {
                throw new ArgumentNullException("routes");
            }

            var typedControllerAction = new ControllerAction<TController>(action);

            routes.Add(routeName, new RouteSubdomain(url, typedControllerAction.DefaultValues, null, new MvcRouteHandler()));
        }

        /// <summary>
        /// Adds a typed route into a RouteCollection.
        /// </summary>
        /// <typeparam name="TController">The controller type.</typeparam>
        /// <param name="routes">The route collection to fill in.</param>
        /// <param name="routeName">Name of the route.</param>
        /// <param name="url">The URL for the route.</param>
        /// <param name="action">The controller action.</param>
        public static void MapRoute<TController>(
            this RouteCollection routes,
            string routeName,
            string url,
            Expression<Func<TController, ActionResult>> action)
            where TController : IController
        {
            if (routes == null)
            {
                throw new ArgumentNullException("routes");
            }

            var typedControllerAction = new ControllerAction<TController>(action);

            routes.Add(routeName, new Route(url, typedControllerAction.DefaultValues, new MvcRouteHandler()));
        }

        /// <summary>
        /// Adds a typed route into a RouteCollection.
        /// </summary>
        /// <typeparam name="TController">The controller type.</typeparam>
        /// <param name="routes">The route collection to fill in.</param>
        /// <param name="routeName">Name of the route.</param>
        /// <param name="url">The URL for the route.</param>
        /// <param name="constraints">The constraints for the route.</param>
        /// <param name="action">The controller action.</param>
        public static void MapRoute<TController>(
            this RouteCollection routes,
            string routeName,
            string url,
            RouteValueDictionary constraints,
            Expression<Func<TController, ActionResult>> action)
            where TController : IController
        {
            if (routes == null)
            {
                throw new ArgumentNullException("routes");
            }

            var typedControllerAction = new ControllerAction<TController>(action);

            routes.Add(routeName, new Route(url, typedControllerAction.DefaultValues, constraints, new MvcRouteHandler()));
        }
    }

    ///<summary>
    /// Typed controller action that provides a <c ref="RouteValueDictionary">RouteValueDictionary</c> based on the parameters passed.
    /// </summary>
    /// <typeparam name="TController">The type of the controller.</typeparam>
    public class ControllerAction<TController>
        where TController : IController
    {
        /// <summary>
        /// Initializes a new instance of the ControllerAction class.
        /// </summary>
        /// <param name="action">The controller action.</param>
        public ControllerAction(Expression<Func<TController, ActionResult>> action)
        {
            this.DefaultValues = new RouteValueDictionary();
            this.DefaultValues.Add("controller", typeof(TController).Name.Remove(typeof(TController).Name.LastIndexOf("Controller")));

            var decorations = (action.Body as MethodCallExpression).Method.GetCustomAttributes(typeof(ActionNameAttribute), true);

            var methodCall = action.Body as MethodCallExpression;

            if (decorations != null && decorations.Length == 1)
            {
                this.DefaultValues.Add("action", (decorations[0] as ActionNameAttribute).Name);
            }
            else
            {
                this.DefaultValues.Add("action", methodCall.Method.Name);
            }

            var paremeters = methodCall.Method.GetParameters();

            for (int parameterIndex = 0; parameterIndex < paremeters.Length; parameterIndex++)
            {
                object value = null;
                var argumentExpression = methodCall.Arguments[parameterIndex];

                if (argumentExpression is ConstantExpression)
                {
                    value = (argumentExpression as ConstantExpression).Value;

                    this.DefaultValues.Add(
                        paremeters[parameterIndex].Name,
                        value);
                }
            }
        }

        /// <summary>
        /// Gets the default route values.
        /// </summary>
        /// <value>The default route values.</value>
        public RouteValueDictionary DefaultValues
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the controller name from the default values.
        /// </summary>
        /// <value>The controller name.</value>
        public string Controller
        {
            get
            {
                return this.DefaultValues["controller"] as string;
            }
        }

        /// <summary>
        /// Gets the controller action from the default values.
        /// </summary>
        /// <value>The controller action.</value>
        public string Action
        {
            get
            {
                return this.DefaultValues["action"] as string;
            }
        }
    }

    public class RouteSubdomain : Route
    {
        public RouteSubdomain(string url, RouteValueDictionary defaults, RouteValueDictionary constraints, IRouteHandler routeHandler) : base(url, defaults, constraints, routeHandler)
        {
        }

        public override RouteData GetRouteData(HttpContextBase httpContext)
        {
            RouteData rd = base.GetRouteData(httpContext);
            if (rd == null) return null;

            var url = httpContext.Request.Url.Host;
            bool hasSubdomain = url.Count(u=>u == '.') > 1;

            if (!hasSubdomain)
                return null;

            return rd;
        }
    }
}