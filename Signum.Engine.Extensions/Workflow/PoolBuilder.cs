using Signum.Entities.Workflow;
using Signum.Engine.Operations;
using Signum.Entities;
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
        private partial class PoolBuilder
        {
            public XmlEntity<WorkflowPoolEntity> pool;
            private Dictionary<Lite<WorkflowLaneEntity>, LaneBuilder> lanes;
            private List<XmlEntity<WorkflowConnectionEntity>> sequenceFlows; //Contains all the connections internal to the pool INCLUDING the internals to each lane

            public PoolBuilder(WorkflowPoolEntity p, IEnumerable<LaneBuilder> laneBuilders, IEnumerable<XmlEntity<WorkflowConnectionEntity>> sequenceFlows)
            {
                this.pool = new XmlEntity<WorkflowPoolEntity>(p);
                this.lanes = laneBuilders.ToDictionary(a => a.lane.Entity.ToLite());
                this.sequenceFlows = sequenceFlows.ToList();
            }

            public void ApplyChanges(XElement processElement, Locator locator)
            {
                var sequenceFlows = processElement.Elements(bpmn + "sequenceFlow").ToDictionary(a => a.Attribute("id").Value);
                var oldSequenceFlows = this.sequenceFlows.ToDictionaryEx(a => a.bpmnElementId, "sequenceFlows");

                Synchronizer.Synchronize(sequenceFlows, oldSequenceFlows,
                 null,
                 (id, osf) =>
                 {
                     this.sequenceFlows.Remove(osf);
                     osf.Entity.Delete(WorkflowConnectionOperation.Delete);
                 },
                 (id, sf, osf) =>
                 {

                     var newFrom = locator.FindEntity(sf.Attribute("sourceRef").Value);
                     var newTo = locator.FindEntity(sf.Attribute("targetRef").Value);

                     if(!newFrom.Is(osf.Entity.From)  ||
                     !newTo.Is(osf.Entity.To))
                     {
                         osf.Entity.InDB().UnsafeUpdate()
                         .Set(a => a.From, a => newFrom)
                         .Set(a => a.To, a => newTo)
                         .Execute();

                         osf.Entity.From = newFrom!;
                         osf.Entity.To = newTo!;
                         osf.Entity.SetCleanModified(false);
                     }
                 });

                var oldLanes = this.lanes.Values.ToDictionaryEx(a => a.lane.bpmnElementId, "lanes");
                var lanes = processElement.Element(bpmn + "laneSet").Elements(bpmn + "lane").ToDictionaryEx(a => a.Attribute("id").Value);

                Synchronizer.Synchronize(lanes, oldLanes,
                    createNew: (id, l) =>
                    {
                        var wl = new WorkflowLaneEntity { Xml = new WorkflowXmlEmbedded(), Pool = this.pool.Entity }.ApplyXml(l, locator);
                        var lb = new LaneBuilder(wl,
                            Enumerable.Empty<WorkflowActivityEntity>(),
                            Enumerable.Empty<WorkflowEventEntity>(),
                            Enumerable.Empty<WorkflowGatewayEntity>(),
                            Enumerable.Empty<XmlEntity<WorkflowConnectionEntity>>());
                        lb.ApplyChanges(processElement, l, locator);

                        this.lanes.Add(wl.ToLite(), lb);
                    },
                    removeOld: null,
                    merge: (id, l, ol) =>
                    {
                        var wl = ol.lane.Entity.ApplyXml(l, locator);
                        ol.ApplyChanges(processElement, l, locator);
                    });

                Synchronizer.Synchronize(lanes, oldLanes,
                       createNew: null,
                       removeOld: (id, ol) =>
                       {
                           ol.ApplyChanges(processElement, ol.lane.Element, locator);
                           this.lanes.Remove(ol.lane.Entity.ToLite());
                           ol.lane.Entity.Delete(WorkflowLaneOperation.Delete);
                       },
                       merge: null);

                Synchronizer.Synchronize(sequenceFlows, oldSequenceFlows,
                       (id, sf) =>
                       {
                           var wc = new WorkflowConnectionEntity { Xml = new WorkflowXmlEmbedded() }.ApplyXml(sf, locator);
                           this.sequenceFlows.Add(new XmlEntity<WorkflowConnectionEntity>(wc));
                       },
                       null,
                       (id, sf, osf) =>
                       {
                           osf.Entity.ApplyXml(sf, locator);
                       });
            }

            public IWorkflowNodeEntity FindEntity(string bpmElementId)
            {
                return this.lanes.Values.Select(lb => lb.FindEntity(bpmElementId)).NotNull().SingleOrDefault();
            }

            internal XElement GetParticipantElement()
            {
                return new XElement(bpmn + "participant",
                                    new XAttribute("id", pool.bpmnElementId),
                                    new XAttribute("name", pool.Entity.Name),
                                    new XAttribute("processRef", "Process_" + pool.bpmnElementId));
            }

            internal XElement GetProcessElement()
            {
                return new XElement(bpmn + "process",
                                        new XAttribute("id", "Process_" + pool.bpmnElementId),
                                        new XAttribute("isExecutable", "false"),
                                            new XElement(bpmn + "laneSet",
                                                lanes.Values.Select(l => l.GetLaneSetElement()).ToList()),
                                            lanes.Values.SelectMany(e => e.GetNodesElement()).ToList(),
                                            GetSequenceFlowsElement());
            }

            internal List<XElement> GetDiagramElements()
            {
                List<XElement> res = new List<XElement>();
                res.Add(pool.Element);
                res.AddRange(lanes.Values.SelectMany(a => a.GetDiagramElement()).ToList());
                res.AddRange(sequenceFlows.Select(a => a.Element));
                return res;
            }

            internal LaneBuilder GetLaneBuilder(Lite<WorkflowLaneEntity> l)
            {
                return lanes.GetOrThrow(l);
            }

            private List<XElement> GetSequenceFlowsElement()
            {
                return sequenceFlows.Select(a => new XElement(bpmn + "sequenceFlow",
                    new XAttribute("id", a.bpmnElementId),
                    a.Entity.Name.HasText() ? new XAttribute("name", a.Entity.Name) : null,
                    new XAttribute("sourceRef", GetLaneBuilder(a.Entity.From.Lane.ToLite()).GetBpmnElementId(a.Entity.From)),
                    new XAttribute("targetRef", GetLaneBuilder(a.Entity.To.Lane.ToLite()).GetBpmnElementId(a.Entity.To))
                )).ToList();
            }

            internal void DeleteAll(Locator? locator)
            {
                foreach (var lb in lanes.Values)
                {
                    lb.DeleteAll(locator);
                }

                this.pool.Entity.Delete(WorkflowPoolOperation.Delete);
            }

            internal void DeleteCaseActivities(Expression<Func<CaseEntity, bool>> filter)
            {
                foreach (var lb in lanes.Values)
                    lb.DeleteCaseActivities(filter);
            }

            internal IEnumerable<XmlEntity<WorkflowActivityEntity>> GetAllActivities()
            {
                return this.lanes.Values.SelectMany(la => la.GetActivities());
            }

            internal List<LaneBuilder> GetLanes()
            {
                return this.lanes.Values.ToList();
            }

            internal List<XmlEntity<WorkflowConnectionEntity>> GetSequenceFlows()
            {
                return this.sequenceFlows;
            }

            internal void Clone(WorkflowEntity wf, Dictionary<IWorkflowNodeEntity, IWorkflowNodeEntity> nodes)
            {
                var oldPool = this.pool.Entity;
                var newPool = new WorkflowPoolEntity
                {
                    Workflow = wf,
                    Name = oldPool.Name,
                    BpmnElementId = oldPool.BpmnElementId,
                    Xml = oldPool.Xml,
                }.Save();

                foreach (var lb in this.lanes.Values)
                {
                    lb.Clone(newPool, nodes);
                }
            }
        }
    }
}
