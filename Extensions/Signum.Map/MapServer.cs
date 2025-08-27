using Microsoft.AspNetCore.Builder;
using Signum.API;
using Signum.Authorization;

namespace Signum.Map;

public static class MapServer
{
    public static void Start(WebServerBuilder wsb)
    {
        if (wsb.AlreadyDefined(MethodBase.GetCurrentMethod()))
            return;

        ReflectionServer.RegisterLike(typeof(MapMessage), () => MapPermission.ViewMap.IsAuthorized());

        MapColorProvider.GetColorProviders += () => new[]
        {
            new MapColorProvider
            {
                Name = "namespace",
                NiceName = MapMessage.Namespace.NiceToString(),
            },
        };

        MapColorProvider.GetColorProviders += () => new[]
        {
            new MapColorProvider
            {
                Name = "entityKind",
                NiceName = typeof(EntityKind).Name,
            }
        };

        MapColorProvider.GetColorProviders += () => new[]
        {
            new MapColorProvider
            {
                Name = "columns",
                NiceName = MapMessage.Columns.NiceToString(),
            }
        };

        MapColorProvider.GetColorProviders += () => new[]
        {
            new MapColorProvider
            {
                Name = "entityData",
                NiceName = typeof(EntityData).Name,
            }
        };

        MapColorProvider.GetColorProviders += () => new[]
        {
            new MapColorProvider
            {
                Name = "rows",
                NiceName = MapMessage.Rows.NiceToString(),
            }
        };

        if (Signum.Engine.Maps.Schema.Current.Tables.Any(a => a.Value.SystemVersioned != null))
        {
            MapColorProvider.GetColorProviders += () => new[]
            {
                new MapColorProvider
                {
                    Name = "rows_history",
                    NiceName = MapMessage.RowsHistory.NiceToString(),
                }
            };
        }

        MapColorProvider.GetColorProviders += () => new[]
        {
            new MapColorProvider
            {
                Name = "tableSize",
                NiceName = MapMessage.TableSize.NiceToString(),
            }
        };

        if(Signum.Engine.Maps.Schema.Current.Tables.Any(a => a.Value.SystemVersioned != null))
        {
            MapColorProvider.GetColorProviders += () => new[]
            {
                new MapColorProvider
                {
                    Name = "tableSize_history",
                    NiceName = MapMessage.TableSizeHistory.NiceToString(),
                }
            };
        }

        if (Signum.Engine.Maps.Schema.Current.Tables.ContainsKey(typeof(UserEntity)))
        {
            MapColorProvider.GetColorProviders += AuthColorProvider.GetMapColors;
        }
    }
}
