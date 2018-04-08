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

                if (e.Type.IsTimerStart())
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
                        if (NextGraph.RelatedTo(g).OrderByDescending(a => a.Value.Order).Any(c => c.Value.DecisonResult != null))
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

                    if (g.Type == WorkflowGatewayType.Exclusive && NextGraph.RelatedTo(g).OrderByDescending(a => a.Value.Order).Skip(1).Any(c => c.Value.DecisonResult == null && c.Value.Condition == null))
                        errors.Add(WorkflowValidationMessage.Gateway0ShouldHasConditionOrDecisionOnEachOutputExceptTheLast.NiceToString(g));

                    if (g.Type == WorkflowGatewayType.Inclusive && NextGraph.RelatedTo(g).Any(c => c.Value.DecisonResult == null && c.Value.Condition == null))
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

            Action<WorkflowActivityEntity, IWorkflowTransitionTo> ValidateTransition = (WorkflowActivityEntity wa, IWorkflowTransitionTo item) =>
            {
                var activity0CanNotXTo1Because2 = (item is WorkflowJumpEmbedded || item is WorkflowScriptPartEmbedded) ?
                    WorkflowValidationMessage.Activity0CanNotJumpTo1Because2 :
                    WorkflowValidationMessage.Activity0CanNotTimeoutTo1Because2;

                var to =
                    item.To is Lite<WorkflowActivityEntity> ? (IWorkflowNodeEntity)Activities.TryGetC((Lite<WorkflowActivityEntity>)item.To) :
                    item.To is Lite<WorkflowGatewayEntity> ? (IWorkflowNodeEntity)Gateways.TryGetC((Lite<WorkflowGatewayEntity>)item.To) :
                    item.To is Lite<WorkflowEventEntity> ? (IWorkflowNodeEntity)Events.TryGetC((Lite<WorkflowEventEntity>)item.To) : null;

                if (to == null)
                    errors.Add(activity0CanNotXTo1Because2.NiceToString(wa, item.To, WorkflowValidationMessage.IsNotInWorkflow.NiceToString()));

                if (to is WorkflowEventEntity && ((WorkflowEventEntity)to).Type.IsStart())
                    errors.Add(activity0CanNotXTo1Because2.NiceToString(wa, item.To, WorkflowValidationMessage.IsStart.NiceToString()));

                if (to is WorkflowActivityEntity && to == wa)
                    errors.Add(activity0CanNotXTo1Because2.NiceToString(wa, item.To, WorkflowValidationMessage.IsSelfJumping.NiceToString()));

                if (TrackId.GetOrThrow(to) != TrackId.GetOrThrow(wa))
                    errors.Add(activity0CanNotXTo1Because2.NiceToString(wa, item.To, WorkflowValidationMessage.IsInDifferentParallelTrack.NiceToString()));
            };


            foreach (var wa in Activities.Values)
            {
                var fanIn = PreviousGraph.RelatedTo(wa).Count;
                var fanOut = NextGraph.RelatedTo(wa).Count;

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

                if (wa.Reject != null)
                {
                    var prevs = PreviousGraph.IndirectlyRelatedTo(wa, kvp => !(kvp.Key is WorkflowActivityEntity));
                    if (prevs.Any(a => a is WorkflowEventEntity && ((WorkflowEventEntity)a).Type.IsStart()))
                        errors.Add(WorkflowValidationMessage.Activity0CanNotRejectToStart.NiceToString(wa));

                    if (prevs.Any(a => IsParallelGateway(a)))
                        errors.Add(WorkflowValidationMessage.Activity0CanNotRejectToParallelGateway.NiceToString(wa));
                }

                foreach (var timer in wa.Timers)
                {
                    ValidateTransition(wa, timer);
                } 

                if (wa.Script != null)
                    ValidateTransition(wa, wa.Script);

                foreach (var item in wa.Jumps)
                {
                    ValidateTransition(wa, item);
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

        private bool IsParallelGateway(IWorkflowNodeEntity a, WorkflowGatewayDirection? direction = null)
        {
            var gateway = a as WorkflowGatewayEntity;

            return gateway != null && gateway.Type != WorkflowGatewayType.Exclusive && (direction == null || direction.Value == gateway.Direction);
        }
    }


}
