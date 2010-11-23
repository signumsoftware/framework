using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Routing;
using System.Web.Mvc;

namespace Signum.Web
{
    public class InArray: IRouteConstraint
    {
        private string[] matchValues;

        public InArray(string[] matchValues)
        {
            this.matchValues = matchValues.Select(s => s.ToLowerInvariant()).ToArray();
        }

        public bool Match(HttpContextBase httpContext, Route route, string parameterName, RouteValueDictionary values, RouteDirection routeDirection)
        {
            return matchValues.Contains(values[parameterName].ToString().ToLowerInvariant());
        }
    }

    public static class RouteExtensions
    {
        public static Route InsertRouteAt0(this RouteCollection routes, string url)
        {
            return routes.InsertRouteAt0(url, null, null);
        }

        public static Route InsertRouteAt0(this RouteCollection routes, string url, object defaults)
        {
            return routes.InsertRouteAt0(url, defaults, null);
        }

        public static Route InsertRouteAt0(this RouteCollection routes, string url, object defaults, object constraints)
        {
            return routes.InsertRouteAt0(url, defaults, constraints, null);
        }

        public static Route InsertRouteAt0(this RouteCollection routes, string url, object defaults, object constraints, string[] namespaces)
        {
            if (routes == null)
                throw new ArgumentNullException("routes");
            
            if (url == null)
                throw new ArgumentNullException("url");
            
            Route route2 = new Route(url, new MvcRouteHandler());
            route2.Defaults = new RouteValueDictionary(defaults);
            route2.Constraints = new RouteValueDictionary(constraints);
            route2.DataTokens = new RouteValueDictionary();
            Route item = route2;
            if ((namespaces != null) && (namespaces.Length > 0))
                item.DataTokens["Namespaces"] = namespaces;
            
            routes.Insert(0, item);
            return item;
        }
    }
}
