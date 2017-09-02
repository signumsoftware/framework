using Signum.Engine;
using Signum.Entities.Basics;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using System.Web.Http.Routing;

namespace Signum.React.Filters
{
    public class SignumAuthenticationAndProfilerAttribute : Attribute, IAuthenticationFilter
    {
        public const string SavedRequestKey = "SAVED_REQUEST";

        public static Func<HttpActionContext, IDisposable> GetCurrentCultures;

        public static readonly IList<Func<HttpActionContext, IDisposable>> Authenticators = new List<Func<HttpActionContext, IDisposable>>();

        public bool AllowMultiple => false;


        public async Task AuthenticateAsync(HttpAuthenticationContext context, CancellationToken cancellationToken)
        {
            var actionContext = context.ActionContext;

            string action = ProfilerActionSplitterAttribute.GetActionDescription(actionContext);

            context.Request.Properties["timeTracker"] = TimeTracker.Start(action);
            context.Request.Properties["heavyProfiler"] = HeavyProfiler.Log("Web.API " + actionContext.Request.Method, () => actionContext.Request.RequestUri.ToString());
            context.Request.Properties[SavedRequestKey] = await actionContext.Request.Content.ReadAsStringAsync();
            context.Request.Properties["authenticate"] = Authenticate(actionContext);
            context.Request.Properties["culture"] = GetCurrentCultures?.Invoke(actionContext);
            
        }

        public Task ChallengeAsync(HttpAuthenticationChallengeContext context, CancellationToken cancellationToken)
        {
            Dispose(context.ActionContext, "culture");
            Dispose(context.ActionContext, "authenticate");
            Dispose(context.ActionContext, "heavyProfiler");
            Dispose(context.ActionContext, "timeTracker");
            Statics.CleanThreadContextAndAssert();

            return Task.CompletedTask;
        }

        private void Dispose(HttpActionContext context, string key)
        {
            object result;
            if (context.Request.Properties.TryGetValue(key, out result))
            {
                if (result != null)
                    ((IDisposable)result).Dispose();
            }
        }

       
        private static IDisposable Authenticate(HttpActionContext actionContext)
        {
            foreach (var item in Authenticators)
            {
                var disposable = item(actionContext);
                if (disposable != null)
                    return disposable;
            }

            return null;
        }       
    }

    public class SignumAuthorizationAttribute : FilterAttribute, IAuthorizationFilter
    {
        public async Task<HttpResponseMessage> ExecuteAuthorizationFilterAsync(HttpActionContext actionContext, CancellationToken cancellationToken, Func<Task<HttpResponseMessage>> continuation)
        {
            var userOne = UserHolder.Current;

            var result = await continuation();

            var userTwo = UserHolder.Current;

            return result;
        }
    }
}