using System;
using System.Text;
using System.IO;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using System.Linq.Expressions;
using System.Linq;
using System.Web.Routing;
using System.Reflection;
using System.Collections.Generic;
using Signum.Utilities;
using System.Web.WebPages;
using Signum.Entities;
using Signum.Entities.Reflection;
using Signum.Engine;

namespace Signum.Web
{
    public static class RouteHelper
    {
        public static UrlHelper New()
        {
            return new UrlHelper(HttpContext.Current.Request.RequestContext);
        }
    }

    public static class GenericRouteHelper
    {
        public static MvcHtmlString Action<TController>(this HtmlHelper helper, Expression<Action<TController>> action)
            where TController : Controller
        {
            RouteValueDictionary rvd = ExpressionHelper.GetRouteValuesFromExpression(action);
            return helper.Action((string)rvd["Action"], (string)rvd["Controller"], rvd);
        }

     
        public static void RenderAction<TController>(this HtmlHelper helper, Expression<Action<TController>> action) 
            where TController : Controller
        {
            RouteValueDictionary rvd = ExpressionHelper.GetRouteValuesFromExpression(action);
            helper.RenderAction((string)rvd["Action"], (string)rvd["Controller"], rvd);
        }


        public static MvcForm BeginForm<TController>(this HtmlHelper htmlHelper, Expression<Action<TController>> action)
            where TController : Controller
        {
            return htmlHelper.BeginForm(action, FormMethod.Post, null); 
        }

        public static MvcForm BeginForm<TController>(this HtmlHelper htmlHelper, Expression<Action<TController>> action, FormMethod method)
           where TController : Controller
        {
             return htmlHelper.BeginForm(action, method, null); 
        }


        public static MvcForm BeginForm<TController>(this HtmlHelper htmlHelper, Expression<Action<TController>> action, object htmlAttributes)
          where TController : Controller
        {
             return htmlHelper.BeginForm(action, FormMethod.Post, new RouteValueDictionary(htmlAttributes)); 
        }

        public static MvcForm BeginForm<TController>(this HtmlHelper htmlHelper, Expression<Action<TController>> action, FormMethod method, object htmlAttributes)
          where TController : Controller
        {
           return htmlHelper.BeginForm(action, method, new RouteValueDictionary(htmlAttributes)); 
        }


        public static MvcForm BeginForm<TController>(this HtmlHelper htmlHelper, Expression<Action<TController>> action, IDictionary<string, object> htmlAttributes)
            where TController : Controller
        {
            return htmlHelper.BeginForm(action, FormMethod.Post, htmlAttributes); 
        }

        public static MvcForm BeginForm<TController>(this HtmlHelper htmlHelper, Expression<Action<TController>> action, FormMethod method, IDictionary<string, object> htmlAttributes)
           where TController : Controller
        {
            RouteValueDictionary rvd = ExpressionHelper.GetRouteValuesFromExpression(action);
            return htmlHelper.BeginForm(null, null, rvd, FormMethod.Post, htmlAttributes);
        }


        public static MvcHtmlString ActionLink<TController>(this HtmlHelper htmlHelper, string linkText, Expression<Action<TController>> action)
            where TController : Controller
        {
            return htmlHelper.ActionLink(linkText, action, new RouteValueDictionary());
        }

        public static MvcHtmlString ActionLink<TController>(this HtmlHelper htmlHelper, string linkText, Expression<Action<TController>> action, object htmlAttributes)
            where TController : Controller
        {
            return htmlHelper.ActionLink(linkText, action, new RouteValueDictionary(htmlAttributes));
        }

        public static MvcHtmlString ActionLink<TController>(this HtmlHelper htmlHelper, string linkText, Expression<Action<TController>> action, IDictionary<string, object> htmlAttributes)
           where TController : Controller
        {
            RouteValueDictionary rvd = ExpressionHelper.GetRouteValuesFromExpression(action);
            return htmlHelper.ActionLink(linkText, null, null, rvd, htmlAttributes);
        }
    }

    public static class UrlGenericExtensions
    {
        public static string SignumAction(this UrlHelper helper, string actionName)
        {
            return helper.Action(actionName, "Signum");
        }

        public static string Action<TController>(this UrlHelper helper, Expression<Action<TController>> action)
           where TController : Controller
        {
            RouteValueDictionary rvd = ExpressionHelper.GetRouteValuesFromExpression(action);
            return helper.Action(null, null, rvd);
        }
    }

    public static class ExpressionHelper
    {
        public static RouteValueDictionary GetRouteValuesFromExpression<TController>(Expression<Action<TController>> action) where TController : Controller
        {
            if (action == null)
            {
                throw new ArgumentNullException("action");
            }

            MethodCallExpression call = action.Body as MethodCallExpression;
            if (call == null)
            {
                throw new ArgumentException("Action must be a method call", "action");
            }

            string controllerName = typeof(TController).Name;
            if (!controllerName.EndsWith("Controller", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Action target must end in controller", "action");
            }
            controllerName = controllerName.Substring(0, controllerName.Length - "Controller".Length);
            if (controllerName.Length == 0)
            {
                throw new ArgumentException("Action cannot route to controller", "action");
            }

            // TODO: How do we know that this method is even web callable?
            //      For now, we just let the call itself throw an exception.

            var rvd = new RouteValueDictionary();
            rvd.Add("Controller", controllerName);
            rvd.Add("Action", call.Method.Name);
            AddParameterValuesFromExpressionToDictionary(rvd, call);
            return rvd;
        }

        public static List<IParameterConverter> ParameterConverters = new List<IParameterConverter> { new LiteConverter() };

        static void AddParameterValuesFromExpressionToDictionary(RouteValueDictionary rvd, MethodCallExpression call)
        {
            ParameterInfo[] parameters = call.Method.GetParameters();

            if (parameters.Length > 0)
            {
                for (int i = 0; i < parameters.Length; i++)
                {
                    Expression arg = call.Arguments[i];
                    object value = null;
                    ConstantExpression ce = arg as ConstantExpression;
                    if (ce != null)
                    {
                        // If argument is a constant expression, just get the value
                        value = ce.Value;
                    }
                    else
                    {
                        // Otherwise, convert the argument subexpression to type object,
                        // make a lambda out of it, compile it, and invoke it to get the value
                        Expression<Func<object>> lambdaExpression = Expression.Lambda<Func<object>>(Expression.Convert(arg, typeof(object)));
                        Func<object> func = lambdaExpression.Compile();
                        value = func();
                    }

                   
                    var conv = ParameterConverters.FirstOrDefault(c => c.CanConvert(value));
                    if (conv != null)
                        value = conv.Convert(value, parameters[i].ParameterType);
                   
                    rvd.Add(parameters[i].Name, value);
                }
            }
        }
    }

    public interface IParameterConverter
    {
        bool CanConvert(object obj);
        object Convert(object obj, Type parameterType);
    }

    public class LiteConverter : IParameterConverter
    {
        public bool CanConvert(object obj)
        {
            return obj is Lite<IIdentifiable>;
        }

        public object Convert(object obj, Type parameterType)
        {
            Lite<IIdentifiable> lite = (Lite<IIdentifiable>)obj;
            if (Lite.Extract(parameterType) == lite.EntityType)
                return lite.Id;
            else
                return lite.Key();
        }
    }
}
