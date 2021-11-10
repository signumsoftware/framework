using Signum.React.Maps;
using Signum.Engine.Isolation;
using Microsoft.AspNetCore.Builder;
using Signum.React.Filters;
using Signum.React.Extensions.Isolation;
using Microsoft.AspNetCore.Mvc;
using Signum.Entities.Isolation;

namespace Signum.React.Isolation;

public static class IsolationServer
{
    public static void Start(IApplicationBuilder app)
    {
        SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());

        SchemaMap.GetColorProviders += GetMapColors;

        SignumExceptionFilterAttribute.ApplyMixins += (ctx, e) => {
            e.Mixin<IsolationMixin>().Isolation = IsolationEntity.Current ?? (Lite<IsolationEntity>?)ctx.HttpContext.Items[IsolationFilter.Signum_Isolation_Key];
        };
    }

    public static MvcOptions AddIsolationFilter(this MvcOptions options, int atIndex = -1)
    {
        if (!options.Filters.OfType<SignumAuthenticationFilter>().Any())
            throw new InvalidOperationException("SignumAuthenticationFilter not found");

        if (atIndex >= 0)
            options.Filters.Insert(atIndex, new IsolationFilter());
        else
            options.Filters.Add(new IsolationFilter());

        return options;
    }

    static MapColorProvider[] GetMapColors()
    {
        var strategies = IsolationLogic.GetIsolationStrategies().SelectDictionary(t => TypeLogic.GetCleanName(t), p => p);

        return new[]
        {
            new MapColorProvider
            {
                Name = "isolation",
                NiceName = "Isolation",
                AddExtra = t =>
                {
                    var s = strategies.TryGetS(t.typeName);

                    if (s == null)
                        return;

                    t.extra["isolation"] = s.ToString();
                },
                Order = 3,
            },
        };
    }
}
