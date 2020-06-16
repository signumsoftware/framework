using Signum.Entities.Workflow;
using Signum.Entities;
using Signum.Engine.Operations;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Linq.Expressions;

namespace Signum.Engine.Workflow
{
    public partial class WorkflowBuilder
    {
        internal partial class LaneBuilder
        {
            public XmlEntity<WorkflowLaneEntity> lane;
            private Dictionary<string, XmlEntity<WorkflowEventEntity>> events;
            private Dictionary<string, XmlEntity<WorkflowActivityEntity>> activities;
            private Dictionary<string, XmlEntity<WorkflowGatewayEntity>> gateways;
            private Dictionary<string, XmlEntity<WorkflowConnectionEntity>> connections;
            private Dictionary<Lite<IWorkflowNodeEntity>, List<XmlEntity<WorkflowConnectionEntity>>> incoming;
            private Dictionary<Lite<IWorkflowNodeEntity>, List<XmlEntity<WorkflowConnectionEntity>>> outgoing;

            public LaneBuilder(WorkflowLaneEntity l, 
                IEnumerable<WorkflowActivityEntity> activities,
                IEnumerable<WorkflowEventEntity> events,
                IEnumerable<WorkflowGatewayEntity> gateways,
                IEnumerable<XmlEntity<WorkflowConnectionEntity>> connections)
            {
                this.lane = new XmlEntity<WorkflowLaneEntity>(l);
                this.events = events.Select(a => new XmlEntity<WorkflowEventEntity>(a)).ToDictionary(x => x.bpmnElementId);
                this.activities = activities.Select(a => new XmlEntity<WorkflowActivityEntity>(a)).ToDictionary(x => x.bpmnElementId);
                this.gateways = gateways.Select(a => new XmlEntity<WorkflowGatewayEntity>(a)).ToDictionary(x => x.bpmnElementId);
                this.connections = connections.ToDictionary(a => a.bpmnElementId);
                this.outgoing = this.connections.Values.GroupToDictionary(a => a.Entity.From.ToLite());
                this.incoming = this.connections.Values.GroupToDictionary(a => a.Entity.To.ToLite());
            }
            
