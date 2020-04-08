using Signum.Utilities;
using System.Linq;
using System.Reflection;
using Signum.Entities;
using Signum.React.Maps;
using Signum.Entities.Map;
using Signum.React.Facades;
using Microsoft.AspNetCore.Builder;
using Signum.Engine.Maps;
using Signum.React.Authorization;
using Signum.Engine.Authorization;

namespace Signum.React.Map
{
    public static class MapServer
    {
        public static void Start(IApplicationBuilder app)
        {
            ReflectionServer.RegisterLike(typeof(MapMessage), () => MapPermission.ViewMap.IsAuthorized());
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
