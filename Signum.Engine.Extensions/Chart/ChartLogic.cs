using Signum.Entities.Chart;
using Signum.Utilities.Reflection;
using Signum.Engine.Authorization;

namespace Signum.Engine.Chart;

public static class ChartLogic
{
    public static void Start(SchemaBuilder sb, bool googleMapsChartScripts, string[]? svgMapUrls = null)
    {
        if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
        {
            QueryLogic.Start(sb);

            PermissionAuthLogic.RegisterTypes(typeof(ChartPermission));

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
