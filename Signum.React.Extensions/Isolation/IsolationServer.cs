using Signum.Utilities;
using System.Reflection;
using Signum.Engine.Basics;
using Signum.React.Maps;
using Signum.Entities.Map;
using Signum.React.Facades;
using Signum.Engine.Isolation;
using Microsoft.AspNetCore.Builder;

namespace Signum.React.Isolation
{
    public static class IsolationServer
    {
        public static void Start(IApplicationBuilder app)
        {
            ReflectionServer.RegisterLike(typeof(MapMessage));
            SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());

            SchemaMap.GetColorProviders += GetMapColors;
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