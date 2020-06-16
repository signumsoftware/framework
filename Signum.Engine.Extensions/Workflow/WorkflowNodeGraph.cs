using Signum.Entities.Workflow;
using Signum.Engine.DynamicQuery;
using Signum.Entities;
using Signum.Utilities;
using Signum.Utilities.DataStructures;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Signum.Engine.Workflow
{
    public class WorkflowNodeGraph
    {
#pragma warning disable CS8618 // Non-nullable field is uninitialized.
        public WorkflowEntity Workflow { get; internal set; }
        public DirectedEdgedGraph<IWorkflowNodeEntity, HashSet<WorkflowConnectionEntity>> NextGraph { get; internal set; }
        public DirectedEdgedGraph<IWorkflowNodeEntity, HashSet<WorkflowConnectionEntity>> PreviousGraph { get; internal set; }

        public Dictionary<Lite<WorkflowEventEntity>, WorkflowEventEntity> Events { get; internal set; }
        public Dictionary<Lite<WorkflowActivityEntity>, WorkflowActivityEntity> Activities { get; internal set; }
        public Dictionary<Lite<WorkflowGatewayEntity>, WorkflowGatewayEntity> Gateways { get; internal set; }
        public Dictionary<Lite<WorkflowConnectionEntity>, WorkflowConnectionEntity> Connections { get; internal set; }
#pragma warning restore CS8618 // Non-nullable field is uninitialized.

        public IWorkflowNodeEntity GetNode(Lite<IWorkflowNodeEntity> lite)
        {
            if (lite is Lite<WorkflowEventEntity> we)
                return Events.GetOrThrow(we);

            if (lite is Lite<WorkflowActivityEntity> wa)
                return Activities.GetOrThrow(wa);

            if (lite is Lite<WorkflowGatewayEntity> wg)
                return Gateways.GetOrThrow(wg);

            throw new InvalidOperationException("Unexpected " + lite.EntityType);
        }

        internal List<Lite<IWorkflowNodeEntity>> Autocomplete(string subString, int count, List<Lite<IWorkflowNodeEntity>> excludes)
        {
            var events = AutocompleteUtils.Autocomplete(Events.Where(a => a.Value.Type == WorkflowEventType.Finish).Select(a => a.Key), subString, count);
            var activities = AutocompleteUtils.Autocomplete(Activities.Keys, subString, count);
            var gateways = AutocompleteUtils.Autocomplete(Gateways.Keys, subString, count);
            return new Sequence<Lite<IWorkflowNodeEntity>>()
                {
                    events,
                    activities,
                    gateways
                }
            .Except(excludes.EmptyIfNull())
            .OrderByDescending(a => a.ToString()!.Length)
            .Take(count)
            .ToList();
        }

        internal void FillGraphs()
        {
            var graph = new DirectedEdgedGraph<IWorkflowNodeEntity, HashSet<WorkflowConnectionEntity>>();

            foreach (var e in this.Events.Values) graph.Add(e);
            foreach (var a in this.Activities.Values) graph.Add(a);
            foreach (var g in this.Gateways.Values) graph.Add(g);
            foreach (var c in this.Connections.Values)
                graph.GetOrCreate(c.From, c.To).Add(c);

            this.NextGraph = graph;
            this.PreviousGraph = graph.Inverse();
        }

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
        public Dictionary<IWorkflowNodeEntity, int> TrackId;
        public Dictionary<int, IWorkflowNodeEntity> TrackCreatedBy;
#pragma warning restore CS8618 // Non-nullable field is uninitialized.

        public IWorkflowNodeEntity GetSplit(WorkflowGatewayEntity entity)
        {
            return PreviousConnections(entity).Select(a => TrackCreatedBy.GetOrThrow(TrackId.GetOrThrow(a.From))).Distinct().SingleEx()!;
        }


        public IEnumerable<WorkflowConnectionEntity> NextConnections(IWorkflowNodeEntity node)
        {
            return NextGraph.RelatedTo(node).SelectMany(a => a.Value);
        }

        public IEnumerable<WorkflowConnectionEntity> PreviousConnections(IWorkflowNodeEntity node)
        {
            return PreviousGraph.RelatedTo(node).SelectMany(a => a.Value);
        }



        public void Validate(List<WorkflowIssue> issuesContainer, Action<WorkflowGatewayEntity, WorkflowGatewayDirection> changeDirection)
        {
            List<WorkflowIssue> issues = issuesContainer;

            if (Events.Count(a => a.Value.Type.IsStart()) == 0)
                issues.AddError(null, WorkflowValidationMessage.SomeStartEventIsRequired.NiceToString());

            if (Workflow.MainEntityStrategies.Any(a => a == WorkflowMainEntityStrategy.SelectByUser || a == WorkflowMainEntityStrategy.Clone))
                if (Events.Count(a => a.Value.Type == WorkflowEventType.Start) == 0)
                    issues.AddError(null,
                        WorkflowValidationMessage.NormalStartEventIsRequiredWhenThe0Are1Or2.NiceToString(
                        Workflow.MainEntityStrategies.GetType().NiceName(),
                        WorkflowMainEntityStrategy.SelectByUser.NiceToString(),
                        WorkflowMainEntityStrategy.Clone.NiceToString()));

            if (Events.Count(a => a.Value.Type == WorkflowEventType.Start) > 1)
                foreach (var e in Events.Where(a => a.Value.Type == WorkflowEventType.Start))
                    issues.AddError(e.Value, WorkflowValidationMessage.MultipleStartEventsAreNotAllowed.NiceToString());

            var finishEventCount = Events.Count(a => a.Value.Type.IsFinish());
            if (finishEventCount == 0)
                issues.AddError(null, WorkflowValidationMessage.FinishEventIsRequired.NiceToString());

            Events.Values.ToList().ForEach(e =>
            {
                var fanIn = PreviousConnections(e).Count();
                var fanOut = NextConnections(e).Count();

                if (e.Type.IsStart())
                {
                    if (fanIn > 0)
                        issues.AddError(e, WorkflowValidationMessage._0HasInputs.NiceToString(e));
                    if (fanOut == 0)
                        issues.AddError(e, WorkflowValidationMessage._0HasNoOutputs.NiceToString(e));
                    if (fanOut > 1)
                        issues.AddError(e, WorkflowValidationMessage._0HasMultipleOutputs.NiceToString(e));

                    if (fanOut == 1)
                    {
                        var nextConn = NextConnections(e).SingleEx();

                        if (e.Type == WorkflowEventType.Start && !(nextConn.To is WorkflowActivityEntity))
                            issues.AddError(e, WorkflowValidationMessage.StartEventNextNodeShouldBeAnActivity.NiceToString());
                    }
                }

                if (e.Type.IsFinish())
                {
                    if (fanIn == 0)
                        issues.AddError(e, WorkflowValidationMessage._0HasNoInputs.NiceToString(e));
                    if (fanOut > 0)
                        issues.AddError(e, WorkflowValidationMessage._0HasOutputs.NiceToString(e));
                }

                if (e.Type.IsScheduledStart())
                {
                    var schedule = e.ScheduledTask();

                    if (schedule == null)
                        issues.AddError(e, WorkflowValidationMessage._0IsTimerStartAndSchedulerIsMandatory.NiceToString(e));

                    var wet = e.WorkflowEventTask();

                    if (wet == null)
                        issues.AddError(e, WorkflowValidationMessage._0IsTimerStartAndTaskIsMandatory.NiceToString(e));
                    else if (wet.TriggeredOn != TriggeredOn.Always)
                    {
                        if (wet.Condition?.Script == null || !wet.Condition.Script.Trim().HasText())
                            issues.AddError(e, WorkflowValidationMessage._0IsConditionalStartAndTaskConditionIsMandatory.NiceToString(e));
                    }
                }

                if (e.Type.IsTimer())
                {
                    var boundaryOutput = NextConnections(e).Only();

                    if (boundaryOutput == null || boundaryOutput.Type != ConnectionType.Normal)
                    {
                        if (e.Type == WorkflowEventType.IntermediateTimer)
                            issues.AddError(e, WorkflowValidationMessage.IntermediateTimer0ShouldHaveOneOutputOfType1.NiceToString(e, ConnectionType.Normal.NiceToString()));
                        else
                        {
                            var parentActivity = Activities.Values.Where(a => a.BoundaryTimers.Contains(e)).SingleEx();
                            issues.AddError(e, WorkflowValidationMessage.BoundaryTimer0OfActivity1ShouldHaveExactlyOneConnectionOfType2.NiceToString(e, parentActivity, ConnectionType.Normal.NiceToString()));
                        }
                    }

                    if (e.Type == WorkflowEventType.IntermediateTimer && !e.Name.HasText())
                        issues.AddError(e, WorkflowValidationMessage.IntermediateTimer0ShouldHaveName.NiceToString(e));
                }
            });

            Gateways.Values.ToList().ForEach(g =>
            {
                var fanIn = PreviousConnections(g).Count();
                var fanOut = NextConnections(g).Count();
                if (fanIn == 0)
                    issues.AddError(g, WorkflowValidationMessage._0HasNoInputs.NiceToString(g));
                if (fanOut == 0)
                    issues.AddError(g, WorkflowValidationMessage._0HasNoOutputs.NiceToString(g));

                if (fanIn == 1 && fanOut == 1)
                    issues.AddError(g, WorkflowValidationMessage._0HasJustOneInputAndOneOutput.NiceToString(g));

                var newDirection = fanOut == 1 ? WorkflowGatewayDirection.Join : WorkflowGatewayDirection.Split;
                if (g.Direction != newDirection)
                    changeDirection(g, newDirection);

                if (g.Direction == WorkflowGatewayDirection.Split)
                {
                    if (g.Type == WorkflowGatewayType.Exclusive || g.Type == WorkflowGatewayType.Inclusive)
                    {
                        if (NextConnections(g).Any(c => IsDecision(c.Type)))
                        {
                            List<WorkflowActivityEntity> previousActivities = new List<WorkflowActivityEntity>();

                            PreviousGraph.DepthExploreConnections(g, (prev, conn, next) =>
                            {
                                if (next is WorkflowActivityEntity a)
                                {
                                    previousActivities.Add(a);
                                    return false;
                                }

                                return true;
                            });

                            foreach (var act in previousActivities.Where(a => a.Type != WorkflowActivityType.Decision))
                                issues.AddError(act, WorkflowValidationMessage.Activity0ShouldBeDecision.NiceToString(act));
                        }
                    }

                    switch (g.Type)
                    {
                        case WorkflowGatewayType.Exclusive:
                            if (NextConnections(g).OrderByDescending(a => a.Order).Skip(1).Any(c => c.Type == ConnectionType.Normal && c.Condition == null))
                                issues.AddError(g, WorkflowValidationMessage.Gateway0ShouldHasConditionOrDecisionOnEachOutputExceptTheLast.NiceToString(g));
                            break;
                        case WorkflowGatewayType.Inclusive:
                            if (NextConnections(g).Count(c => c.Type == ConnectionType.Normal && c.Condition == null) != 1)
                                issues.AddError(g, WorkflowValidationMessage.InclusiveGateway0ShouldHaveOneConnectionWithoutCondition.NiceToString(g));

                            break;
                        case WorkflowGatewayType.Parallel:
                            if (NextConnections(g).Count() == 0)
                                issues.AddError(g, WorkflowValidationMessage.ParallelSplit0ShouldHaveAtLeastOneConnection.NiceToString(g));

                            if (NextConnections(g).Any(a => a.Type != ConnectionType.Normal || a.Condition != null))
                                issues.AddError(g, WorkflowValidationMessage.ParallelSplit0ShouldHaveOnlyNormalConnectionsWithoutConditions.NiceToString(g));
                            break;
                        default:
                            break;
                    }
                }
            });

            var starts = Events.Values.Where(a => a.Type.IsStart()).ToList();
            TrackId = starts.ToDictionary(a => (IWorkflowNodeEntity)a, a => 0);
            TrackCreatedBy = new Dictionary<int, IWorkflowNodeEntity> { { 0, null! } };


            Queue<IWorkflowNodeEntity> queue = new Queue<IWorkflowNodeEntity>();
            queue.EnqueueRange(starts);
            while (queue.Count > 0)
            {
                IWorkflowNodeEntity node = queue.Dequeue();

                var nextConns = NextConnections(node).ToList(); //Clone;
                if (node is WorkflowActivityEntity wa && wa.BoundaryTimers.Any())
                {
                    foreach (var bt in wa.BoundaryTimers)
                    {
                        nextConns.AddRange(NextConnections(bt));
                    }
                }

                foreach (var con in nextConns)
                {
                    if (ContinueExplore(node, con, con.To))
                        queue.Enqueue(con.To);
                }
            }


            bool ContinueExplore(IWorkflowNodeEntity prev, WorkflowConnectionEntity conn, IWorkflowNodeEntity next)
            {
                var prevTrackId = TrackId.GetOrThrow(prev);
                int newTrackId;

                if (IsParallelGateway(prev, WorkflowGatewayDirection.Split))
                {
                    if (IsParallelGateway(next, WorkflowGatewayDirection.Join))
                        newTrackId = prevTrackId;
                    else
                    {
                        newTrackId = TrackCreatedBy.Count + 1;
                        TrackCreatedBy.Add(newTrackId, (WorkflowGatewayEntity)prev);
                    }
                }
                else if (prev is WorkflowActivityEntity act && act.BoundaryTimers.Any(bt => bt.Type == WorkflowEventType.BoundaryForkTimer))
                {
                    if (IsParallelGateway(next, WorkflowGatewayDirection.Join))
                        newTrackId = prevTrackId;
                    else
                    {
                        if (conn.From is WorkflowEventEntity ev && ev.Type == WorkflowEventType.BoundaryForkTimer)
                        {
                            newTrackId = TrackCreatedBy.Count + 1;
                            TrackCreatedBy.Add(newTrackId, act);
                        }
                        else
                        {
                            var mainTrackId = NextConnections(act)
                                .Concat(act.BoundaryTimers.Where(a => a.Type == WorkflowEventType.BoundaryInterruptingTimer).SelectMany(we => NextConnections(we)))
                                .Select(c => TrackId.TryGetS(c.To))
                                .Where(c => c != null)
                                .Distinct()
                                .SingleOrDefaultEx();

                            if (mainTrackId.HasValue)
                            {
                                newTrackId = mainTrackId.Value;
                            }
                            else
                            {
                                newTrackId = TrackCreatedBy.Count + 1;
                                TrackCreatedBy.Add(newTrackId, act);
                            }
                        }

                    }
                }
                else if (IsParallelGateway(next, WorkflowGatewayDirection.Join))
                {
                    var split = TrackCreatedBy.TryGetC(prevTrackId);
                    if (split == null)
                    {
                        issues.Add(new WorkflowIssue(WorkflowIssueType.Warning, conn.BpmnElementId, WorkflowValidationMessage._0CanNotBeConnectedToAParallelJoinBecauseHasNoPreviousParallelSplit.NiceToString(prev)));
                        return false;
                    }


                    var join = (WorkflowGatewayEntity)next;
                    var splitType = split is WorkflowGatewayEntity wg ? wg.Type :
                        split is WorkflowActivityEntity ? WorkflowGatewayType.Inclusive :
                        throw new UnexpectedValueException(split);

                    if (join.Type != splitType)
                    {
                        string message = WorkflowValidationMessage.Join0OfType1DoesNotMatchWithItsPairTheSplit2OfType3.NiceToString(join, join.Type, split, splitType);
                        issues.AddError(split, message);
                        issues.AddError(join, message);
                    }

                    newTrackId = TrackId.GetOrThrow(split);
                }
                else
                    newTrackId = prevTrackId;


                if (TrackId.ContainsKey(next))
                {
                    if (TrackId[next] != newTrackId)
                        issues.Add(new WorkflowIssue(WorkflowIssueType.Warning, conn.BpmnElementId, WorkflowValidationMessage._0Track1CanNotBeConnectedTo2Track3InsteadOfTrack4.NiceToString(prev, prevTrackId, next, TrackId[next], newTrackId)));

                    return false;
                }
                else
                {
                    TrackId[next] = newTrackId;
                    return true;
                }
            }


            foreach (var wa in Activities.Values)
            {
                var fanIn = PreviousConnections(wa).Count();
                var fanOut = NextConnections(wa).Count(v => IsNormalOrDecision(v.Type));

                if (fanIn == 0)
                    issues.AddError(wa, WorkflowValidationMessage._0HasNoInputs.NiceToString(wa));
                if (fanOut == 0)
                    issues.AddError(wa, WorkflowValidationMessage._0HasNoOutputs.NiceToString(wa));
                if (fanOut > 1)
                    issues.AddError(wa, WorkflowValidationMessage._0HasMultipleOutputs.NiceToString(wa));

                if (fanOut == 1 && wa.Type == WorkflowActivityType.Decision)
                {
                    var nextConn = NextConnections(wa).Where(c => c.Type == ConnectionType.Normal).SingleEx();
                    if (!(nextConn.To is WorkflowGatewayEntity) || ((WorkflowGatewayEntity)nextConn.To).Type == WorkflowGatewayType.Parallel)
                        issues.AddError(wa, WorkflowValidationMessage.Activity0WithDecisionTypeShouldGoToAnExclusiveOrInclusiveGateways.NiceToString(wa));
                }

                if (wa.Type == WorkflowActivityType.Script)
                {
                    var scriptException = NextConnections(wa).Where(a => a.Type == ConnectionType.ScriptException).Only();

                    if (scriptException == null)
                        issues.AddError(wa, WorkflowValidationMessage.Activity0OfType1ShouldHaveExactlyOneConnectionOfType2.NiceToString(wa, wa.Type.NiceToString(), ConnectionType.ScriptException.NiceToString()));
                }
                else
                {
                    if (NextConnections(wa).Any(a => a.Type == ConnectionType.ScriptException))
                        issues.AddError(wa, WorkflowValidationMessage.Activity0OfType1CanNotHaveConnectionsOfType2.NiceToString(wa, wa.Type.NiceToString(), ConnectionType.ScriptException.NiceToString()));
                }

                if (wa.Type == WorkflowActivityType.CallWorkflow || wa.Type == WorkflowActivityType.DecompositionWorkflow)
                {
                    if (NextConnections(wa).Any(a => a.Type != ConnectionType.Normal))
                        issues.AddError(wa, WorkflowValidationMessage.Activity0OfType1ShouldHaveExactlyOneConnectionOfType2.NiceToString(wa, wa.Type.NiceToString(), ConnectionType.Normal.NiceToString()));
                }
            }

            if (issues.Any(a => a.Type == WorkflowIssueType.Error))
            {
                this.TrackCreatedBy = null!;
                this.TrackId = null!;
            }
        }

        private bool IsNormalOrDecision(ConnectionType type)
        {
            return type == ConnectionType.Normal || IsDecision(type);
        }

        private bool IsDecision(ConnectionType type)
        {
            return type == ConnectionType.Approve || type == ConnectionType.Decline;
        }

        public bool IsParallelGateway(IWorkflowNodeEntity a, WorkflowGatewayDirection? direction = null)
        {
            var gateway = a as WorkflowGatewayEntity;

            return gateway != null && gateway.Type != WorkflowGatewayType.Exclusive && (direction == null || direction.Value == gateway.Direction);
        }
    }

  

    public class WorkflowIssue
    {
        public WorkflowIssueType Type;
        public string? BpmnElementId;
        public string Message;

        public WorkflowIssue(WorkflowIssueType type, string? bpmnElementId, string message)
        {
            this.Type = type;
            this.BpmnElementId = bpmnElementId;
            this.Message = message;
        }

        public override string ToString()
        {
            return $"{Type}({BpmnElementId}): {Message}";
        }
    }

    public static class WorkflowIssuesExtensions
    {

        public static void AddError(this List<WorkflowIssue> issues, IWorkflowNodeEntity? node, string message)
        {
            issues.Add(new WorkflowIssue(WorkflowIssueType.Error, node?.BpmnElementId, message));
        }
    }
}
