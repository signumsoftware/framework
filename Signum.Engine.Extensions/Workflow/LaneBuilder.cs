using Signum.Entities.Workflow;
using Signum.Engine;
using Signum.Entities;
using Signum.Engine.Operations;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Signum.Entities.Reflection;
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
                var laneElements = processElement.Elements().Where(a => laneIds.Contains(a.Attribute("id")?.Value));

                var events = laneElements.Where(a=>WorkflowEventTypes.Values.Contains(a.Name.LocalName)).ToDictionary(a => a.Attribute("id").Value);
                var oldEvents = this.events.Values.ToDictionaryEx(a => a.bpmnElementId, "events");

                Synchronizer.Synchronize(events, oldEvents,
                   (id, e) =>
                   {
                       var already = (WorkflowEventEntity)locator.FindEntity(id);
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
                       var already = (WorkflowActivityEntity)locator.FindEntity(id);
                       if (already != null)
                       {
                           locator.FindLane(already.Lane).activities.Remove(id);
                           already.Lane = this.lane.Entity;
                       }

                       var wa = (already ?? new WorkflowActivityEntity { Xml = new WorkflowXmlEmbedded(), Lane = this.lane.Entity }).ApplyXml(a, locator);
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
                       var we = oa.Entity.ApplyXml(a, locator);
                   });

                var gateways = laneElements
                    .Where(a => WorkflowGatewayTypes.Values.Contains(a.Name.LocalName))
                    .ToDictionary(a => a.Attribute("id").Value);
                var oldGateways = this.gateways.Values.ToDictionaryEx(a => a.bpmnElementId, "gateways");

                Synchronizer.Synchronize(gateways, oldGateways,
                   (id, g) =>
                   {
                       var already = (WorkflowGatewayEntity)locator.FindEntity(id);
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

            public IWorkflowNodeEntity FindEntity(string bpmElementId)
            {
                return this.events.TryGetC(bpmElementId)?.Entity ??
                    this.activities.TryGetC(bpmElementId)?.Entity ??
                    (IWorkflowNodeEntity)this.gateways.TryGetC(bpmElementId)?.Entity;
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
                { WorkflowEventType.TimerStart, "startEvent" },
                { WorkflowEventType.Finish, "endEvent" },
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
                return new XElement(bpmn + WorkflowEventTypes.GetOrThrow(e.Entity.Type),
                    new XAttribute("id", e.bpmnElementId),
                    e.Entity.Name.HasText() ? new XAttribute("name", e.Entity.Name) : null,
                    e.Entity.Type.IsTimerStart() ? 
                        new XElement(bpmn + (((WorkflowEventModel)e.Entity.GetModel()).Task.TriggeredOn == TriggeredOn.Always ? "timerEventDefinition" : "conditionalEventDefinition")) : null, 
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

          
            internal void DeleteAll(Locator locator)
            {
                foreach (var c in connections.Values.Select(a => a.Entity))
                {
                    c.Delete(WorkflowConnectionOperation.Delete);
                }

                foreach (var e in events.Values.Select(a => a.Entity))
                {
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

            private static void DeleteCaseActivities(WorkflowActivityEntity ac, Expression<Func<CaseEntity, bool>> filter)
            {
                if (ac.Type == WorkflowActivityType.DecompositionWorkflow || ac.Type == WorkflowActivityType.CallWorkflow)
                {
                    var sw = ac.SubWorkflow.Workflow;
                    var wb = new WorkflowBuilder(sw);
                    wb.DeleteCases(c => filter.Evaluate(c.ParentCase) && c.ParentCase.Workflow == ac.Lane.Pool.Workflow);
                }

                var caseActivities = ac.CaseActivities().Where(ca => filter.Evaluate(ca.Case));
                if (caseActivities.Any())
                {
                    var notifications = caseActivities.SelectMany(a => a.Notifications());

                    if (notifications.Any())
                        notifications.UnsafeDelete();

                    Database.Query<CaseActivityEntity>()
                        .Where(ca => ca.Previous.Entity.WorkflowActivity.Is(ac) && filter.Evaluate(ca.Previous.Entity.Case))
                        .UnsafeUpdate()
                        .Set(ca => ca.Previous, ca => null)
                        .Execute();

                    caseActivities.UnsafeDelete();
                }
            }

            private static void MoveCasesAndDelete(WorkflowActivityEntity wa, Locator locator)
            {
                if (wa.CaseActivities().Any())
                {
                    if (locator.HasReplacement(wa.ToLite()))
                    {
                        wa.CaseActivities()
                            .UnsafeUpdate()
                            .Set(ca => ca.WorkflowActivity, ca => locator.GetReplacement(wa.ToLite()))
                            .Execute();
                    }
                    else
                    {
                        DeleteCaseActivities(wa, a => true);
                    }
                }

                wa.Delete(WorkflowActivityOperation.Delete);
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
                    ActorsEval = oldLane.ActorsEval != null ? oldLane.ActorsEval.Clone() : null,
                    Xml = oldLane.Xml,
                }.Save();

                var newActivities = this.activities.Values.Select(a => a.Entity).ToDictionary(a => a, a => new WorkflowActivityEntity
                {
                    Lane = newLane,
                    Name = a.Name,
                    BpmnElementId = a.BpmnElementId,
                    Xml = a.Xml,
                    Type = a.Type,
                    ViewName = a.ViewName,
                    RequiresOpen = a.RequiresOpen,
                    Reject = a.Reject,
                    Timers = a.Timers.Select(t => t.Clone()).ToMList(),
                    EstimatedDuration = a.EstimatedDuration,
                    Jumps = a.Jumps.Select(j => j.Clone()).ToMList(),
                    Script = a.Script?.Clone(),
                    SubWorkflow = a.SubWorkflow?.Clone(),
                    UserHelp = a.UserHelp,
                    Comments = a.Comments,
                });
                newActivities.Values.SaveList();
                nodes.AddRange(newActivities.ToDictionary(kvp => (IWorkflowNodeEntity)kvp.Key, kvp => (IWorkflowNodeEntity)kvp.Value));

                var newEvents = this.events.Values.Select(e => e.Entity).ToDictionary(e => e, e => new WorkflowEventEntity
                {
                    Lane = newLane,
                    Name = e.Name,
                    BpmnElementId = e.BpmnElementId,
                    Type = e.Type,
                    Xml = e.Xml,
                });
                newEvents.Values.SaveList();
                nodes.AddRange(newEvents.ToDictionary(kvp => (IWorkflowNodeEntity)kvp.Key, kvp => (IWorkflowNodeEntity)kvp.Value));

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
