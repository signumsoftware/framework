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
using Signum.React.Facades;
using Signum.Engine.Disconnected;

namespace Signum.React.Map
{
    public static class DisconnectedServer
    {
        public static void Start(HttpConfiguration config)
        {
            SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());

            SchemaMap.GetColorProviders += GetMapColors;
        }

        static MapColorProvider[] GetMapColors()
        {
            var strategies = DisconnectedLogic.GetStrategyPairs().SelectDictionary(t => TypeLogic.GetCleanName(t), p => p);

            return new[]
            {
                new MapColorProvider
                {
                    Name = "disconnected",
                    NiceName = "Disconnected",
                    AddExtra = t =>
                    {
                        var s = strategies.TryGetC(t.typeName);

                        if (s == null)
                            return;

                        t.extra["disc-upload"] = s.Upload.ToString();
                        foreach (var mt in t.mlistTables)
                            mt.extra["disc-upload"] = s.Upload.ToString();

                        t.extra["disc-download"] = s.Download.ToString();
                        foreach (var mt in t.mlistTables)
                            mt.extra["disc-download"] = s.Download.ToString();

                    },
                    Order = 4,
                },
            };
        }

    }
}