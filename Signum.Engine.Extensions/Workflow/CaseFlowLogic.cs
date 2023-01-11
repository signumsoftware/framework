using Signum.Entities.Basics;
using Signum.Entities.Workflow;

namespace Signum.Engine.Workflow;

public static class CaseFlowLogic
{
    public static CaseFlow GetCaseFlow(CaseEntity @case)
    {
        var averages = new Dictionary<Lite<IWorkflowNodeEntity>, double?>();            
        averages.AddRange(@case.Workflow.WorkflowActivities().Select(a => KeyValuePair.Create((Lite<IWorkflowNodeEntity>)a.ToLite(), a.AverageDuration())));
        averages.AddRange(@case.Workflow.WorkflowEvents().Where(e => e.Type == WorkflowEventType.IntermediateTimer).Select(e => KeyValuePair.Create((Lite<IWorkflowNodeEntity>)e.ToLite(), e.AverageDuration())));

        var caseActivities = @case.CaseActivities().Select(ca => new CaseActivityStats
        {
            CaseActivity = ca.ToLite(),
            PreviousActivity = ca.Previous,
            WorkflowActivity = ca.WorkflowActivity.ToLite(),
            WorkflowActivityType = (WorkflowActivityType?)(ca.WorkflowActivity as WorkflowActivityEntity)!.Type,
            WorkflowEventType = (WorkflowEventType?)(ca.WorkflowActivity as WorkflowEventEntity)!.Type,
            SubWorkflow = (ca.WorkflowActivity as WorkflowActivityEntity).Try(wa => wa.SubWorkflow).Try(sw => sw.Workflow.ToLite()),
            BpmnElementId = ca.WorkflowActivity.BpmnElementId,
            Notifications = ca.Notifications().Count(),
            StartDate = ca.StartDate,
            DoneDate = ca.DoneDate,
            DoneType = ca.DoneType,
            DoneDecision = ca.DoneDecision,
            DoneBy = ca.DoneBy,
            Duration = ca.Duration,
            AverageDuration = averages.TryGetS(ca.WorkflowActivity.ToLite()),
            EstimatedDuration = ca.WorkflowActivity is WorkflowActivityEntity ?
            ((WorkflowActivityEntity)ca.WorkflowActivity).EstimatedDuration :
            ((WorkflowEventEntity)ca.WorkflowActivity).Timer!.Duration == null ? (double?)null :
            ((WorkflowEventEntity)ca.WorkflowActivity).Timer!.Duration!.ToTimeSpan().TotalMinutes,
        }).ToDictionary(a => a.CaseActivity);

        var gr = WorkflowLogic.GetWorkflowNodeGraph(@case.Workflow.ToLite());

        IEnumerable<CaseConnectionStats>? GetSyncPaths(CaseActivityStats prev, IWorkflowNodeEntity from, IWorkflowNodeEntity to)
        {
            if (prev.DoneType == DoneType.Timeout)
            {
                if (from is WorkflowActivityEntity wa)
                {
                    var conns = wa.BoundaryTimers.Where(a => a.Type == WorkflowEventType.BoundaryInterruptingTimer)
                    .SelectMany(e => gr.GetAllConnections(e, to, path =>
                    {
                        if (prev.DoneDecision != null)
                            return IsValidPath(DoneType.Timeout, prev.DoneDecision, path);

                        return path.All(a => a.Type == ConnectionType.Normal);
                    }));

                    if (conns.Any())
                        return conns.Select(c => new CaseConnectionStats().WithConnection(c).WithDone(prev));
                }
                else if (from is WorkflowEventEntity we)
                {
                    var conns = gr.GetAllConnections(we, to, path => path.All(a => a.Type == ConnectionType.Normal)); ;
                    if (conns.Any())
                        return conns.Select(c => new CaseConnectionStats().WithConnection(c).WithDone(prev));
                }
            }
            else
            {
                var conns = gr.GetAllConnections(from, to, path => IsValidPath(prev.DoneType!.Value, prev.DoneDecision, path));

                if (conns.Any())
                    return conns.Select(c => new CaseConnectionStats().WithConnection(c).WithDone(prev));
            }

            return null;
        }


        var connections = caseActivities.Values
            .Where(cs => cs.PreviousActivity != null && caseActivities.ContainsKey(cs.PreviousActivity))
            .SelectMany(cs =>
            {
                var prev = caseActivities.GetOrThrow(cs.PreviousActivity!);
                var from = gr.GetNode(prev.WorkflowActivity);
                var to = gr.GetNode(cs.WorkflowActivity);

                if (prev.DoneType.HasValue)
                {
                    var res = GetSyncPaths(prev, from, to);
                    if (res != null)
                        return res;
                }

                if (from is WorkflowActivityEntity waFork)
                {
                    var conns = waFork.BoundaryTimers.Where(a => a.Type == WorkflowEventType.BoundaryForkTimer).SelectMany(e => gr.GetAllConnections(e, to, path => path.All(a => a.Type == ConnectionType.Normal)));
                    if (conns.Any())
                        return conns.Select(c => new CaseConnectionStats().WithConnection(c).WithDone(prev));
                }

                return new[]
                {
                    new CaseConnectionStats
                    {
                        FromBpmnElementId = from.BpmnElementId,
                        ToBpmnElementId = to.BpmnElementId,
                    }.WithDone(prev)
                };
            }).ToList();

        var isInPrevious = caseActivities.Values.Select(a => a.PreviousActivity).ToHashSet();


        connections.AddRange(caseActivities.Values
            .Where(cs => cs.DoneDate.HasValue && !isInPrevious.Contains(cs.CaseActivity))
            .Select(cs =>
            {
                var from = gr.GetNode(cs.WorkflowActivity);
                var candidates = cs.DoneType == DoneType.Timeout && from is WorkflowActivityEntity wa ?
                    wa.BoundaryTimers.SelectMany(e => gr.NextConnections(e)) :
                    gr.NextConnections(from);

                var nextConnection = candidates
                .SingleOrDefaultEx(c => IsCompatible(c.Type, cs.DoneType!.Value) && (c.DoneDecision() == null || c.DoneDecision() == cs.DoneDecision) && gr.IsParallelGateway(c.To, WorkflowGatewayDirection.Join));

                if (nextConnection != null)
                    return new CaseConnectionStats().WithConnection(nextConnection).WithDone(cs);

                return null;

            }).NotNull());
       
        var firsts = caseActivities.Values.Where(a => (a.PreviousActivity == null || !caseActivities.ContainsKey(a.PreviousActivity)));
        foreach (var f in firsts)
        {
            WorkflowEventEntity? start = GetStartEvent(@case, f.CaseActivity, gr);
            if (start != null)
                connections.AddRange(gr.GetAllConnections(start, gr.GetNode(f.WorkflowActivity), path => path.All(a => a.Type == ConnectionType.Normal))
                    .Select(c => new CaseConnectionStats().WithConnection(c).WithDone(f)));
        }

        if(@case.FinishDate != null)
        {
            var lasts = caseActivities.Values.Where(last => !caseActivities.Values.Any(a => a.PreviousActivity.Is(last.CaseActivity))).ToList();

            var ends = gr.Events.Values.Where(a => a.Type == WorkflowEventType.Finish);
            foreach (var last in lasts)
            {
                var from = gr.GetNode(last.WorkflowActivity);
                var compatibleEnds = ends.Select(end => GetSyncPaths(last, from, end)).NotNull().ToList();

                if (compatibleEnds.Count != 0)
                {
                    foreach (var path in compatibleEnds)
                    {
                        connections.AddRange(path);
                    }
                }
                else //Cancel Case
                {
                    var firstEnd = ends.FirstOrDefault();
                    if(firstEnd != null)
                    {
                        connections.Add(new CaseConnectionStats
                        {
                            FromBpmnElementId = from.BpmnElementId,
                            ToBpmnElementId = firstEnd.BpmnElementId,
                        }.WithDone(last));
                    }
                }
            }
        }

        return new CaseFlow
        {
            Activities = caseActivities.Values.GroupToDictionary(a => a.BpmnElementId),
            Connections = connections.Where(a => a.BpmnElementId != null).GroupToDictionary(a => a.BpmnElementId!),
            Jumps = connections.Where(a => a.BpmnElementId == null).ToList(),
            AllNodes = connections.Select(a => a.FromBpmnElementId!)
            .Union(connections.Select(a => a.ToBpmnElementId!)).ToList()
        };
    }

