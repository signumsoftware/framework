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
                BpmnElementId = ca.WorkflowActivity.BpmnElementId,
                Notifications = ca.Notifications().Count(),
                StartOn = ca.StartDate,
                DoneDate = ca.DoneDate,
                DoneType = ca.DoneType,
                DoneBy = ca.DoneBy,
                Duration = ca.Duration,
                AverageDuration = averages.TryGetS(ca.WorkflowActivity.ToLite()),
                EstimatedDuration = ca.WorkflowActivity.EstimatedDuration,
            }).ToDictionary(a => a.CaseActivity);

            var gr = WorkflowLogic.WorkflowGraphLazy.Value.TryGetC(@case.Workflow.ToLite());

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

                        return conns.Select(c => new CaseConnectionStats
                        {
                            BpmnElementId = c.BpmnElementId,
                            Connection = c.ToLite(),
                            FromBpmnElementId = from.BpmnElementId,
                            ToBpmnElementId = to.BpmnElementId,
                            DoneBy = prev.DoneBy,
                            DoneDate = prev.DoneDate.Value,
                            DoneType = prev.DoneType.Value
                        });
                    }
                    else
                    {
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
                    }
                }).ToList();

            WorkflowEventEntity start = GetStartEvent(@case, caseActivities, gr, out Lite<IUserEntity> startUser);

            if (start != null)
            {
                var firsts = caseActivities.Values.Where(a => (a.PreviousActivity == null || !caseActivities.ContainsKey(a.PreviousActivity)));
                foreach (var f in firsts)
                {
                    connections.AddRange(GetAllConnections(gr, start, gr.Activities.GetOrThrow(f.WorkflowActivity)).Select(c => new CaseConnectionStats
                    {
                        BpmnElementId = c.BpmnElementId,
                        Connection = c.ToLite(),
                        FromBpmnElementId = c.From.BpmnElementId,
                        ToBpmnElementId = c.To.BpmnElementId,
                        DoneBy = startUser,
                        DoneDate = f.StartOn,
                    }));
                }
            }


            WorkflowEventEntity finish = GetFinishEvent(@case, caseActivities, gr);

            if (finish != null)
            {
                var prevs = GetPreviousActivities(@case, caseActivities, gr, finish);
                if (prevs.Any())
                {
                    foreach (var p in prevs)
                    {
                        connections.AddRange(GetAllConnections(gr, gr.Activities.GetOrThrow(p.WorkflowActivity), finish).Select(c => new CaseConnectionStats
                        {
                            BpmnElementId = c.BpmnElementId,
                            Connection = c.ToLite(),
                            FromBpmnElementId = c.From.BpmnElementId,
                            ToBpmnElementId = c.To.BpmnElementId,
                            DoneBy = p.DoneBy,
                            DoneDate = p.DoneDate.Value,
                        }));
                    }
                }
                else
                {

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

        private static List<CaseActivityStats> GetPreviousActivities(CaseEntity @case, Dictionary<Lite<CaseActivityEntity>, CaseActivityStats> caseActivities, WorkflowNodeGraph wg, WorkflowEventEntity finish)
        {
            var byWorkflowActivity = caseActivities.Values.GroupToDictionary(a => a.WorkflowActivity);


            List<CaseActivityStats> result = new List<Workflow.CaseActivityStats>();
            var stack = new Stack<IWorkflowNodeEntity>(new [] { finish });
            wg.PreviousGraph.DepthExploreConnections(stack, (prev, conn, next) =>
            {
                if (next is WorkflowActivityEntity wa)
                {
                    var act = byWorkflowActivity.TryGetC(wa.ToLite())?.Where(a => a.DoneDate.HasValue && IsNormal(a.DoneType.Value)).WithMax(a => a.DoneDate.Value);
                    if (act != null)
                        result.Add(act);
                    return false;
                }

                if (next is WorkflowEventEntity)
                    return true;

                if (next is WorkflowGatewayEntity g)
                    return g.Direction == WorkflowGatewayDirection.Join;

                throw new InvalidOperationException("Unexpected");
            });

            return result;
        }

        private static bool IsNormal(DoneType type)
        {
            return
                type == DoneType.Approve ||
                type == DoneType.Decline ||
                type == DoneType.Next || 
                type == DoneType.ScriptSuccess;
        }

        private static WorkflowEventEntity GetFinishEvent(CaseEntity @case, Dictionary<Lite<CaseActivityEntity>, CaseActivityStats> caseActivities, WorkflowNodeGraph gr)
        {
            if (!@case.FinishDate.HasValue)
                return null;

            var last = caseActivities.Values.Where(a => a.DoneDate != null).WithMax(a => a.DoneDate.Value);

            WorkflowEventEntity result = null;

            var lastAct = gr.Activities.GetOrThrow(last.WorkflowActivity);

            Stack<IWorkflowNodeEntity> stack = new Stack<IWorkflowNodeEntity>(new[] { lastAct });
            gr.NextGraph.DepthExploreConnections(stack, (prev, con, next) =>
            {
                if (next is WorkflowActivityEntity)
                    return false;

                if (next is WorkflowEventEntity we)
                {
                    if (we.Type.IsFinish())
                        result = we;
                    return false;
                }

                if (next is WorkflowGatewayEntity)
                    return true;

                throw new InvalidOperationException("Unexpected");
            });

            return result;
        }

        private static WorkflowEventEntity GetStartEvent(CaseEntity @case, Dictionary<Lite<CaseActivityEntity>, CaseActivityStats> caseActivities, WorkflowNodeGraph gr, out Lite<IUserEntity> user)
        {
            user = null;
            var wet = Database.Query<OperationLogEntity>()
            .Where(l => l.Operation == CaseActivityOperation.CreateCaseFromWorkflowEventTask.Symbol && l.Target.RefersTo(@case))
            .Select(l => new { l.Origin, l.User })
            .SingleOrDefaultEx();

            if (wet != null)
            {
                var lite = (wet.Origin as Lite<WorkflowEventTaskEntity>).InDB(a => a.Event);
                user = wet.User;
                return lite == null ? null : gr.Events.GetOrThrow(lite);
            }
            else
            {
                var firstActivity = caseActivities.Values.WithMin(a => a.StartOn)?.CaseActivity;

                user = Database.Query<OperationLogEntity>()
                   .Where(l => l.Operation == CaseActivityOperation.Register.Symbol && l.Target.Is(firstActivity) && l.Exception == null)
                   .Select(a => a.User)
                   .SingleEx();

                if (user != null)
                    return gr.Events.Values.SingleEx(a => a.Type == WorkflowEventType.Start);
                else
                    return null;
            }
        }

        private static HashSet<WorkflowConnectionEntity> GetAllConnections(WorkflowNodeGraph gr, IWorkflowNodeEntity from, IWorkflowNodeEntity to)
        {
            HashSet<WorkflowConnectionEntity> result = new HashSet<WorkflowConnectionEntity>(); 

            Stack<WorkflowConnectionEntity> partialPath = new Stack<WorkflowConnectionEntity>(); //Stack of connections, not NODES
            Action<IWorkflowNodeEntity> flood = null;
            flood = node =>
            {
                if (node.Is(to))
                    result.AddRange(partialPath);

                if (node is WorkflowActivityEntity && !node.Is(from))
                    return;
                
                foreach (var kvp in gr.NextGraph.RelatedTo(node))
                {
                    if (!partialPath.Contains(kvp.Value))
                    {
                        partialPath.Push(kvp.Value);
                        flood(kvp.Key);
                        partialPath.Pop();
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
        public int Notifications;
        public DateTime StartOn;
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
