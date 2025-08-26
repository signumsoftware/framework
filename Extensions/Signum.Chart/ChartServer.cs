using Signum.UserAssets;
using Signum.Chart;
using Microsoft.AspNetCore.Builder;
using System.Text.Json;
using Signum.Chart.UserChart;
using Signum.API;
using Signum.API.Json;
using Signum.Authorization;
using Signum.Authorization.Rules;

namespace Signum.Chart;

public static class ChartServer
{
    public static void Start(WebServerBuilder wsb)
    {
        if (wsb.AlreadyDefined(MethodBase.GetCurrentMethod()))
            return;

        UserAssetServer.Start(wsb);
        UserAssetServer.QueryPermissionSymbols.Add(ChartPermission.ViewCharting);

        CustomizeChartRequest();

        SignumServer.WebEntityJsonConverterFactory.AfterDeserilization.Register((ChartRequestModel cr) =>
        {
            if (cr.ChartScript != null)
                cr.GetChartScript().SynchronizeColumns(cr, null);

            if (cr.QueryName != null)
            {
                var qd = QueryLogic.Queries.QueryDescription(cr.QueryName);

                if (cr.Columns != null)
                    foreach (var c in cr.Columns)
                    {
                        c.ParseData(cr, qd, SubTokensOptions.CanElement | SubTokensOptions.CanAggregate | (cr.ChartTimeSeries != null ? SubTokensOptions.CanTimeSeries : 0));
                    }
            }
        });

        SignumServer.WebEntityJsonConverterFactory.AfterDeserilization.Register((UserChartEntity uc) =>
        {
            if (uc.ChartScript != null)
                uc.GetChartScript().SynchronizeColumns(uc, null);

            if (uc.Query != null)
            {
                var qd = QueryLogic.Queries.QueryDescription(uc.Query.ToQueryName());
                uc.ParseData(qd);
            }
        });

        UserChartEntity.SetConverters(
            query => QueryLogic.ToQueryName(query.Key),
            queryName => QueryLogic.GetQueryEntity(queryName));

        EntityPackTS.AddExtension += ep =>
        {
            if (ep.entity.IsNew || !ChartPermission.ViewCharting.IsAuthorized() || TypeAuthLogic.GetAllowed(typeof(UserChartEntity)).MaxDB() == TypeAllowedBasic.None)
                return;

            var userCharts = UserChartLogic.GetUserCharts(ep.entity.GetType());
            if (userCharts.Any())
                ep.extension.Add("userCharts", userCharts);
        };
    }

    private static void CustomizeChartRequest()
    {
        var converters = SignumServer.WebEntityJsonConverterFactory.GetPropertyConverters(typeof(ChartRequestModel));
        converters.Remove("queryName");

        converters.Add("queryKey", new PropertyConverter()
        {
            AvoidValidate = true,
            CustomReadJsonProperty = (ref Utf8JsonReader reader, ReadJsonPropertyContext ctx) =>
            {
                ((ChartRequestModel)ctx.Entity).QueryName = QueryLogic.ToQueryName(reader.GetString()!);
            },
            CustomWriteJsonProperty = (Utf8JsonWriter writer, WriteJsonPropertyContext ctx) =>
            {
                var cr = (ChartRequestModel)ctx.Entity;

                writer.WritePropertyName(ctx.LowerCaseName);
                writer.WriteStringValue(QueryLogic.GetQueryEntity(cr.QueryName).Key);
            }
        });

        converters.Add("filters", new PropertyConverter()
        {
            AvoidValidate = true,
            CustomReadJsonProperty = (ref Utf8JsonReader reader, ReadJsonPropertyContext ctx) =>
            {
                var list = JsonSerializer.Deserialize<List<FilterTS>>(ref reader, ctx.JsonSerializerOptions)!;

                var cr = (ChartRequestModel)ctx.Entity;

                var qd = QueryLogic.Queries.QueryDescription(cr.QueryName);

                cr.Filters = list.Select(l => l.ToFilter(qd, canAggregate: true, SignumServer.JsonSerializerOptions, cr.ChartTimeSeries != null)).ToList();
            },
            CustomWriteJsonProperty = (Utf8JsonWriter writer, WriteJsonPropertyContext ctx) =>
            {
                var cr = (ChartRequestModel)ctx.Entity;

                writer.WritePropertyName(ctx.LowerCaseName);
                JsonSerializer.Serialize(writer, cr.Filters.Select(f => FilterTS.FromFilter(f)).ToList(), ctx.JsonSerializerOptions);
            }
        });
    }
}
