using Microsoft.AspNetCore.Mvc.Filters;
using Signum.Engine.Isolation;
using Signum.Entities.Authorization;
using Signum.Entities.Isolation;
using Signum.React.Filters;
using System.Threading.Tasks;

namespace Signum.React.Extensions.Isolation
{
    public class IsolationFilter : SignumDisposableResourceFilter
    {
        public const string Signum_Isolation_Key = "Signum_Isolation";

        public override IDisposable? GetResource(ResourceExecutingContext context)
        {
            var user = UserEntity.Current;

            if (user == null)
                return null;

            var isolation = user.TryMixin<IsolationMixin>()?.Isolation;

            if (isolation == null)
            {
                var isolationKey = context.HttpContext.Request.Headers[Signum_Isolation_Key].FirstOrDefault();
                if (isolationKey != null)
                    isolation = Lite.Parse<IsolationEntity>(isolationKey);
            }

            context.HttpContext.Items[Signum_Isolation_Key] = isolation;

            return IsolationEntity.Override(isolation);
        }
    }
}
