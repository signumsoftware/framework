using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Signum.Entities.Basics;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace Signum.React.Filters
{
    public class SignumAuthenticationResult
    {
        public IUserEntity? User { get; set; }
    }

    public class SignumEnableBufferingFilter : IResourceFilter
    {
        public void OnResourceExecuting(ResourceExecutingContext context)
        {
            context.HttpContext.Request.EnableBuffering();
        }

        public void OnResourceExecuted(ResourceExecutedContext context)
        {
        }

    }

    public class CleanThreadContextAndAssertFilter : IResourceFilter
    {
        public void OnResourceExecuting(ResourceExecutingContext context)
        {
        }

        public void OnResourceExecuted(ResourceExecutedContext context)
        {
            Statics.CleanThreadContextAndAssert();
        }
    }

    public class SignumAuthenticationFilter : SignumDisposableResourceFilter
    {
        public const string Signum_User_Key = "Signum_User";

        public static readonly IList<Func<FilterContext, SignumAuthenticationResult?>> Authenticators = new List<Func<FilterContext, SignumAuthenticationResult?>>();

        private static SignumAuthenticationResult? Authenticate(ResourceExecutingContext actionContext)
        {
            foreach (var item in Authenticators)
            {
                var result = item(actionContext);
                if (result != null)
                    return result;
            }

            return null;
        }

        public override IDisposable? GetResource(ResourceExecutingContext context)
        {
            var result = Authenticate(context);

            if (result == null)
                return null;

            context.HttpContext.Items[Signum_User_Key] = result.User;

            return UserHolder.UserSession(result.User!);
        }
    }

    public class SignumCultureSelectorFilter : IResourceFilter
    {
        public static Func<ResourceExecutingContext, CultureInfo?>? GetCurrentCulture;

        const string Culture_Key = "OldCulture";
        const string UICulture_Key = "OldUICulture";
        public void OnResourceExecuting(ResourceExecutingContext context)
        {
            var culture = GetCurrentCulture?.Invoke(context);
            if (culture != null)
            {
                context.HttpContext.Items[Culture_Key] = CultureInfo.CurrentCulture;
                context.HttpContext.Items[UICulture_Key] = CultureInfo.CurrentUICulture;

                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;
            }
        }

        public void OnResourceExecuted(ResourceExecutedContext context)
        {
            CultureInfo.CurrentUICulture = context.HttpContext.Items[UICulture_Key] as CultureInfo ?? CultureInfo.CurrentUICulture;
            CultureInfo.CurrentCulture = context.HttpContext.Items[Culture_Key] as CultureInfo ?? CultureInfo.CurrentCulture;
        }
    }

    public class SignumTimesTrackerFilter : SignumDisposableResourceFilter
    {
        public override IDisposable? GetResource(ResourceExecutingContext context)
        {
            string action = ProfilerActionSplitterAttribute.GetActionDescription(context);
            return TimeTracker.Start(action);
        }
    }

    public class SignumHeavyProfilerFilter : SignumDisposableResourceFilter
    {
        public override IDisposable? GetResource(ResourceExecutingContext context)
        {
            return HeavyProfiler.Log("Web.API " + context.HttpContext.Request.Method, () => context.HttpContext.Request.GetDisplayUrl());
        }
    }

    public class SignumCurrentContextFilter : SignumDisposableResourceFilter
    {
        static ThreadVariable<FilterContext?> CurrentContextVariable = Statics.ThreadVariable<FilterContext?>("currentContext");

        public static FilterContext? CurrentContext => CurrentContextVariable.Value;

        public static UrlHelper? Url
        {
            get
            {
                var cc = CurrentContext;
                return cc == null ? null : new UrlHelper(cc);
            }
        }


        public override IDisposable? GetResource(ResourceExecutingContext context)
        {
            var old = CurrentContextVariable.Value;
            CurrentContextVariable.Value = context;
            return new Disposable(() => CurrentContextVariable.Value = old);
        }
    }

    public static class UrlHelperExtensions
    {
        public static string Action<T>(this UrlHelper helper, string action, object values) where T : ControllerBase
        {
            return helper.Action(new UrlActionContext
            {
                Action = action,
                Controller = typeof(T).Name.TryBefore("Controller") ?? typeof(T).Name,
                Values = values,
            });
        }
    }

    public abstract class SignumDisposableResourceFilter : IAsyncResourceFilter
    {
        public abstract IDisposable? GetResource(ResourceExecutingContext context);

        public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            using (GetResource(context))
                await next();
        }
    }

    public class SignumHeavyProfilerResultFilter : IAsyncAlwaysRunResultFilter
    {
        public Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            using (HeavyProfiler.Log("Result", () => context.Result?.GetType().ToString()))
                return next();
        }
    }

    public class SignumHeavyProfilerActionFilter : IAsyncActionFilter
    {
        public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            using (HeavyProfiler.Log("Action", () => context.ActionDescriptor.DisplayName))
                return next();
        }
    }
}
