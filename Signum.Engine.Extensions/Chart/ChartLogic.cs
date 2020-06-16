using System;
using System.Collections.Generic;
using System.Linq;
using Signum.Entities.Chart;
using Signum.Engine.DynamicQuery;
using System.Reflection;
using Signum.Utilities.Reflection;
using Signum.Entities.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Engine.Basics;
using Signum.Engine.Authorization;
using System.Threading.Tasks;
using System.Threading;

namespace Signum.Engine.Chart
{
    public static class ChartLogic
    {
        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                QueryLogic.Start(sb);

                PermissionAuthLogic.RegisterTypes(typeof(ChartPermission));

                ChartColorLogic.Start(sb);
                ChartScriptLogic.Start(sb);
                UserChartLogic.Start(sb);
            }
        }

        public static Task<ResultTable> ExecuteChartAsync(ChartRequestModel request, CancellationToken token)
        {
            IDynamicQueryCore core = QueryLogic.Queries.GetQuery(request.QueryName).Core.Value;

            return miExecuteChartAsync.GetInvoker(core.GetType().GetGenericArguments()[0])(request, core, token);
        }

        static GenericInvoker<Func<ChartRequestModel, IDynamicQueryCore, CancellationToken, Task<ResultTable>>> miExecuteChartAsync =
            new GenericInvoker<Func<ChartRequestModel, IDynamicQueryCore, CancellationToken, Task<ResultTable>>>((req, dq, token) => ExecuteChartAsync<int>(req, (DynamicQueryCore<int>)dq, token));
        static async Task<ResultTable> ExecuteChartAsync<T>(ChartRequestModel request, DynamicQueryCore<T> dq, CancellationToken token)
        {
            using (ExecutionMode.UserInterface())
            {
                return await dq.ExecuteQueryAsync(new QueryRequest
                {
                    GroupResults = request.HasAggregates(),
                    QueryName = request.QueryName,
                    Columns =  request.GetQueryColumns(),
                    Filters = request.Filters,
                    Orders = request.GetQueryOrders(),
                    Pagination = new Pagination.All(),
                }, token);
            }
        }

        public static ResultTable ExecuteChart(ChartRequestModel request)
        {
            IDynamicQueryCore core = QueryLogic.Queries.GetQuery(request.QueryName).Core.Value;

            return miExecuteChart.GetInvoker(core.GetType().GetGenericArguments()[0])(request, core);
        }

        static GenericInvoker<Func<ChartRequestModel, IDynamicQueryCore, ResultTable>> miExecuteChart =
            new GenericInvoker<Func<ChartRequestModel, IDynamicQueryCore, ResultTable>>((req, dq) => ExecuteChart<int>(req, (DynamicQueryCore<int>)dq));
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
