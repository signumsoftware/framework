using Signum.Engine;
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
    public class SignumAuthenticationAndProfilerAttribute : FilterAttribute, IAuthorizationFilter
    {
        public const string SavedRequestKey = "SAVED_REQUEST";

        public static Func<HttpActionContext, IDisposable> Authenticate;

        public static Func<HttpActionContext, IDisposable> GetCurrentCultures;

        public async Task<HttpResponseMessage> ExecuteAuthorizationFilterAsync(HttpActionContext actionContext, CancellationToken cancellationToken, Func<Task<HttpResponseMessage>> continuation)
        {
            string action = ProfilerActionSplitterAttribute.GetActionDescription(actionContext);

            try
            {
                using (TimeTracker.Start(action))
                {
                    using (HeavyProfiler.Log("Web.API " + actionContext.Request.Method, () => actionContext.Request.RequestUri.ToString()))
                    {
                        //if (ProfilerLogic.SessionTimeout != null)
                        //{
                        //    IDisposable sessionTimeout = Connector.CommandTimeoutScope(ProfilerLogic.SessionTimeout.Value);
                        //    if (sessionTimeout != null)
                        //        actionContext.Request.RegisterForDispose(sessionTimeout);
                        //}

                        actionContext.Request.Properties[SavedRequestKey] = await actionContext.Request.Content.ReadAsStringAsync();

                        using (Authenticate == null ? null : Authenticate(actionContext))
                        {
                            using (GetCurrentCultures?.Invoke(actionContext))
                            {
                                if (actionContext.Response != null)
                                    return actionContext.Response;

                                return await continuation();
                            }
                        }
                    }
                }
            }
            finally
            {
                Statics.CleanThreadContextAndAssert();
            }
        }
    }
}