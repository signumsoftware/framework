using Signum.Entities.Workflow;
using Signum.Engine;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Entities.Reflection;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Signum.Engine.Workflow
{
    public partial class WorkflowBuilder
    {
        public static readonly XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";
        public static readonly XNamespace bpmn = "http://www.omg.org/spec/BPMN/20100524/MODEL";
        public static readonly XNamespace bpmndi = "http://www.omg.org/spec/BPMN/20100524/DI";
        public static readonly XNamespace dc = "http://www.omg.org/spec/DD/20100524/DC";
        public static readonly XNamespace di = "http://www.omg.org/spec/DD/20100524/DI";
        public static readonly string targetNamespace = "http://bpmn.io/schema/bpmn";

        private Dictionary<Lite<WorkflowPoolEntity>, PoolBuilder> pools;
        private List<XmlEntity<WorkflowConnectionEntity>> messageFlows;
        private WorkflowEntity workflow;

        public WorkflowBuilder(WorkflowEntity wf)
        {
            using (HeavyProfiler.Log("WorkflowBuilder"))
            using (new EntityCache())
            {
                this.workflow = wf;

                var connections = wf.IsNew ? Enumerable.Empty<WorkflowConnectionEntity>().ToList() : wf.WorkflowConnectionsFromCache().Select(a => a.ToLite()).RetrieveFromListOfLite().ToList();
                var xmlConnections = connections.Select(a => new XmlEntity<WorkflowConnectionEntity>(a)).ToList();

                var events = wf.IsNew ? Enumerable.Empty<WorkflowEventEntity>().ToList() : wf.WorkflowEventsFromCache().Select(a => a.ToLite()).RetrieveFromListOfLite().ToList(); ;
                var activities = wf.IsNew ? Enumerable.Empty<WorkflowActivityEntity>().ToList() : wf.WorkflowActivitiesFromCache().Select(a => a.ToLite()).RetrieveFromListOfLite().ToList();
                var gateways = wf.IsNew ? Enumerable.Empty<WorkflowGatewayEntity>().ToList() : wf.WorkflowGatewaysFromCache().Select(a => a.ToLite()).RetrieveFromListOfLite().ToList();
                var nodes = events.Cast<IWorkflowNodeEntity>().Concat(activities).Concat(gateways).ToList();

                this.pools = (from n in nodes
                              group n by n.Lane into grLane
                              select new LaneBuilder(grLane.Key,
                              grLane.OfType<WorkflowActivityEntity>(),
                              grLane.OfType<WorkflowEventEntity>(),
                              grLane.OfType<WorkflowGatewayEntity>(),
                              xmlConnections.Where(c => c.Entity.From.Lane.Is(grLane.Key) || c.Entity.To.Lane.Is(grLane.Key))) into lb
                              group lb by lb.lane.Entity.Pool into grPool
                              select new PoolBuilder(grPool.Key, grPool, xmlConnections.Where(c => c.Entity.From.Lane.Pool.Is(grPool.Key) && c.Entity.To.Lane.Pool.Is(grPool.Key))))
                             .ToDictionary(pb => pb.pool.Entity.ToLite());

                this.messageFlows = xmlConnections.Where(c => !c.Entity.From.Lane.Pool.Is(c.Entity.To.Lane.Pool)).ToList();

            }
        }

        internal WorkflowModel GetWorkflowModel()
        {
            XDocument xml = GetXDocument();

            Dictionary<string, ModelEntity> dic = new Dictionary<string, ModelEntity>();

            dic.AddRange(this.pools.Values.Select(pb => pb.pool.ToModelKVP()));

            var lanes = this.pools.Values.SelectMany(pb => pb.GetLanes());
            dic.AddRange(lanes.Select(lb => lb.lane.ToModelKVP()));

            dic.AddRange(lanes.SelectMany(lb => lb.GetActivities()).Select(a => a.ToModelKVP()));

            dic.AddRange(this.messageFlows.Select(mf => mf.ToModelKVP()));
            dic.AddRange(this.pools.Values.SelectMany(pb => pb.GetSequenceFlows()).Select(sf => sf.ToModelKVP()));

            return new WorkflowModel
            {
                DiagramXml = xml.ToString(),
                Entities = dic.Select(kvp => new BpmnEntityPair { BpmnElementId = kvp.Key, Model = kvp.Value }).ToMList()
            };
        }

        public XDocument GetXDocument()
        {
            return new XDocument(
              new XDeclaration("1.0", "UTF-8", null),
                  new XElement(bpmn + "definitions",
                      new XAttribute(XNamespace.Xmlns + "bpmn", bpmn.NamespaceName),
                      new XAttribute(XNamespace.Xmlns + "bpmndi", bpmndi.NamespaceName),
                      new XAttribute(XNamespace.Xmlns + "dc", dc.NamespaceName),
                      new XAttribute(XNamespace.Xmlns + "di", di.NamespaceName),
                      new XAttribute("targetNamespace", targetNamespace),
                      new XElement(bpmn + "collaboration",
                        new XAttribute("id", "Collaboration_" + workflow.Id),
                        pools.Values.Select(a => a.GetParticipantElement()).ToList(),
                        GetMessageFlowElements()),
                      GetProcesses(),
                      new XElement(bpmndi + "BPMNDiagram",
                          new XAttribute("id", "BPMNDiagram" + workflow.Id),
                          new XElement(bpmndi + "BPMNPlane",
                              new XAttribute("id", "BPMNPlane_" + workflow.Id),
                              new XAttribute("bpmnElement", "Collaboration_" + workflow.Id),
                              GetDiagramElements()))));
        }

        internal List<XElement> GetMessageFlowElements()
        {
            return messageFlows.Select(a =>
                new XElement(bpmn + "messageFlow",
                    new XAttribute("id", a.bpmnElementId),
                    a.Entity.Name.HasText() ? new XAttribute("name", a.Entity.Name) : null,
                    new XAttribute("sourceRef", GetBpmnElementId(a.Entity.From)),
                    new XAttribute("targetRef", GetBpmnElementId(a.Entity.To))
                )
            ).ToList();
        }
        
        internal List<XElement> GetProcesses()
        {
            return pools.Values.Select(a => a.GetProcessElement()).ToList();
        }

        internal List<XElement> GetDiagramElements()
        {
            List<XElement> res = new List<XElement>();
            res.AddRange(pools.Values.SelectMany(a => a.GetDiagramElements()).ToList());
            res.AddRange(messageFlows.Select(a => a.Element));
            return res;
        }

        public void ApplyChanges(WorkflowModel model, WorkflowReplacementModel replacements)
        {
            var document = XDocument.Parse(model.DiagramXml);

            var participants = document.Descendants(bpmn + "collaboration").Elements(bpmn + "participant").ToDictionaryEx(a => a.Attribute("id").Value);
            var processElements = document.Descendants(bpmn + "process").ToDictionaryEx(a => a.Attribute("id").Value);
            var diagramElements = document.Descendants(bpmndi + "BPMNPlane").Elements().ToDictionaryEx(a => a.Attribute("bpmnElement").Value, "bpmnElement");

            if (participants.Count != processElements.Count)
                throw new InvalidOperationException(WorkflowBuilderMessage.ParticipantsAndProcessesAreNotSynchronized.NiceToString());

            var startEventCount = processElements.Values.SelectMany(a => a.Elements()).ToList().Where(a => a.Name == bpmn + "startEvent").Count();
            if (startEventCount == 0)
                throw new InvalidOperationException(WorkflowBuilderMessage.StartEventIsRequired.NiceToString());

            if (startEventCount > 1)
                throw new InvalidOperationException(WorkflowBuilderMessage.MultipleStartEventsAreNotAllowed.NiceToString());

            Locator locator = new Workflow.Locator(this, diagramElements, model, replacements);
            var oldPools = this.pools.Values.ToDictionaryEx(a => a.pool.bpmnElementId, "pools");

            Synchronizer.Synchronize(participants, oldPools,
                (id, pa) =>
                {
                    var wp = new WorkflowPoolEntity { Xml = new WorkflowXmlEntity(), Workflow = this.workflow }.ApplyXml(pa, locator);
                    var pb = new PoolBuilder(wp, Enumerable.Empty<LaneBuilder>(), Enumerable.Empty<XmlEntity<WorkflowConnectionEntity>>());
                    this.pools.Add(wp.ToLite(), pb);
                    pb.ApplyChanges(processElements.GetOrThrow(pa.Attribute("processRef").Value), locator);
                },
                (id, pb) =>
                {
                    this.pools.Remove(pb.pool.Entity.ToLite());
                    pb.DeleteAll(locator);
                },
                (id, pa, pb) =>
                {
                    var wp = pb.pool.Entity.ApplyXml(pa, locator);
                    pb.ApplyChanges(processElements.GetOrThrow(pa.Attribute("processRef").Value), locator);
                });

            var messageFlows = document.Descendants(bpmn + "collaboration").Elements(bpmn + "messageFlow").ToDictionaryEx(a => a.Attribute("id").Value);
            var oldMessageFlows = this.messageFlows.ToDictionaryEx(a => a.bpmnElementId, "messageFlows");

            Synchronizer.Synchronize(messageFlows, oldMessageFlows,
                (id, mf) =>
                {
                    var wc = new WorkflowConnectionEntity { Xml = new WorkflowXmlEntity() }.ApplyXml(mf, locator);
                    this.messageFlows.Add(new XmlEntity<WorkflowConnectionEntity>(wc));
                },
                (id, omf) =>
                {
                    this.messageFlows.Remove(omf);
                    omf.Entity.Delete(WorkflowConnectionOperation.Delete);
                },
                (id, mf, omf) =>
                {
                    omf.Entity.ApplyXml(mf, locator);
                });
        }

        internal IWorkflowNodeEntity FindEntity(string bpmElementId)
        {
            return this.pools.Values.Select(pb => pb.FindEntity(bpmElementId)).NotNull().SingleOrDefaultEx();
        }

        internal LaneBuilder FindLane(WorkflowLaneEntity lane)
        {
            return this.pools.Values.SelectMany(a => a.GetLanes()).Single(l => l.lane.Entity.Is(lane));
        }

        private string GetBpmnElementId(IWorkflowNodeEntity node)
        {
            return this.pools.GetOrThrow(node.Lane.Pool.ToLite()).GetLaneBuilder(node.Lane.ToLite()).GetBpmnElementId(node);
        }

        public PreviewResult PreviewChanges(XDocument document)
        {
            var oldTasks = this.pools.Values.SelectMany(p => p.GetAllActivities())
                .ToDictionary(a => a.bpmnElementId);

            var newElements = document.Descendants().Where(a => LaneBuilder.WorkflowActivityTypes.ContainsKey(a.Name.LocalName))
                .ToDictionary(a => a.Attribute("id").Value);

            return new PreviewResult
            {
                Model = new WorkflowReplacementModel
                {
                    Replacements = oldTasks.Where(kvp => !newElements.ContainsKey(kvp.Key) && kvp.Value.Entity.CaseActivities().Any(c => c.DoneDate == null))
                    .Select(a => new WorkflowReplacementItemEntity { OldTask = a.Value.Entity.ToLite() })
                    .ToMList(),

                },
                NewTasks = newElements.Select(a => new PreviewTask
                {
                    BpmnId = a.Key,
                    Name = a.Value.Attribute("name").Value,
                }).ToList(),
            };
        }
    }

    public class PreviewResult
    {
        public WorkflowReplacementModel Model;
        public List<PreviewTask> NewTasks;
    }

    public class PreviewTask
    {
        public string BpmnId;
        public string Name; 
    }

    public class Locator
    {
        WorkflowBuilder wb;
        Dictionary<string, XElement> diagramElements;
        Dictionary<string, ModelEntity> entitiesFromModel;

        public Locator(WorkflowBuilder wb, Dictionary<string, XElement> diagramElements, WorkflowModel model, WorkflowReplacementModel replacements)
        {
            this.wb = wb;
            this.diagramElements = diagramElements;
            this.Replacements = (replacements?.Replacements).EmptyIfNull().ToDictionary(a => a.OldTask, a => a.NewTask);
            this.entitiesFromModel = model.Entities.ToDictionary(a => a.BpmnElementId, a => a.Model);
        }

        public IWorkflowNodeEntity FindEntity(string bpmElementId)
        {
            return wb.FindEntity(bpmElementId);
        }

        internal WorkflowBuilder.LaneBuilder FindLane(WorkflowLaneEntity lane)
        {
            return wb.FindLane(lane);
        }

        public bool ExistDiagram(string bpmElementId)
        {
            return diagramElements.ContainsKey(bpmElementId);
        }

        public XElement GetDiagram(string bpmElementId)
        {
            return diagramElements.GetOrThrow(bpmElementId);
        }


        public Dictionary<Lite<WorkflowActivityEntity>, string> Replacements; 
        public WorkflowActivityEntity GetReplacement(Lite<WorkflowActivityEntity> lite)
        {
            string bpmnElementId = Replacements.GetOrThrow(lite);
            return (WorkflowActivityEntity)this.FindEntity(bpmnElementId);
        }

        internal T GetModelEntity<T>(string bpmnElementId)
            where T : ModelEntity, new()
        {
            return (T)this.entitiesFromModel.GetOrCreate(bpmnElementId, new T());
        }
    }

    public static class NodeEntityExtensions
    {
        public static WorkflowPoolEntity ApplyXml(this WorkflowPoolEntity wp, XElement participant, Locator locator)
        {
            var bpmnElementId = participant.Attribute("id").Value;
            var model = locator.GetModelEntity<WorkflowPoolModel>(bpmnElementId);
            if (model != null)
                wp.SetModel(model);
            wp.Name = participant.Attribute("name").Value;
            wp.Xml.DiagramXml = locator.GetDiagram(participant.Attribute("id").Value).ToString();
            if (GraphExplorer.HasChanges(wp))
                wp.Execute(WorkflowPoolOperation.Save);
            return wp;
        }

        public static WorkflowLaneEntity ApplyXml(this WorkflowLaneEntity wl, XElement lane, Locator locator)
        {
            var bpmnElementId = lane.Attribute("id").Value;
            var model = locator.GetModelEntity<WorkflowLaneModel>(bpmnElementId);
            if (model != null)
                wl.SetModel(model);
            wl.Name = lane.Attribute("name").Value;
            wl.Xml.DiagramXml = locator.GetDiagram(bpmnElementId).ToString();
            if (GraphExplorer.HasChanges(wl))
                wl.Execute(WorkflowLaneOperation.Save);

            return wl;
        }

        public static WorkflowEventEntity ApplyXml(this WorkflowEventEntity we, XElement @event, Locator locator)
        {
            we.Name = @event.Attribute("name")?.Value;
            we.Type = WorkflowBuilder.LaneBuilder.WorkflowEventTypes.GetOrThrow(@event.Name.LocalName);
            we.Xml.DiagramXml = locator.GetDiagram(@event.Attribute("id").Value).ToString();
            if (GraphExplorer.HasChanges(we))
                we.Execute(WorkflowEventOperation.Save);

            return we;
        }

        public static WorkflowActivityEntity ApplyXml(this WorkflowActivityEntity wa, XElement activity, Locator locator)
        {
            var bpmnElementId = activity.Attribute("id").Value;
            var model = locator.GetModelEntity<WorkflowActivityModel>(bpmnElementId);
            if (model != null)
                wa.SetModel(model);
            wa.Name = activity.Attribute("name").Value;
            wa.Xml.DiagramXml = locator.GetDiagram(bpmnElementId).ToString();
            if (GraphExplorer.HasChanges(wa))
                wa.Execute(WorkflowActivityOperation.Save);

            return wa;
        }

        public static WorkflowGatewayEntity ApplyXml(this WorkflowGatewayEntity wg, XElement gateway, Locator locator)
        {
            wg.Name = gateway.Attribute("name")?.Value;
            wg.Type = WorkflowBuilder.LaneBuilder.WorkflowGatewayTypes.GetOrThrow(gateway.Name.LocalName);
            wg.Xml.DiagramXml = locator.GetDiagram(gateway.Attribute("id").Value).ToString();
            if (GraphExplorer.HasChanges(wg))
                wg.Execute(WorkflowGatewayOperation.Save);

            return wg;
        }

        public static WorkflowConnectionEntity ApplyXml(this WorkflowConnectionEntity wc, XElement flow, Locator locator)
        {
            wc.From = locator.FindEntity(flow.Attribute("sourceRef").Value);
            wc.To = locator.FindEntity(flow.Attribute("targetRef").Value);

            var bpmnElementId = flow.Attribute("id").Value;
            var model = locator.GetModelEntity<WorkflowConnectionModel>(bpmnElementId);
            if (model != null)
                wc.SetModel(model);
            wc.Name = flow.Attribute("name")?.Value;
            wc.Xml.DiagramXml = locator.GetDiagram(bpmnElementId).ToString();
            if (GraphExplorer.HasChanges(wc))
                wc.Execute(WorkflowConnectionOperation.Save);
            return wc;
        }
    }

    public class XmlEntity<T>
    where T : Entity, IWorkflowObjectEntity, IWithModel
    {
        public XmlEntity(T entity)
        {
            var finalXml = @"<bpmn:definitions xmlns:xsi = " + ToQuoted(WorkflowBuilder.xsi.ToString()) + " " +
                          @"xmlns:bpmn = " + ToQuoted(WorkflowBuilder.bpmn.ToString()) + " " +
                          @"xmlns:bpmndi = " + ToQuoted(WorkflowBuilder.bpmndi.ToString()) + " " +
                          @"xmlns:dc = " + ToQuoted(WorkflowBuilder.dc.ToString()) + " " +
                          @"xmlns:di = " + ToQuoted(WorkflowBuilder.di.ToString()) + @" id = ""Definitions_1"" targetNamespace = " + ToQuoted(WorkflowBuilder.targetNamespace) + " > " +
                          @"<bpmndi:BPMNDiagram id = ""BPMNDiagram_1"" >"
                           + entity.Xml.DiagramXml +
                          @"</bpmndi:BPMNDiagram>" +
                          @"</bpmn:definitions>";

            Entity = entity;
            Document = XDocument.Parse(finalXml);
            Element = Document.Root.Element(WorkflowBuilder.bpmndi + "BPMNDiagram").Elements().First();
            bpmnElementId = Element.Attribute("bpmnElement").Value;
        }

        public XDocument Document;

        private XElement element;
        public XElement Element { get { return element; } private set { element = value; } }
        public string bpmnElementId;
        
        public T Entity;

        public KeyValuePair<string, Entity> ToKVP() => new KeyValuePair<string, Entity>(bpmnElementId, Entity);
        public KeyValuePair<string, ModelEntity> ToModelKVP() => new KeyValuePair<string, ModelEntity>(bpmnElementId, Entity.GetModel());

        public override string ToString() => $"{bpmnElementId} {Entity.GetType().Name} {Entity.Name}";

        public string ToQuoted(string str)
        {
            return "\"" + str + "\"";
        }
    }

    public enum WorkflowBuilderMessage
    {
        [Description("Node type {0} with Id {1} is invalid.")]
        NodeType0WithId1IsInvalid,
        [Description("Participants and Processes are not synchronized.")]
        ParticipantsAndProcessesAreNotSynchronized,
        [Description("Multiple start events are not allowed.")]
        MultipleStartEventsAreNotAllowed,
        [Description("Start event is required. each workflow could have one and only one start event.")]
        StartEventIsRequired,
        [Description("The following tasks are going to be deleted :")]
        TheFollowingTasksAreGoingToBeDeleted
    }
}
