using Signum.Engine;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace Signum.React.Filters
{
    public class SignumAuthenticationAndProfilerAttribute : FilterAttribute, IAuthorizationFilter
    {
        public static Func<HttpActionContext, IDisposable> Authenticate;

        public Task<HttpResponseMessage> ExecuteAuthorizationFilterAsync(HttpActionContext actionContext, CancellationToken cancellationToken, Func<Task<HttpResponseMessage>> continuation)
        {
            string action = ProfilerActionSplitterAttribute.GetActionSescription(actionContext);

            actionContext.Request.RegisterForDispose(TimeTracker.Start(action));

            IDisposable profiler = HeavyProfiler.Log("Web.API " + actionContext.Request.Method, () => actionContext.Request.RequestUri.ToString());
            if (profiler != null)
                actionContext.Request.RegisterForDispose(profiler);

            //if (ProfilerLogic.SessionTimeout != null)
            //{
            //    IDisposable sessionTimeout = Connector.CommandTimeoutScope(ProfilerLogic.SessionTimeout.Value);
            //    if (sessionTimeout != null)
            //        actionContext.Request.RegisterForDispose(sessionTimeout);
            //}

            if (Authenticate != null)
            {
                var session = Authenticate(actionContext);
                if (session != null)
                    actionContext.Request.RegisterForDispose(session);
            }

            if (actionContext.Response != null)
                return Task.FromResult(actionContext.Response);
            return continuation();
        }
    }
}