    private static bool IsCompatible(ConnectionType type, DoneType doneType)
    {
        switch (doneType)
        {
            case DoneType.Next:return type == ConnectionType.Normal;
            case DoneType.Jump: return type == ConnectionType.Jump;
            case DoneType.Timeout: return type == ConnectionType.Normal;
            case DoneType.ScriptSuccess: return type == ConnectionType.Normal;
            case DoneType.ScriptFailure: return type == ConnectionType.ScriptException;
            case DoneType.Recompose: return type == ConnectionType.Normal;
            default: throw new UnexpectedValueException(doneType);
        }
    }

    private static bool IsValidPath(DoneType doneType, string? doneDecision, Stack<WorkflowConnectionEntity> path)
    {
        switch (doneType)
        {
            case DoneType.Next: 
            case DoneType.ScriptSuccess:
            case DoneType.Recompose:
            case DoneType.Timeout:
                return path.All(a => a.Type == ConnectionType.Normal || doneDecision != null && (a.DoneDecision() == doneDecision));
            case DoneType.Jump: return path.All(a => a.Is(path.FirstEx()) ? a.Type == ConnectionType.Jump : a.Type == ConnectionType.Normal);
            case DoneType.ScriptFailure: return path.All(a => a.Is(path.FirstEx()) ? a.Type == ConnectionType.ScriptException : a.Type == ConnectionType.Normal);
            default:
                throw new InvalidOperationException();
        }

    }
    

