using Signum.Engine.DynamicQuery;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Workflow;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Engine.Workflow
{
    public static class WorkflowActivityMonitorLogic
    {
        public static WorkflowActivityMonitor GetWorkflowActivityMonitor(WorkflowActivityMonitorRequest request)
        {
            if (request.Columns.Any(c => !(c.Token is AggregateToken)))
                throw new InvalidOperationException("Invalid columns");

            var qd = DynamicQueryManager.Current.QueryDescription(typeof(CaseActivityEntity));

            var filters = new List<Filter>
            {
                new Filter(QueryUtils.Parse($"Entity.{nameof(CaseActivityEntity.Case)}.{nameof(CaseEntity.Workflow)}", qd, 0), FilterOperation.EqualTo, request.Workflow)
            };
            filters.AddRange(request.Filters);
            
            var columns = new List<Column>
            {
                new Column(QueryUtils.Parse($"Entity.{nameof(CaseActivityEntity.WorkflowActivity)}", qd, 0), null),
                new Column(QueryUtils.Parse($"Count", qd, SubTokensOptions.CanAggregate), null),
            };
            columns.AddRange(request.Columns);


            var rt = DynamicQueryManager.Current.ExecuteQuery(new QueryRequest
            {
                QueryName = typeof(CaseActivityEntity),
                GroupResults = true,
                Filters = filters,
                Columns = columns,
                Orders = new List<Order>(),
                Pagination = new Pagination.All(),
            });

            var customCols = rt.Columns.Skip(2).ToArray();

            return new WorkflowActivityMonitor
            {
                Workflow = request.Workflow,
                CustomColumns = request.Columns.Select(a => a.Token.FullKey()).ToList(),
                Activities = rt.Rows.Select(row => new WorkflowActivityStats
                {
                    WorkflowActivity = (Lite<WorkflowActivityEntity>)row[0],
                    CaseActivityCount = (int)row[1],
                    CustomValues = row.GetValues(customCols),
                }).ToList(),
            };
        }
    }

    public class WorkflowActivityMonitorRequest
    {
        public Lite<WorkflowEntity> Workflow;
        public List<Filter> Filters; // Case
        public List<Column> Columns; // CaseActivity
    }

    public class WorkflowActivityStats
    {
        public Lite<WorkflowActivityEntity> WorkflowActivity;
        public int CaseActivityCount;
        public object[] CustomValues;
    }

    public class WorkflowActivityMonitor
    {
        public Lite<WorkflowEntity> Workflow;
        public List<string> CustomColumns;
        public List<WorkflowActivityStats> Activities;
    }
}
