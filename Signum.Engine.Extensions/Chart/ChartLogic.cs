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

namespace Signum.Engine.Chart
{
    public static class ChartLogic
    {
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                QueryLogic.Start(sb);

                PermissionAuthLogic.RegisterTypes(typeof(ChartPermission));

                ChartColorLogic.Start(sb, dqm);
                ChartScriptLogic.Start(sb, dqm);
                UserChartLogic.Start(sb, dqm);
            }
        }

        public static ResultTable ExecuteChart(ChartRequest request)
        {
            IDynamicQueryCore core = DynamicQueryManager.Current.TryGetQuery(request.QueryName).Core.Value;

            using (ExecutionMode.UserInterface())
                return miExecuteChart.GetInvoker(core.GetType().GetGenericArguments()[0])(request, core);
        }

        static GenericInvoker<Func<ChartRequest, IDynamicQueryCore, ResultTable>> miExecuteChart =
            new GenericInvoker<Func<ChartRequest, IDynamicQueryCore, ResultTable>>((req, dq) => ExecuteChart<int>(req, (DynamicQueryCore<int>)dq));
        static ResultTable ExecuteChart<T>(ChartRequest request, DynamicQueryCore<T> dq)
        {
            List<Column> columns = request.Columns.Where(c => c.Token != null).Select(t => t.CreateColumn()).ToList();

            var multiplications = request.Multiplications;;

            if (!request.GroupResults)
            {
                return dq.ExecuteQuery(new QueryRequest
                {
                    QueryName = request.QueryName,
                    Columns = columns,
                    Filters = request.Filters,
                    Orders = request.Orders,
                    Pagination = new Pagination.All(),
                });
            }
            else
            {
                return dq.ExecuteQueryGroup(new QueryGroupRequest
                {
                    QueryName = request.QueryName,
                    Columns = columns, 
                    Filters = request.Filters,
                    Orders = request.Orders
                }); 
            }
        }
    }
}