            public void ApplyChanges(XElement processElement, XElement laneElement, Locator locator)
            {
                var laneIds = laneElement.Elements(bpmn + "flowNodeRef").Select(a => a.Value).ToHashSet();
                var laneElements = processElement.Elements().Where(a => laneIds.Contains(a.Attribute("id")?.Value!));

                var events = laneElements.Where(a => WorkflowEventTypes.Where(kvp => !kvp.Key.IsBoundaryTimer()).ToDictionary().Values.Contains(a.Name.LocalName)).ToDictionary(a => a.Attribute("id").Value);
                var oldEvents = this.events.Values.Where(a => a.Entity.BoundaryOf == null).ToDictionaryEx(a => a.bpmnElementId, "events");

                Synchronizer.Synchronize(events, oldEvents,
                   (id, e) =>
                   {
                       var already = (WorkflowEventEntity?)locator.FindEntity(id);
                       if (already != null)
                       {
                           locator.FindLane(already.Lane).events.Remove(id);
                           already.Lane = this.lane.Entity;
                       }

                       var we = (already ?? new WorkflowEventEntity { Xml = new WorkflowXmlEmbedded(), Lane = this.lane.Entity }).ApplyXml(e, locator);
                       this.events.Add(id, new XmlEntity<WorkflowEventEntity>(we));
                   },
                   (id, oe) =>
                   {
                       if (!locator.ExistDiagram(id))
                       {
                           this.events.Remove(id);
                           if (oe.Entity.Type == WorkflowEventType.IntermediateTimer)
                               MoveCasesAndDelete(oe.Entity, locator);
                           else
                               oe.Entity.Delete(WorkflowEventOperation.Delete);
                       };
                   },
                   (id, e, oe) =>
                   {
                       var we = oe.Entity.ApplyXml(e, locator);
                   });

                var activities = laneElements.Where(a => WorkflowActivityTypes.Values.Contains(a.Name.LocalName)).ToDictionary(a => a.Attribute("id").Value);
                var oldActivities = this.activities.Values.ToDictionaryEx(a => a.bpmnElementId, "activities");

                Synchronizer.Synchronize(activities, oldActivities,
                   (id, a) =>
                   {
                       var already = (WorkflowActivityEntity?)locator.FindEntity(id);
                       if (already != null)
                       {
                           locator.FindLane(already.Lane).activities.Remove(id);
                           already.Lane = this.lane.Entity;
                       }

                       var wa = (already ?? new WorkflowActivityEntity { Xml = new WorkflowXmlEmbedded(), Lane = this.lane.Entity }).ApplyXml(a, locator, this.events);
                       this.activities.Add(id, new XmlEntity<WorkflowActivityEntity>(wa));
                   },
                   (id, oa) =>
                   {
                       if (!locator.ExistDiagram(id))
                       {
                           this.activities.Remove(id);
                           MoveCasesAndDelete(oa.Entity, locator);
                       };
                   },
                   (id, a, oa) =>
                   {
                       var we = oa.Entity.ApplyXml(a, locator, this.events);
                   });

                var gateways = laneElements
                    .Where(a => WorkflowGatewayTypes.Values.Contains(a.Name.LocalName))
                    .ToDictionary(a => a.Attribute("id").Value);
                var oldGateways = this.gateways.Values.ToDictionaryEx(a => a.bpmnElementId, "gateways");

                Synchronizer.Synchronize(gateways, oldGateways,
                   (id, g) =>
                   {
                       var already = (WorkflowGatewayEntity?)locator.FindEntity(id);
                       if (already != null)
                       {
                           locator.FindLane(already.Lane).gateways.Remove(id);
                           already.Lane = this.lane.Entity;
                       }

                       var wg = (already ?? new WorkflowGatewayEntity { Xml = new WorkflowXmlEmbedded(), Lane = this.lane.Entity }).ApplyXml(g, locator);
                       this.gateways.Add(id, new XmlEntity<WorkflowGatewayEntity>(wg));
                   },
                   (id, og) =>
                   {
                       if (!locator.ExistDiagram(id))
                       {
                           this.gateways.Remove(id);
                           og.Entity.Delete(WorkflowGatewayOperation.Delete);
                       };
                   },
                   (id, g, og) =>
                   {
                       var we = og.Entity.ApplyXml(g, locator);
                   });
            }

            public IWorkflowNodeEntity? FindEntity(string bpmElementId)
            {
                return this.events.TryGetC(bpmElementId)?.Entity ??
                    this.activities.TryGetC(bpmElementId)?.Entity ??
                    (IWorkflowNodeEntity?)this.gateways.TryGetC(bpmElementId)?.Entity;
            }

            internal IEnumerable<XmlEntity<WorkflowEventEntity>> GetEvents()
            {
                return this.events.Values;
            }

            internal IEnumerable<XmlEntity<WorkflowActivityEntity>> GetActivities()
            {
                return this.activities.Values;
            }

            internal IEnumerable<XmlEntity<WorkflowGatewayEntity>> GetGateways()
            {
                return this.gateways.Values;
            }

            internal IEnumerable<XmlEntity<WorkflowConnectionEntity>> GetConnections()
            {
                return this.connections.Values;
            }

            internal bool IsEmpty()
            {
                return (!this.GetActivities().Any() && !this.GetEvents().Any() && !this.GetGateways().Any());
            }

            internal string GetBpmnElementId(IWorkflowNodeEntity node)
            {
                return (node is WorkflowEventEntity) ? events.Values.Single(a => a.Entity.Is(node)).bpmnElementId :
                    (node is WorkflowActivityEntity) ? activities.Values.Single(a => a.Entity.Is(node)).bpmnElementId :
                    (node is WorkflowGatewayEntity) ? gateways.Values.Single(a => a.Entity.Is(node)).bpmnElementId :
                    throw new InvalidOperationException(WorkflowValidationMessage.NodeType0WithId1IsInvalid.NiceToString(node.GetType().NiceName(), node.Id.ToString()));
            }

