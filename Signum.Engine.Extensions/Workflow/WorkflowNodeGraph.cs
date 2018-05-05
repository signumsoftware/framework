using Signum.Entities.Workflow;
using Signum.Engine;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Operations;
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
        public WorkflowEntity Workflow { get; internal set; }
        public DirectedEdgedGraph<IWorkflowNodeEntity, WorkflowConnectionEntity> NextGraph { get; internal set; }
        public DirectedEdgedGraph<IWorkflowNodeEntity, WorkflowConnectionEntity> PreviousGraph { get; internal set; }

        public Dictionary<Lite<WorkflowEventEntity>, WorkflowEventEntity> Events { get; internal set; }
        public Dictionary<Lite<WorkflowActivityEntity>, WorkflowActivityEntity> Activities { get; internal set; }
        public Dictionary<Lite<WorkflowGatewayEntity>, WorkflowGatewayEntity> Gateways { get; internal set; }
        public Dictionary<Lite<WorkflowConnectionEntity>, WorkflowConnectionEntity> Connections { get; internal set; }

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
            .OrderByDescending(a => a.ToString().Length)
            .Take(count)
            .ToList();
        }

        internal void FillGraphs()
        {
            var graph = new DirectedEdgedGraph<IWorkflowNodeEntity, WorkflowConnectionEntity>();

            foreach (var e in this.Events.Values) graph.Add(e);
            foreach (var a in this.Activities.Values) graph.Add(a);
            foreach (var g in this.Gateways.Values) graph.Add(g);
            foreach (var c in this.Connections.Values) graph.Add(c.From, c.To, c);

            this.NextGraph = graph;
            this.PreviousGraph = graph.Inverse();
        }

        //Split -> Join
        public Dictionary<WorkflowGatewayEntity, WorkflowGatewayEntity> ParallelWorkflowPairs;
        public Dictionary<IWorkflowNodeEntity, int> TrackId;
        public Dictionary<int, WorkflowGatewayEntity> TrackCreatedBy;

        public List<string> Validate(Action<WorkflowGatewayEntity, WorkflowGatewayDirection> changeDirection)
        {
            List<string> errors = new List<string>();

            if (Events.Count(a => a.Value.Type.IsStart()) == 0)
                errors.Add(WorkflowValidationMessage.SomeStartEventIsRequired.NiceToString());

            if (Workflow.MainEntityStrategy != WorkflowMainEntityStrategy.CreateNew)
                if (Events.Count(a => a.Value.Type == WorkflowEventType.Start) == 0)
                    errors.Add(WorkflowValidationMessage.NormalStartEventIsRequiredWhenThe0Are1Or2.NiceToString(
                        Workflow.MainEntityStrategy.GetType().NiceName(), 
                        WorkflowMainEntityStrategy.SelectByUser.NiceToString(), 
                        WorkflowMainEntityStrategy.Both.NiceToString()));

            if (Events.Count(a => a.Value.Type == WorkflowEventType.Start) > 1)
                errors.Add(WorkflowValidationMessage.MultipleStartEventsAreNotAllowed.NiceToString());

            var finishEventCount = Events.Count(a => a.Value.Type.IsFinish());
            if (finishEventCount == 0)
                errors.Add(WorkflowValidationMessage.FinishEventIsRequired.NiceToString());

            Events.Values.ToList().ForEach(e =>
            {
                var fanIn = PreviousGraph.RelatedTo(e).Count;
                var fanOut = NextGraph.RelatedTo(e).Count;

                if (e.Type.IsStart())
                {
                    if (fanIn > 0)
                        errors.Add(WorkflowValidationMessage._0HasInputs.NiceToString(e));
                    if (fanOut == 0)
                        errors.Add(WorkflowValidationMessage._0HasNoOutputs.NiceToString(e));
                    if (fanOut > 1)
                        errors.Add(WorkflowValidationMessage._0HasMultipleOutputs.NiceToString(e));

                    if (fanOut == 1)
                    {
                        var nextConn = NextGraph.RelatedTo(e).Single().Value;

                        if (e.Type == WorkflowEventType.Start && !(nextConn.To is WorkflowActivityEntity))
                            errors.Add(WorkflowValidationMessage.StartEventNextNodeShouldBeAnActivity.NiceToString());
                    }
                }

                if (e.Type.IsFinish())
                {
                    if (fanIn == 0)
                        errors.Add(WorkflowValidationMessage._0HasNoInputs.NiceToString(e));
                    if (fanOut > 0)
                        errors.Add(WorkflowValidationMessage._0HasOutputs.NiceToString(e));
                }

                if (e.Type.IsScheduledStart())
                {
                    var schedule = e.ScheduledTask();

                    if (schedule == null)
                        errors.Add(WorkflowValidationMessage._0IsTimerStartAndSchedulerIsMandatory.NiceToString(e));

                    var wet = e.WorkflowEventTask();

                    if (wet == null)
                        errors.Add(WorkflowValidationMessage._0IsTimerStartAndTaskIsMandatory.NiceToString(e));
                    else if (wet.TriggeredOn != TriggeredOn.Always)
                    {
                        if (wet.Condition?.Script == null || !wet.Condition.Script.Trim().HasText())
                            errors.Add(WorkflowValidationMessage._0IsConditionalStartAndTaskConditionIsMandatory.NiceToString(e));
                    }
                }

                if (e.Type.IsTimer())
                {
                    var boundaryOutput = NextGraph.RelatedTo(e).Where(c => c.Value.Type == ConnectionType.Normal).Only().Value;

                    if (boundaryOutput == null)
                    {
                        if (e.Type == WorkflowEventType.IntermediateTimer)
                            errors.Add(WorkflowValidationMessage.IntermediateTimer0ShouldHaveOneOutputOfType1.NiceToString(e, ConnectionType.Normal.NiceToString()));
                        else
                        {
                            var wa = Activities.Values.Where(a => a.BoundaryTimers.Contains(e)).SingleEx();
                            errors.Add(WorkflowValidationMessage.BoundaryTimer0OfActivity1ShouldHaveExactlyOneConnectionOfType2.NiceToString(e, wa, ConnectionType.Normal.NiceToString()));
                        }
                    }
                }
            });

            Gateways.Values.ToList().ForEach(g =>
            {
                var fanIn = PreviousGraph.RelatedTo(g).Count;
                var fanOut = NextGraph.RelatedTo(g).Count;
                if (fanIn == 0)
                    errors.Add(WorkflowValidationMessage._0HasNoInputs.NiceToString(g));
                if (fanOut == 0)
                    errors.Add(WorkflowValidationMessage._0HasNoOutputs.NiceToString(g));

                if (fanIn == 1 && fanOut == 1)
                    errors.Add(WorkflowValidationMessage._0HasJustOneInputAndOneOutput.NiceToString(g));

                var newDirection =  fanOut == 1 ? WorkflowGatewayDirection.Join : WorkflowGatewayDirection.Split;
                if (g.Direction != newDirection)
                    changeDirection(g, newDirection);

                if (g.Direction == WorkflowGatewayDirection.Split)
                {
                    if (g.Type == WorkflowGatewayType.Exclusive || g.Type == WorkflowGatewayType.Inclusive)
                    {
                        if (NextGraph.RelatedTo(g).Any(c => IsDecision(c.Value.Type)))
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
                                errors.Add(WorkflowValidationMessage.Activity0ShouldBeDecision.NiceToString(act));
                        }
                    }

                    if (g.Type == WorkflowGatewayType.Exclusive && NextGraph.RelatedTo(g).OrderByDescending(a => a.Value.Order).Skip(1).Any(c => !IsDecision(c.Value.Type) && c.Value.Condition == null))
                        errors.Add(WorkflowValidationMessage.Gateway0ShouldHasConditionOrDecisionOnEachOutputExceptTheLast.NiceToString(g));

                    if (g.Type == WorkflowGatewayType.Inclusive && NextGraph.RelatedTo(g).Any(c => !IsDecision(c.Value.Type) && c.Value.Condition == null))
                        errors.Add(WorkflowValidationMessage.Gateway0ShouldHasConditionOnEachOutput.NiceToString(g));
                }
            });

            var starts = Events.Values.Where(a => a.Type.IsStart()).ToList();
            TrackId = starts.ToDictionary(a => (IWorkflowNodeEntity)a, a => 0);
            TrackCreatedBy = new Dictionary<int, WorkflowGatewayEntity> { { 0, null } };

            ParallelWorkflowPairs = new Dictionary<WorkflowGatewayEntity, WorkflowGatewayEntity>();

            starts.ForEach(st =>
               NextGraph.BreadthExploreConnections(st,
                (prev, conn, next) =>
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
                    else
                    {
                        if (IsParallelGateway(next, WorkflowGatewayDirection.Join))
                        {
                            var split = TrackCreatedBy.TryGetC(prevTrackId);
                            if (split == null)
                            {
                                errors.Add(WorkflowValidationMessage._0CanNotBeConnectedToAParallelJoinBecauseHasNoPreviousParallelSplit.NiceToString(prev));
                                return false;
                            }
                            
                            ParallelWorkflowPairs[split] = (WorkflowGatewayEntity)next;

                            newTrackId =  TrackId.GetOrThrow(split);
                        }
                        else
                            newTrackId = prevTrackId;
                    }

                    if (TrackId.ContainsKey(next))
                    {
                        if (TrackId[next] != newTrackId)
                            errors.Add(WorkflowValidationMessage._0Track1CanNotBeConnectedTo2Track3InsteadOfTrack4.NiceToString(prev, prevTrackId, next, TrackId[next], newTrackId));

                        return false;
                    }
                    else
                    {
                        TrackId[next] = newTrackId;
                        return true;
                    }
                })
            );

            foreach (var wa in Activities.Values)
            {
                var fanIn = PreviousGraph.RelatedTo(wa).Count(a=> IsNormalOrDecision(a.Value.Type));
                var fanOut = NextGraph.RelatedTo(wa).Count(a => IsNormalOrDecision(a.Value.Type));

                if (fanIn == 0)
                    errors.Add(WorkflowValidationMessage._0HasNoInputs.NiceToString(wa));
                if (fanOut == 0)
                    errors.Add(WorkflowValidationMessage._0HasNoOutputs.NiceToString(wa));
                if (fanOut > 1)
                    errors.Add(WorkflowValidationMessage._0HasMultipleOutputs.NiceToString(wa));

                if (fanOut == 1 && wa.Type == WorkflowActivityType.Decision)
                {
                    var nextConn = NextGraph.RelatedTo(wa).Single().Value;
                    if (!(nextConn.To is WorkflowGatewayEntity) || ((WorkflowGatewayEntity)nextConn.To).Type == WorkflowGatewayType.Parallel)
                        errors.Add(WorkflowValidationMessage.Activity0WithDecisionTypeShouldGoToAnExclusiveOrInclusiveGateways.NiceToString(wa));
                }   
                
                if (wa.Type == WorkflowActivityType.Script)
                {
                    var scriptException = NextGraph.RelatedTo(wa).Where(a => a.Value.Type == ConnectionType.ScriptException).Only().Value;

                    if(scriptException == null)
                        errors.Add(WorkflowValidationMessage.Activity0OfType1ShouldHaveExactlyOneConnectionOfType2.NiceToString(wa, wa.Type.NiceToString(), ConnectionType.ScriptException.NiceToString()));
                }

                if (wa.Type == WorkflowActivityType.CallWorkflow || wa.Type == WorkflowActivityType.DecompositionWorkflow)
                {
                    if (NextGraph.RelatedTo(wa).Any(a => a.Value.Type != ConnectionType.Normal))
                        errors.Add(WorkflowValidationMessage.Activity0OfType1ShouldHaveExactlyOneConnectionOfType2.NiceToString(wa, wa.Type.NiceToString(), ConnectionType.Normal.NiceToString()));
                }
            }

            if (errors.HasItems())
            {
                this.TrackCreatedBy = null;
                this.TrackId = null;
                this.ParallelWorkflowPairs = null;
            }

            return errors;
        }

        private bool IsNormalOrDecision(ConnectionType type)
        {
            return type == ConnectionType.Normal || IsDecision(type);
        }

        private bool IsDecision(ConnectionType type)
        {
            return type == ConnectionType.Approve || type == ConnectionType.Decline;
        }

        private bool IsParallelGateway(IWorkflowNodeEntity a, WorkflowGatewayDirection? direction = null)
        {
            var gateway = a as WorkflowGatewayEntity;

            return gateway != null && gateway.Type != WorkflowGatewayType.Exclusive && (direction == null || direction.Value == gateway.Direction);
        }
    }


}
