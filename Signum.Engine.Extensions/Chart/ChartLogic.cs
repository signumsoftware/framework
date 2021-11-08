using Signum.Entities.Chart;
using Signum.Utilities.Reflection;
using Signum.Engine.Authorization;
using System.Threading.Tasks;
using System.Threading;

namespace Signum.Engine.Chart
{
    public static class ChartLogic
    {
        public static void Start(SchemaBuilder sb, bool googleMapsChartScripts, string[]? svgMapUrls = null)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                QueryLogic.Start(sb);

                PermissionAuthLogic.RegisterTypes(typeof(ChartPermission));

                ChartColorLogic.Start(sb);
                ChartScriptLogic.Start(sb, googleMapsChartScripts, svgMapUrls);
                UserChartLogic.Start(sb);
            }
        }

        public static Task<ResultTable> ExecuteChartAsync(ChartRequestModel request, CancellationToken token)
        {
            IDynamicQueryCore core = QueryLogic.Queries.GetQuery(request.QueryName).Core.Value;

            return miExecuteChartAsync.GetInvoker(core.GetType().GetGenericArguments()[0])(request, core, token);
        }

        static GenericInvoker<Func<ChartRequestModel, IDynamicQueryCore, CancellationToken, Task<ResultTable>>> miExecuteChartAsync =
            new((req, dq, token) => ExecuteChartAsync<int>(req, (DynamicQueryCore<int>)dq, token));
        static async Task<ResultTable> ExecuteChartAsync<T>(ChartRequestModel request, DynamicQueryCore<T> dq, CancellationToken token)
        {
            using (ExecutionMode.UserInterface())
            {
                var result = await dq.ExecuteQueryAsync(new QueryRequest
                {
                    GroupResults = request.HasAggregates(),
                    QueryName = request.QueryName,
                    Columns = request.GetQueryColumns(),
                    Filters = request.Filters,
                    Orders = request.GetQueryOrders(),
                    Pagination = request.MaxRows.HasValue ? new Pagination.Firsts(request.MaxRows.Value + 1) : new Pagination.All(),
                }, token);


                if (request.MaxRows.HasValue && result.Rows.Length == request.MaxRows.Value)
                    throw new InvalidOperationException($"The chart request for ${request.QueryName} exceeded the max rows ({request.MaxRows})");

                return result;
            }
        }

        public static ResultTable ExecuteChart(ChartRequestModel request)
        {
            IDynamicQueryCore core = QueryLogic.Queries.GetQuery(request.QueryName).Core.Value;

            return miExecuteChart.GetInvoker(core.GetType().GetGenericArguments()[0])(request, core);
        }

        static GenericInvoker<Func<ChartRequestModel, IDynamicQueryCore, ResultTable>> miExecuteChart =
            new((req, dq) => ExecuteChart<int>(req, (DynamicQueryCore<int>)dq));
        static ResultTable ExecuteChart<T>(ChartRequestModel request, DynamicQueryCore<T> dq)
        {
            using (ExecutionMode.UserInterface())
            {
                return dq.ExecuteQuery(new QueryRequest
                {
                    GroupResults = request.HasAggregates(),
                    QueryName = request.QueryName,
                    Columns = request.GetQueryColumns(),
                    Filters = request.Filters,
                    Orders = request.GetQueryOrders(),
                    Pagination = new Pagination.All(),
                });
            }
        }
    }
}