            internal XElement GetLaneSetElement()
            {
                return new XElement(bpmn + "lane",
                                        new XAttribute("id", lane.bpmnElementId),
                                        new XAttribute("name", lane.Entity.Name),
                                        events.Values.Select(e => GetLaneFlowNodeRefElement(e.bpmnElementId)),
                                        activities.Values.Select(e => GetLaneFlowNodeRefElement(e.bpmnElementId)),
                                        gateways.Values.Select(e => GetLaneFlowNodeRefElement(e.bpmnElementId)));
            }

            private XElement GetLaneFlowNodeRefElement(string bpmnElementId)
            {
                return new XElement(bpmn + "flowNodeRef", bpmnElementId);
            }

            internal List<XElement> GetNodesElement()
            {
                return events.Values.Select(e => GetEventProcessElement(e))
                        .Concat(activities.Values.Select(e => GetActivityProcessElement(e)))
                        .Concat(gateways.Values.Select(e => GetGatewayProcessElement(e))).ToList();
            }

            internal List<XElement> GetDiagramElement()
            {
                List<XElement> res = new List<XElement>();
                res.Add(lane.Element);
                res.AddRange(events.Values.Select(a => a.Element)
                                .Concat(activities.Values.Select(a => a.Element))
                                .Concat(gateways.Values.Select(a => a.Element)));
                return res;
            }

            public static Dictionary<WorkflowEventType, string> WorkflowEventTypes = new Dictionary<WorkflowEventType, string>()
            {
                { WorkflowEventType.Start, "startEvent" },
                { WorkflowEventType.ScheduledStart, "startEvent" },
                { WorkflowEventType.Finish, "endEvent" },
                { WorkflowEventType.BoundaryForkTimer, "boundaryEvent" },
                { WorkflowEventType.BoundaryInterruptingTimer, "boundaryEvent" },
                { WorkflowEventType.IntermediateTimer, "intermediateCatchEvent" },

            };

            public static Dictionary<WorkflowActivityType, string> WorkflowActivityTypes = new Dictionary<WorkflowActivityType, string>()
            {
                { WorkflowActivityType.Task, "task" },
                { WorkflowActivityType.Decision, "userTask" },
                { WorkflowActivityType.CallWorkflow, "callActivity" },
                { WorkflowActivityType.DecompositionWorkflow, "callActivity" },
                { WorkflowActivityType.Script, "scriptTask" },
            };

            public static Dictionary<WorkflowGatewayType, string> WorkflowGatewayTypes = new Dictionary<WorkflowGatewayType, string>()
            {
                { WorkflowGatewayType.Inclusive, "inclusiveGateway" },
                { WorkflowGatewayType.Parallel, "parallelGateway" },
                { WorkflowGatewayType.Exclusive, "exclusiveGateway" },
            };

            private XElement GetEventProcessElement(XmlEntity<WorkflowEventEntity> e)
            {
                var activity = e.Entity.BoundaryOf?.Let(lite => this.activities.Values.SingleEx(a => lite.Is(a.Entity))).Entity;
                
                return new XElement(bpmn + WorkflowEventTypes.GetOrThrow(e.Entity.Type),
                    new XAttribute("id", e.bpmnElementId),
                    activity != null ? new XAttribute("attachedToRef", activity.BpmnElementId) : null,
                    e.Entity.Type == WorkflowEventType.BoundaryForkTimer ? new XAttribute("cancelActivity", false) : null,
                    e.Entity.Name.HasText() ? new XAttribute("name", e.Entity.Name) : null,
                    e.Entity.Type.IsScheduledStart() || e.Entity.Type.IsTimer() ? 
                        new XElement(bpmn + ((((WorkflowEventModel)e.Entity.GetModel()).Task?.TriggeredOn == TriggeredOn.Always || (e.Entity.Type.IsTimer() && e.Entity.Timer!.Duration != null)) ? 
                            "timerEventDefinition" : "conditionalEventDefinition")) : null, 
                    GetConnections(e.Entity.ToLite()));
            }

            private XElement GetActivityProcessElement(XmlEntity<WorkflowActivityEntity> a)
            {
                return new XElement(bpmn + WorkflowActivityTypes.GetOrThrow(a.Entity.Type),
                    new XAttribute("id", a.bpmnElementId),
                    new XAttribute("name", a.Entity.Name),
                    GetConnections(a.Entity.ToLite()));
            }

