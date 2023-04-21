using Signum.API;
using Signum.Chart.ColorPalette;
using Signum.Chart.UserChart;

namespace Signum.Chart;

public static class ChartLogic
{
    public static void Start(SchemaBuilder sb, WebServerBuilder? wsb, bool googleMapsChartScripts, string[]? svgMapUrls = null)
    {
        if (wsb != null)
            ChartServer.Start(wsb.ApplicationBuilder);

        if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
        {
            QueryLogic.Start(sb);

            PermissionLogic.RegisterTypes(typeof(ChartPermission));

            ColorPaletteLogic.Start(sb);
            ChartScriptLogic.Start(sb, googleMapsChartScripts, svgMapUrls);
            UserChartLogic.Start(sb);
        }
    }

    public static Task<ResultTable> ExecuteChartAsync(ChartRequestModel request, CancellationToken token)
    {
        return QueryLogic.Queries.ExecuteQueryAsync(request.ToQueryRequest(), token);
    }

  
    public static ResultTable ExecuteChart(ChartRequestModel request)
    {
        return QueryLogic.Queries.ExecuteQuery(request.ToQueryRequest());

    }

    public static QueryRequest ToQueryRequest(this ChartRequestModel request)
    {
        return new QueryRequest
        {
            QueryName = request.QueryName,
            GroupResults = request.HasAggregates(),
            Columns = request.GetQueryColumns(),
            Filters = request.Filters,
            Orders = request.GetQueryOrders(),
            Pagination = request.MaxRows.HasValue ? new Pagination.Firsts(request.MaxRows.Value + 1) : new Pagination.All(),
        };
    }

}
