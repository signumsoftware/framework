using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Signum.Engine.Authorization;
using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using Signum.Entities.Isolation;
using Signum.React.Filters;

namespace Signum.React.Extensions.Isolation;

public class IsolationFilter : SignumDisposableResourceFilter
{
    public const string Signum_Isolation_Key = "Signum_Isolation";

    public static Func<HttpContext, Lite<IsolationEntity>?> GetIsolationFromHttpContext; 

    public override IDisposable? GetResource(ResourceExecutingContext context)
    {
        var isolation = IsolationEntity.CurrentUserIsolation;

        if (isolation == null && UserHolder.Current != null && !UserHolder.Current.User.Is(AuthLogic.AnonymousUser))
        {
            var isolationKey = context.HttpContext.Request.Headers[Signum_Isolation_Key].FirstOrDefault();
            if (isolationKey != null)
                isolation = Lite.Parse<IsolationEntity>(isolationKey);
        }

        if (isolation == null)
        {
            if (GetIsolationFromHttpContext != null)
                isolation = GetIsolationFromHttpContext(context.HttpContext);
        }

        context.HttpContext.Items[Signum_Isolation_Key] = isolation;

        return IsolationEntity.Override(isolation);
    }
}
