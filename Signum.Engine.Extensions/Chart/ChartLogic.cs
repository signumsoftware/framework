using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Chart;
using Signum.Engine.DynamicQuery;
using Signum.Utilities;
using System.Reflection;
using Signum.Utilities.Reflection;
using Signum.Entities.DynamicQuery;
using System.Linq.Expressions;
using Signum.Utilities.DataStructures;
using Signum.Utilities.ExpressionTrees;
using Signum.Entities;
using Signum.Engine.Maps;
using Signum.Engine.Basics;
using Signum.Entities.Authorization;
using Signum.Engine.Authorization;
using Signum.Entities.Reflection;
using Signum.Engine.Operations;
using System.Threading.Tasks;
using System.Threading;

namespace Signum.Engine.Chart
{
    public static class ChartLogic
    {
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                QueryLogic.Start(sb, dqm);

                PermissionAuthLogic.RegisterTypes(typeof(ChartPermission));

                ChartColorLogic.Start(sb, dqm);
                ChartScriptLogic.Start(sb, dqm);
                UserChartLogic.Start(sb, dqm);
            }
        }

        public static Task<ResultTable> ExecuteChartAsync(ChartRequest request, CancellationToken token)
        {
            IDynamicQueryCore core = DynamicQueryManager.Current.TryGetQuery(request.QueryName).Core.Value;

            return miExecuteChart.GetInvoker(core.GetType().GetGenericArguments()[0])(request, core, token);
        }

        static GenericInvoker<Func<ChartRequest, IDynamicQueryCore, CancellationToken, Task<ResultTable>>> miExecuteChart =
            new GenericInvoker<Func<ChartRequest, IDynamicQueryCore, CancellationToken, Task<ResultTable>>>((req, dq, token) => ExecuteChartAsync<int>(req, (DynamicQueryCore<int>)dq, token));
        static async Task<ResultTable> ExecuteChartAsync<T>(ChartRequest request, DynamicQueryCore<T> dq, CancellationToken token)
        {
            List<Column> columns = request.Columns.Where(c => c.Token != null).Select(t => t.CreateColumn()).ToList();

            var multiplications = request.Multiplications;;
            using (ExecutionMode.UserInterface())
            {
                if (!request.GroupResults)
                {
                    return await dq.ExecuteQueryAsync(new QueryRequest
                    {
                        QueryName = request.QueryName,
                        Columns = columns,
                        Filters = request.Filters,
                        Orders = request.Orders,
                        Pagination = new Pagination.All(),
                    }, token);
                }
                else
                {
                    return await dq.ExecuteQueryGroupAsync(new QueryGroupRequest
                    {
                        QueryName = request.QueryName,
                        Columns = columns,
                        Filters = request.Filters,
                        Orders = request.Orders
                    }, token);
                }
            }
        }
    }
}
