using Signum.Utilities;
using System.Reflection;
using Signum.Engine.Basics;
using Signum.React.Maps;
using Signum.Entities.Map;
using Signum.React.Facades;
using Signum.Engine.Isolation;
using Microsoft.AspNetCore.Builder;
using Signum.Engine.Authorization;
using Signum.React.Filters;
using Signum.React.Extensions.Isolation;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System;

namespace Signum.React.Isolation
{
    public static class IsolationServer
    {
        public static void Start(IApplicationBuilder app)
        {
            ReflectionServer.RegisterLike(typeof(MapMessage), () => MapPermission.ViewMap.IsAuthorized());
            SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());

            SchemaMap.GetColorProviders += GetMapColors;
        }

        public static MvcOptions AddIsolationFilter(this MvcOptions options)
        {
            if (!options.Filters.OfType<SignumAuthenticationFilter>().Any())
                throw new InvalidOperationException("SignumAuthenticationFilter not found");

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
}
