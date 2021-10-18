using Microsoft.AspNetCore.Mvc.Filters;
using Signum.Engine.Isolation;
using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Entities.Isolation;
using Signum.React.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Signum.React.Extensions.Isolation
{
    public class IsolationFilter : SignumDisposableResourceFilter
    {
        public override IDisposable? GetResource(ResourceExecutingContext context)
        {
            var user =  UserEntity.Current;

            if (user == null)
                return null;

            var isolation = user.TryMixin<IsolationMixin>()?.Isolation;

            if (isolation == null)
            {
                var isolationKey = context.HttpContext.Request.Headers["SF_Isolation"].FirstOrDefault();
                if (isolationKey != null)
                    isolation = Lite.Parse<IsolationEntity>(isolationKey);
            }

            return IsolationEntity.Override(isolation);
        }
    }
}
