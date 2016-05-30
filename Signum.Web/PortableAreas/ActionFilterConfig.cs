using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Signum.Utilities;
using System.Linq.Expressions;

namespace Signum.Web.PortableAreas
{
    public class ActionFilterConfig<T> : IFilterConfig where T : Controller
    {
        ControllerFilterConfig<T> parent; 

        public ActionFilterConfig(ControllerFilterConfig<T> parent)
        {
            this.parent = parent;
        }

        List<FilterAttribute> addFilters;
        Func<ConditionalFilterContext, FilterAttribute> addFilterDelegate; 
        List<Type> removeFilters;

        public ActionFilterConfig<T> AddFilters(params FilterAttribute[] actionFilters)
        {
            if (actionFilters == null || actionFilters.Length == 0)
                throw new ArgumentNullException("actionFilters");

            if (addFilters == null)
                addFilters = new List<FilterAttribute>();

            addFilters.AddRange(actionFilters);

            return this;
        }

        public ActionFilterConfig<T> AddFilters(Func<ConditionalFilterContext, FilterAttribute> addFilter)
        {
            addFilterDelegate += addFilter;

            return this; 
        }

        public ActionFilterConfig<T> RemoveFilters<R>()
        {
            if (removeFilters == null)
                removeFilters = new List<Type>();

            removeFilters.AddRange(typeof(R));

            return this;
        }

        public void Configure(FilterInfo filterInfo, ControllerContext controllerContext, ActionDescriptor actionDescription)
        {
            if (removeFilters != null)
            {
                foreach (var type in removeFilters)
                    filterInfo.RemoveFilters(type);
            }
          
            if (addFilters != null)
                filterInfo.AddFilters(addFilters);

            if (addFilterDelegate != null)
            {
                var ctx = new ConditionalFilterContext(controllerContext, actionDescription, filterInfo);

                var filters = addFilterDelegate.GetInvocationListTyped().Select(del => del(ctx)).NotNull().ToList();

                filterInfo.AddFilters(filters);
            }
        }

        public ActionFilterConfig<T> Action(Expression<Func<T, ActionResult>> expression)
        {
            return parent.Action(expression);
        }
    }


    public class ConditionalFilterContext
    {
        internal ConditionalFilterContext(ControllerContext controllerContext, ActionDescriptor actionDescriptor, FilterInfo filterInfo)
        {
            this.ControllerContext = controllerContext;
            this.ActionDescriptor = actionDescriptor;
            this.FilterInfo = filterInfo; 
        }

        public ControllerContext ControllerContext { get; private set; }
        public ActionDescriptor ActionDescriptor { get; private set; }
        public FilterInfo FilterInfo { get; private set; }
    }

    public class ControllerFilterConfig<T> : IFilterConfig where T : Controller
    {
        Dictionary<string, ActionFilterConfig<T>> actions;
        List<FilterAttribute> addFilters;
        Func<ConditionalFilterContext, FilterAttribute> addFilterDelegate;
        List<Type> removeFilters;

        public ControllerFilterConfig<T> AddFilters(params FilterAttribute[] actionFilters)
        {
            if (actionFilters == null || actionFilters.Length == 0)
                throw new ArgumentNullException("actionFilters");

            if (addFilters == null)
                addFilters = new List<FilterAttribute>();

            addFilters.AddRange(actionFilters);

            return this;
        }

        public ControllerFilterConfig<T> AddFilters(Func<ConditionalFilterContext, FilterAttribute> addFilter)
        {
            addFilterDelegate += addFilter;

            return this;
        }

        public ControllerFilterConfig<T> RemoveFilters<R>()
        {
            if (removeFilters == null)
                removeFilters = new List<Type>();

            removeFilters.AddRange(typeof(T));

            return this;
        }

        public ActionFilterConfig<T> Action(Expression<Func<T, ActionResult>> expression)
        {
            if (actions == null)
                actions = new Dictionary<string, ActionFilterConfig<T>>(); 

            return actions.GetOrCreate(GetMethodName(expression), ()=> new ActionFilterConfig<T>(this)); 
        }

        static string GetMethodName(Expression<Func<T, ActionResult>> expression)
        {
            var methodCallEpxression = expression.Body as MethodCallExpression;
            if (methodCallEpxression == null)
                throw new InvalidOperationException("Lambda to Action method expected"); 
            return methodCallEpxression.Method.Name;
        }

        public void Configure(FilterInfo filterInfo, ControllerContext controllerContext, ActionDescriptor actionDescription)
        {
            if (removeFilters != null)
                foreach (var type in removeFilters)
                    filterInfo.RemoveFilters(type);

            if (addFilters != null)
                filterInfo.AddFilters(addFilters);

            if (addFilterDelegate != null)
            {
                var ctx = new ConditionalFilterContext(controllerContext, actionDescription, filterInfo); 

                filterInfo.AddFilters(
                    addFilterDelegate.GetInvocationListTyped()
                    .Select(del => del(ctx)).NotNull());
            }

            var afc = actions?.TryGetC(actionDescription.ActionName);
            if (afc != null)
                afc.Configure(filterInfo, controllerContext, actionDescription);
        }
    }

    static class FilterInfoExtensions
    {
        public static void AddFilters(this FilterInfo filters, IEnumerable<FilterAttribute> actionFilters)
        {
            filters.ActionFilters.AddRange(actionFilters.OfType<IActionFilter>());
            filters.ResultFilters.AddRange(actionFilters.OfType<IResultFilter>());
            filters.AuthorizationFilters.AddRange(actionFilters.OfType<IAuthorizationFilter>());
            filters.ExceptionFilters.AddRange(actionFilters.OfType<IExceptionFilter>());
        }

        public static void RemoveFilters(this FilterInfo filters, Type type)
        {
            filters.ActionFilters.RemoveAll(a => a.GetType() == type);
            filters.ResultFilters.RemoveAll(a => a.GetType() == type);
            filters.AuthorizationFilters.RemoveAll(a => a.GetType() == type);
            filters.ExceptionFilters.RemoveAll(a => a.GetType() == type);
        }
    }
}