            private XElement GetGatewayProcessElement(XmlEntity<WorkflowGatewayEntity> g)
            {
                return new XElement(bpmn + WorkflowGatewayTypes.GetOrThrow(g.Entity.Type),
                    new XAttribute("id", g.bpmnElementId),
                    g.Entity.Name.HasText() ? new XAttribute("name", g.Entity.Name) : null,
                    GetConnections(g.Entity.ToLite()));
            }

            private IEnumerable<XElement> GetConnections(Lite<IWorkflowNodeEntity> lite)
            {
                List<XElement> result = new List<XElement>();
                result.AddRange(incoming.TryGetC(lite).EmptyIfNull().Select(c => new XElement(bpmn + "incoming", c.bpmnElementId)));
                result.AddRange(outgoing.TryGetC(lite).EmptyIfNull().Select(c => new XElement(bpmn + "outgoing", c.bpmnElementId)));
                return result;
            }

          
            internal void DeleteAll(Locator? locator)
            {
                foreach (var c in connections.Values.Select(a => a.Entity))
                {
                    c.Delete(WorkflowConnectionOperation.Delete);
                }

                foreach (var e in events.Values.Select(a => a.Entity))
                {
                    if (e.Type == WorkflowEventType.IntermediateTimer)
                    {
                        if (locator != null)
                            MoveCasesAndDelete(e, locator);
                        else
                        {
                            DeleteCaseActivities(e, c => true);
                            e.Delete(WorkflowEventOperation.Delete);
                        }

                    }
                    else
                        e.Delete(WorkflowEventOperation.Delete);
                }

                foreach (var g in gateways.Values.Select(a => a.Entity))
                {
                    g.Delete(WorkflowGatewayOperation.Delete);
                }

                foreach (var ac in activities.Values.Select(a => a.Entity))
                {
                    if (locator != null)
                        MoveCasesAndDelete(ac, locator);
                    else
                    {
                        DeleteCaseActivities(ac, c => true);
                        ac.Delete(WorkflowActivityOperation.Delete);
                    }
                }

                this.lane.Entity.Delete(WorkflowLaneOperation.Delete);
            }

            internal void DeleteCaseActivities(Expression<Func<CaseEntity, bool>> filter)
            {
                foreach (var ac in activities.Values.Select(a => a.Entity))
                    DeleteCaseActivities(ac, filter);
            }

            private static void DeleteCaseActivities(IWorkflowNodeEntity node, Expression<Func<CaseEntity, bool>> filter)
            {
                if (node is WorkflowActivityEntity wa && (wa.Type == WorkflowActivityType.DecompositionWorkflow || wa.Type == WorkflowActivityType.CallWorkflow))
                {
                    var sw = wa.SubWorkflow!.Workflow;
                    var wb = new WorkflowBuilder(sw);
                    wb.DeleteCases(c => filter.Evaluate(c.ParentCase!) && c.ParentCase!.Workflow == wa.Lane.Pool.Workflow);
                }

                var caseActivities = node.CaseActivities().Where(ca => filter.Evaluate(ca.Case));
                if (caseActivities.Any())
                {
                    caseActivities.SelectMany(a => a.Notifications()).UnsafeDelete();

                    Database.Query<CaseActivityEntity>()
                        .Where(ca => ca.Previous!.Entity.WorkflowActivity.Is(node) && filter.Evaluate(ca.Previous.Entity.Case))
                        .UnsafeUpdate()
                        .Set(ca => ca.Previous, ca => ca.Previous!.Entity.Previous)
                        .Execute();

                    var running = caseActivities.Where(a => a.State == CaseActivityState.PendingDecision || a.State == CaseActivityState.PendingNext).ToList();

                    running.ForEach(a => {
                        if (a.Previous == null)
                            throw new ApplicationException(CaseActivityMessage.ImpossibleToDeleteCaseActivity0OnWorkflowActivity1BecauseHasNoPreviousActivity.NiceToString(a.Id, a.WorkflowActivity));

                        a.Previous.ExecuteLite(CaseActivityOperation.Undo);
                    });

                    caseActivities.UnsafeDelete();
                }
            }

