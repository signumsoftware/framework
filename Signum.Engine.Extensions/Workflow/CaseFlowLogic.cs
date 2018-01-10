using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Entities.Workflow;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Engine.Workflow
{
    public static class CaseFlowLogic
    {
        public static CaseFlow GetCaseFlow(CaseEntity @case)
        {
            var averages = @case.Workflow.WorkflowActivities().Select(w => KVP.Create(w.ToLite(), w.CaseActivities().Average(a => a.Duration))).ToDictionary();
            
            var caseActivities = @case.CaseActivities().Select(ca => new CaseActivityStats
            {
                CaseActivity = ca.ToLite(),
                PreviousActivity = ca.Previous,
                WorkflowActivity = ca.WorkflowActivity.ToLite(),
                WorkflowActivityType = ca.WorkflowActivity.Type,
                SubWorkflow = ca.WorkflowActivity.SubWorkflow.Workflow.ToLite(),
                BpmnElementId = ca.WorkflowActivity.BpmnElementId,
                Notifications = ca.Notifications().Count(),
                StartDate = ca.StartDate,
                DoneDate = ca.DoneDate,
                DoneType = ca.DoneType,
                DoneBy = ca.DoneBy,
                Duration = ca.Duration,
                AverageDuration = averages.TryGetS(ca.WorkflowActivity.ToLite()),
                EstimatedDuration = ca.WorkflowActivity.EstimatedDuration,
            }).ToDictionary(a => a.CaseActivity);

            var gr = WorkflowLogic.GetWorkflowNodeGraph(@case.Workflow.ToLite());

            var connections = caseActivities.Values
                .Where(cs => cs.PreviousActivity != null && caseActivities.ContainsKey(cs.PreviousActivity))
                .SelectMany(cs =>
                {
                    var prev = caseActivities.GetOrThrow(cs.PreviousActivity);
                    var from = gr.Activities.GetOrThrow(prev.WorkflowActivity);
                    var to = gr.Activities.GetOrThrow(cs.WorkflowActivity);
                    if (IsNormal(prev.DoneType.Value))
                    {
                        var conns = GetAllConnections(gr, from, to);
                        if (conns.Any())
                            return conns.Select(c => new CaseConnectionStats
                            {
                                BpmnElementId = c.BpmnElementId,
                                Connection = c.ToLite(),
                                FromBpmnElementId = c.From.BpmnElementId,
                                ToBpmnElementId = c.To.BpmnElementId,
                                DoneBy = prev.DoneBy,
                                DoneDate = prev.DoneDate.Value,
                                DoneType = prev.DoneType.Value
                            });
                    }

                    return new[]
                    {
                        new CaseConnectionStats
                        {
                            FromBpmnElementId = from.BpmnElementId,
                            ToBpmnElementId = to.BpmnElementId,
                            DoneBy = prev.DoneBy,
                            DoneDate = prev.DoneDate.Value,
                            DoneType = prev.DoneType.Value
                        }
                    };
                }).ToList();

      

           
            var firsts = caseActivities.Values.Where(a => (a.PreviousActivity == null || !caseActivities.ContainsKey(a.PreviousActivity)));
            foreach (var f in firsts)
            {
                WorkflowEventEntity start = GetStartEvent(@case, f.CaseActivity, gr);
                connections.AddRange(GetAllConnections(gr, start, gr.Activities.GetOrThrow(f.WorkflowActivity)).Select(c => new CaseConnectionStats
                {
                    BpmnElementId = c.BpmnElementId,
                    Connection = c.ToLite(),
                    FromBpmnElementId = c.From.BpmnElementId,
                    ToBpmnElementId = c.To.BpmnElementId,
                    DoneBy = f.DoneBy,
                    DoneDate = f.StartDate,
                }));
            }

            if(@case.FinishDate != null)
            {
                var lasts = caseActivities.Values.Where(last => !caseActivities.Values.Any(a => a.PreviousActivity.Is(last.CaseActivity))).ToList();

                var ends = gr.Events.Values.Where(a => a.Type == WorkflowEventType.Finish);
                foreach (var last in lasts)
                {
                    foreach (var end in ends)
                    {
                        connections.AddRange(GetAllConnections(gr, gr.Activities.GetOrThrow(last.WorkflowActivity), end).Select(c => new CaseConnectionStats
                        {
                            BpmnElementId = c.BpmnElementId,
                            Connection = c.ToLite(),
                            FromBpmnElementId = c.From.BpmnElementId,
                            ToBpmnElementId = c.To.BpmnElementId,
                            DoneBy = last.DoneBy,
                            DoneDate = last.DoneDate.Value,
                        }));
                    }
                }
            }

            return new CaseFlow
            {
                Activities = caseActivities.Values.GroupToDictionary(a => a.BpmnElementId),
                Connections = connections.Where(a => a.BpmnElementId != null).GroupToDictionary(a => a.BpmnElementId),
                Jumps = connections.Where(a => a.BpmnElementId == null).ToList(),
                AllNodes = connections.Select(a => a.FromBpmnElementId).Union(connections.Select(a => a.ToBpmnElementId)).ToList()
            };
        }
        
        private static bool IsNormal(DoneType type)
        {
            return
                type == DoneType.Approve ||
                type == DoneType.Decline ||
                type == DoneType.Next || 
                type == DoneType.ScriptSuccess;
        }

        private static WorkflowEventEntity GetStartEvent(CaseEntity @case, Lite<CaseActivityEntity> firstActivity, WorkflowNodeGraph gr)
        {
            var wet = Database.Query<OperationLogEntity>()
            .Where(l => l.Operation == CaseActivityOperation.CreateCaseFromWorkflowEventTask.Symbol && l.Target.RefersTo(@case))
            .Select(l => new { l.Origin, l.User })
            .SingleOrDefaultEx();

            if (wet != null)
            {
                var lite = (wet.Origin as Lite<WorkflowEventTaskEntity>).InDB(a => a.Event);
                return lite == null ? null : gr.Events.GetOrThrow(lite);
            }
            
            bool register = Database.Query<OperationLogEntity>()
               .Where(l => l.Operation == CaseActivityOperation.Register.Symbol && l.Target.Is(firstActivity) && l.Exception == null)
               .Any();

            if (register)
                return gr.Events.Values.SingleEx(a => a.Type == WorkflowEventType.Start);
            
            return gr.Events.Values.Where(a => a.Type.IsStart()).Only();
        }

        private static HashSet<WorkflowConnectionEntity> GetAllConnections(WorkflowNodeGraph gr, IWorkflowNodeEntity from, IWorkflowNodeEntity to)
        {
            HashSet<WorkflowConnectionEntity> result = new HashSet<WorkflowConnectionEntity>(); 

            Stack<WorkflowConnectionEntity> partialPath = new Stack<WorkflowConnectionEntity>(); 
            HashSet<IWorkflowNodeEntity> visited = new HashSet<IWorkflowNodeEntity>();
            Action<IWorkflowNodeEntity> flood = null;
            flood = node =>
            {
                if (node.Is(to))
                    result.AddRange(partialPath);

                if (node is WorkflowActivityEntity && !node.Is(from))
                    return;

                
                foreach (var kvp in gr.NextGraph.RelatedTo(node).ToList())
                {
                    if (!visited.Contains(kvp.Key))
                    {
                        visited.Add(kvp.Key);
                        partialPath.Push(kvp.Value);
                        flood(kvp.Key);
                        partialPath.Pop();
                        visited.Remove(kvp.Key);
                    }
                }
            };

            flood(from);

            return result;
        }
    }

    public class CaseActivityStats
    {
        public Lite<CaseActivityEntity> CaseActivity;
        public Lite<CaseActivityEntity> PreviousActivity;
        public Lite<WorkflowActivityEntity> WorkflowActivity;
        public WorkflowActivityType WorkflowActivityType;
        public Lite<WorkflowEntity> SubWorkflow;
        public int Notifications;
        public DateTime StartDate;
        public DateTime? DoneDate;
        public DoneType? DoneType;
        public Lite<IUserEntity> DoneBy;
        public double? Duration;
        public double? AverageDuration;
        public double? EstimatedDuration;

        public string BpmnElementId { get; internal set; }
    }

    public class CaseConnectionStats
    {
        public Lite<WorkflowConnectionEntity> Connection;
        public DateTime DoneDate;
        public Lite<IUserEntity> DoneBy;
        public DoneType DoneType;

        public string BpmnElementId { get; internal set; }
        public string FromBpmnElementId { get; internal set; }
        public string ToBpmnElementId { get; internal set; }
    }

    public class CaseFlow
    {
        public Dictionary<string, List<CaseActivityStats>> Activities;
        public Dictionary<string, List<CaseConnectionStats>> Connections;
        public List<CaseConnectionStats> Jumps;
        public List<string> AllNodes;
    }
}
