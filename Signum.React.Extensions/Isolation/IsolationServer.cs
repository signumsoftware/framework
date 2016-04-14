using Signum.Entities.UserAssets;
using Signum.React.Json;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Http;
using Newtonsoft.Json;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Basics;
using Signum.React.UserAssets;
using Signum.Entities;
using Signum.React.ApiControllers;
using Signum.Entities.DynamicQuery;
using Signum.React.Maps;
using Signum.Entities.Map;
using Signum.React.Facades;
using Signum.Engine.Isolation;

namespace Signum.React.Isolation
{
    public static class IsolationServer
    {
        public static void Start(HttpConfiguration config)
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