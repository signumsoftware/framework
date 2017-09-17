using Signum.Engine;
using Signum.Entities.Basics;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
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
    public class SignumAuthenticationResult
    {
        public IUserEntity User { get; set; }
        public IHttpActionResult ErrorResult { get; set; }
    }

    public class SignumAuthenticationFilterAttribute : Attribute, IAuthenticationFilter
    {
        public const string SavedRequestKey = "SAVED_REQUEST";
        public const string UserKey = "USER";

        public static Func<HttpActionContext, CultureInfo> GetCurrentCultures;      

        public static readonly IList<Func<HttpActionContext, SignumAuthenticationResult>> Authenticators = new List<Func<HttpActionContext, SignumAuthenticationResult>>();

        public bool AllowMultiple => false;


        public async Task AuthenticateAsync(HttpAuthenticationContext context, CancellationToken cancellationToken)
        {
            var actionContext = context.ActionContext;
          
            context.Request.Properties[SavedRequestKey] = await actionContext.Request.Content.ReadAsStringAsync();
            var result = Authenticate(actionContext);
            if(result != null)
            {
                context.ErrorResult = result.ErrorResult;
                context.Request.Properties[UserKey] = result.User;
            }
        }

        public Task ChallengeAsync(HttpAuthenticationChallengeContext context, CancellationToken cancellationToken)
        {
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

       
        private static SignumAuthenticationResult Authenticate(HttpActionContext actionContext)
        {
            foreach (var item in Authenticators)
            {
                var result = item(actionContext);
                if (result != null)
                    return result;
            }

            return null;
        }       
    }

    public class SignumAuthorizationFilterAttribute : FilterAttribute, IAuthorizationFilter
    {
        public async Task<HttpResponseMessage> ExecuteAuthorizationFilterAsync(HttpActionContext actionContext, CancellationToken cancellationToken, Func<Task<HttpResponseMessage>> continuation)
        {

            string action = ProfilerActionSplitterAttribute.GetActionDescription(actionContext);
            try
            {
                using (TimeTracker.Start(action))
                {
                    using (HeavyProfiler.Log("Web.API " + actionContext.Request.Method, () => actionContext.Request.RequestUri.ToString()))
                    {
                        var user = (IUserEntity)GetProp(actionContext, SignumAuthenticationFilterAttribute.UserKey);
                        using (user != null ? UserHolder.UserSession(user) : null)
                        {
                            var culture = (CultureInfo)SignumAuthenticationFilterAttribute.GetCurrentCultures?.Invoke(actionContext);
                            using (culture != null ? CultureInfoUtils.ChangeBothCultures(culture) : null)
                            {
                                var result =  await continuation();
                                return result;
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

        private object GetProp(HttpActionContext actionContext, string key)
        {
            object result = null;
            actionContext.Request.Properties.TryGetValue(key, out result);
            return result;
        }
    }
}