    private static WorkflowEventEntity? GetStartEvent(CaseEntity @case, Lite<CaseActivityEntity> firstActivity, WorkflowNodeGraph gr)
    {
        var wet = Database.Query<OperationLogEntity>()
        .Where(l => l.Operation.Is(CaseActivityOperation.CreateCaseFromWorkflowEventTask.Symbol) && l.Target.Is(@case))
        .Select(l => new { l.Origin, l.User })
        .SingleOrDefaultEx();

        if (wet != null)
        {
            var lite = ((Lite<WorkflowEventTaskEntity>)wet.Origin!).InDB(a => a.Event);
            return lite == null ? null : gr.Events.GetOrThrow(lite);
        }
        
        bool register = Database.Query<OperationLogEntity>()
           .Where(l => l.Operation.Is(CaseActivityOperation.Register.Symbol) && l.Target.Is(firstActivity) && l.Exception == null)
           .Any();

        if (register)
            return gr.Events.Values.SingleEx(a => a.Type == WorkflowEventType.Start);
        
        return gr.Events.Values.Where(a => a.Type.IsStart()).Only();
    }
}

public class CaseActivityStats
{
    public required Lite<CaseActivityEntity> CaseActivity;
    public required Lite<CaseActivityEntity>? PreviousActivity;
    public required Lite<IWorkflowNodeEntity> WorkflowActivity;
    public required WorkflowActivityType? WorkflowActivityType;
    public required WorkflowEventType? WorkflowEventType;
    public required Lite<WorkflowEntity>? SubWorkflow;
    public required int Notifications;
    public required DateTime StartDate;
    public required DateTime? DoneDate;
    public required DoneType? DoneType;
    public required string? DoneDecision;
    public required Lite<IUserEntity>? DoneBy;
    public required double? Duration;
    public required double? AverageDuration;
    public required double? EstimatedDuration;

    public required string BpmnElementId;
}

public class CaseConnectionStats
{
    public CaseConnectionStats WithConnection(WorkflowConnectionEntity c)
    {
        this.BpmnElementId = c.BpmnElementId;
        this.Connection = c.ToLite();
        this.FromBpmnElementId = c.From.BpmnElementId;
        this.ToBpmnElementId = c.To.BpmnElementId;
        return this;
    }

    public CaseConnectionStats WithDone(CaseActivityStats activity)
    {
        this.DoneBy = activity.DoneBy;
        this.DoneDate = activity.DoneDate;
        this.DoneType = activity.DoneType;
        this.DoneDecision = activity.DoneDecision;
        return this;
    }

    public Lite<WorkflowConnectionEntity>? Connection;
    public DateTime? DoneDate;
    public Lite<IUserEntity>? DoneBy;
    public DoneType? DoneType;

    public string? DoneDecision { get; private set; }
    public string? BpmnElementId { get; internal set; }
    public string? FromBpmnElementId { get; internal set; }
    public string? ToBpmnElementId { get; internal set; }

    public override string ToString() => $"{FromBpmnElementId} =({DoneType})=> {ToBpmnElementId}";
}

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
public class CaseFlow
{
    public Dictionary<string, List<CaseActivityStats>> Activities;
    public Dictionary<string, List<CaseConnectionStats>> Connections;
    public List<CaseConnectionStats> Jumps;
    public List<string> AllNodes;
}
#pragma warning restore CS8618 // Non-nullable field is uninitialized.
