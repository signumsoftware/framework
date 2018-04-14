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
using Signum.Engine.Maps;

namespace Signum.React.Map
{
    public static class MapServer
    {
        public static void Start(HttpConfiguration config)
        {
            ReflectionServer.RegisterLike(typeof(MapMessage));
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
            
            if (Schema.Current.Tables.Any(a => a.Value.SystemVersioned != null))
            {
                SchemaMap.GetColorProviders += () => new[]
                {
                    new MapColorProvider
                    {
                        Name = "rows_history",
                        NiceName = MapMessage.RowsHistory.NiceToString(),
                    }
                };
            }

            SchemaMap.GetColorProviders += () => new[]
            {
                new MapColorProvider
                {
                    Name = "tableSize",
                    NiceName = MapMessage.TableSize.NiceToString(),
                }
            };

            if(Schema.Current.Tables.Any(a => a.Value.SystemVersioned != null))
            { 
                SchemaMap.GetColorProviders += () => new[]
                {
                    new MapColorProvider
                    {
                        Name = "tableSize_history",
                        NiceName = MapMessage.TableSizeHistory.NiceToString(),
                    }
                };
            }
        }
    }
}