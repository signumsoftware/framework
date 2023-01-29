using Signum.Entities.Workflow;

namespace Signum.Engine.Workflow;

public static class WorkflowActivityMonitorLogic
{
    public static WorkflowActivityMonitor GetWorkflowActivityMonitor(WorkflowActivityMonitorRequest request)
    {
        if (request.Columns.Any(c => !(c.Token is AggregateToken)))
            throw new InvalidOperationException("Invalid columns");

        var qd = QueryLogic.Queries.QueryDescription(typeof(CaseActivityEntity));

        var filters = new List<Filter>
        {
            new FilterCondition(QueryUtils.Parse($"Entity.{nameof(CaseActivityEntity.Case)}.{nameof(CaseEntity.Workflow)}", qd, 0), FilterOperation.EqualTo, request.Workflow)
        };
        filters.AddRange(request.Filters);
        
        var columns = new List<Column>
        {
            new Column(QueryUtils.Parse($"Entity.{nameof(CaseActivityEntity.WorkflowActivity)}", qd, 0), null),
            new Column(QueryUtils.Parse($"Count", qd, SubTokensOptions.CanAggregate), null),
        };
        columns.AddRange(request.Columns);


        var rt = QueryLogic.Queries.ExecuteQuery(new QueryRequest
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
                WorkflowActivity = (Lite<IWorkflowNodeEntity>)row[0]!,
                CaseActivityCount = (int)row[1]!,
                CustomValues = row.GetValues(customCols),
            }).ToList(),
        };
    }
}

public class WorkflowActivityMonitorRequest
{
    public required Lite<WorkflowEntity> Workflow;
    public required List<Filter> Filters; // Case
    public required List<Column> Columns; // CaseActivity
}

public class WorkflowActivityStats
{
    public required Lite<IWorkflowNodeEntity> WorkflowActivity;
    public required int CaseActivityCount;
    public required object?[] CustomValues;
}

public class WorkflowActivityMonitor
{
    public required Lite<WorkflowEntity> Workflow;
    public required List<string> CustomColumns;
    public required List<WorkflowActivityStats> Activities;
}
