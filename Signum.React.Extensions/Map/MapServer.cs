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

namespace Signum.React.Map
{
    public static class MaptServer
    {
        public static void Start(HttpConfiguration config)
        {
            SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());

            SchemaMap.GetColorProviders += () => new[]
            {
                new MapColorProvider
                {
                    Name = "namespace",
                    NiceName = MapMessage.Namespace.NiceToString(),
                },
            };

            SchemaMap.GetColorProviders += () => new[]
            {
                new MapColorProvider
                {
                    Name = "entityKind",
                    NiceName = typeof(EntityKind).Name,
                }
            };

            SchemaMap.GetColorProviders += () => new[]
            {
                new MapColorProvider
                {
                    Name = "columns",
                    NiceName = MapMessage.Columns.NiceToString(),
                }
            };

            SchemaMap.GetColorProviders += () => new[]
            {
                new MapColorProvider
                {
                    Name = "entityData",
                    NiceName = typeof(EntityData).Name,
                }
            };

            SchemaMap.GetColorProviders += () => new[]
            {
                new MapColorProvider
                {
                    Name = "rows",
                    NiceName = MapMessage.Rows.NiceToString(),
                }
            };

            SchemaMap.GetColorProviders += () => new[]
            {
                new MapColorProvider
                {
                    Name = "tableSize",
                    NiceName = MapMessage.TableSize.NiceToString(),
                }
            };
        }
    }
}