using Signum.API;
using Signum.Chart.ColorPalette;
using Signum.Chart.UserChart;
using Signum.Omnibox;

namespace Signum.Chart;

public static class ChartLogic
{
    public static void Start(SchemaBuilder sb, bool googleMapsChartScripts, string[]? svgMapUrls = null)
    {
        if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
        {
            QueryLogic.Start(sb);

            PermissionLogic.RegisterTypes(typeof(ChartPermission));

            ColorPaletteLogic.Start(sb);
            ChartScriptLogic.Start(sb, googleMapsChartScripts, svgMapUrls);
            UserChartLogic.Start(sb);

            if (sb.WebServerBuilder != null)
            {
                ChartServer.Start(sb.WebServerBuilder);
                OmniboxParser.Generators.Add(new ChartOmniboxResultGenerator());
                OmniboxParser.Generators.Add(new UserChartOmniboxResultGenerator(UserChartLogic.Autocomplete));
            }
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
            SystemTime = request.ChartTimeSeries?.ToSystemTimeRequest(),
            Columns = request.GetQueryColumns(),
            Filters = request.Filters,
            Orders = request.GetQueryOrders(),
            Pagination = request.MaxRows.HasValue ? new Pagination.Firsts(request.MaxRows.Value + 1) : new Pagination.All(),
        };
    }

}