            private static void MoveCasesAndDelete(IWorkflowNodeEntity node, Locator? locator)
            {
                if (node.CaseActivities().Any())
                {
                    if (locator!.HasReplacement(node.ToLite()))
                    {
                        var replacement = locator.GetReplacement(node.ToLite())!;

                        node.CaseActivities()
                            .Where(a => a.State == CaseActivityState.Done)
                            .UnsafeUpdate()
                            .Set(ca => ca.WorkflowActivity, ca => replacement)
                            .Execute();

                        var running = node.CaseActivities().Where(a => a.State == CaseActivityState.PendingDecision || a.State == CaseActivityState.PendingNext).ToList();

                        running.ForEach(a =>
                        {
                            a.Notifications().UnsafeDelete();
                            a.WorkflowActivity = replacement;
                            a.Save();
                            CaseActivityLogic.InsertCaseActivityNotifications(a);
                        });
                    }
                    else
                    {
                        DeleteCaseActivities(node, a => true);
                    }
                }

                if (node is WorkflowActivityEntity wa)
                    wa.Delete(WorkflowActivityOperation.Delete);
                else
                    ((WorkflowEventEntity)node).Delete(WorkflowEventOperation.Delete);
            }

            internal void Clone(WorkflowPoolEntity pool, Dictionary<IWorkflowNodeEntity, IWorkflowNodeEntity> nodes)
            {
                var oldLane = this.lane.Entity;
                WorkflowLaneEntity newLane = new WorkflowLaneEntity
                {
                    Pool = pool,
                    Name = oldLane.Name,
                    BpmnElementId = oldLane.BpmnElementId,
                    Actors = oldLane.Actors.ToMList(),
                    ActorsEval = oldLane.ActorsEval?.Clone(),
                    Xml = oldLane.Xml,
                }.Save();

                var newEvents = this.events.Values.Select(e => e.Entity).ToDictionary(e => e, e => new WorkflowEventEntity
                {
                    Lane = newLane,
                    Name = e.Name,
                    BpmnElementId = e.BpmnElementId,
                    Timer = e.Timer?.Clone(),
                    Type = e.Type,
                    Xml = e.Xml,
                });
                newEvents.Values.SaveList();
                nodes.AddRange(newEvents.ToDictionary(kvp => (IWorkflowNodeEntity)kvp.Key, kvp => (IWorkflowNodeEntity)kvp.Value));

                var newActivities = this.activities.Values.Select(a => a.Entity).ToDictionary(a => a, a =>
                {
                    var na = new WorkflowActivityEntity
                    {
                        Lane = newLane,
                        Name = a.Name,
                        BpmnElementId = a.BpmnElementId,
                        Xml = a.Xml,
                        Type = a.Type,
                        ViewName = a.ViewName,
                        ViewNameProps = a.ViewNameProps.Select(p=> new ViewNamePropEmbedded
                        {
                            Name = p.Name,
                            Expression = p.Expression
                        }).ToMList(),
                        RequiresOpen = a.RequiresOpen,
                        EstimatedDuration = a.EstimatedDuration,
                        Script = a.Script?.Clone(),
                        SubWorkflow = a.SubWorkflow?.Clone(),
                        UserHelp = a.UserHelp,
                        Comments = a.Comments,
                    };
                    na.BoundaryTimers  = a.BoundaryTimers.Select(t => newEvents.GetOrThrow(t)).ToMList();
                    return na;
                });
                newActivities.Values.SaveList();
                nodes.AddRange(newActivities.ToDictionary(kvp => (IWorkflowNodeEntity)kvp.Key, kvp => (IWorkflowNodeEntity)kvp.Value));
                
                var newGateways = this.gateways.Values.Select(g => g.Entity).ToDictionary(g => g, g => new WorkflowGatewayEntity
                {
                    Lane = newLane,
                    Name = g.Name,
                    BpmnElementId = g.BpmnElementId,
                    Type = g.Type,
                    Direction = g.Direction,
                    Xml = g.Xml,
                });
                newGateways.Values.SaveList();
                nodes.AddRange(newGateways.ToDictionary(kvp => (IWorkflowNodeEntity)kvp.Key, kvp => (IWorkflowNodeEntity)kvp.Value));
            }
        }
    }
}
