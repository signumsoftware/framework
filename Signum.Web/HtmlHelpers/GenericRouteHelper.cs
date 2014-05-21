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
using Signum.Utilities.ExpressionTrees;
using System.Collections.Concurrent;

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
            return htmlHelper.BeginForm(null, null, rvd, method, htmlAttributes);
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
        public static string Action<TController>(this UrlHelper helper, Expression<Action<TController>> action)
           where TController : Controller
        {
            using (var a = HeavyProfiler.LogNoStackTrace("GetRouteValuesFromExpression"))
            {
                RouteValueDictionary rvd = ExpressionHelper.GetRouteValuesFromExpression(action);
                a.Switch("Action");
                return helper.Action(null, null, rvd);
            }
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
            controllerName = controllerName.RemoveEnd("Controller".Length);


            // TODO: How do we know that this method is even web callable?
            //      For now, we just let the call itself throw an exception.

            var rvd = new RouteValueDictionary();
            rvd.Add("Controller", controllerName);
            rvd.Add("Action", call.Method.Name);
            AddParameterValuesFromExpressionToDictionary(rvd, call);
            return rvd;
        }

        public static List<IParameterConverter> ParameterConverters = new List<IParameterConverter> { new LiteConverter() };

        static ConcurrentDictionary<ExpressionEvaluator.MethodKey, ParameterInfo[]> paramsCache = new ConcurrentDictionary<ExpressionEvaluator.MethodKey, ParameterInfo[]>();

        static void AddParameterValuesFromExpressionToDictionary(RouteValueDictionary rvd, MethodCallExpression call)
        {
            ParameterInfo[] parameters = paramsCache.GetOrAdd(new ExpressionEvaluator.MethodKey(call.Method), mt => call.Method.GetParameters());

            if (parameters.Length > 0)
            {
                for (int i = 0; i < parameters.Length; i++)
                {
                    Expression arg = call.Arguments[i];

                    object value = ExpressionEvaluator.Eval(arg);

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
