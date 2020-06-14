using Signum.Entities.Workflow;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Entities.Reflection;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Linq.Expressions;
using System.Xml;
using Signum.Entities.UserAssets;
using System.Globalization;
using System.Xml.Schema;

namespace Signum.Engine.Workflow
{
    public class WorkflowImportExport
    {
        private WorkflowEntity workflow;

        Dictionary<string, WorkflowConnectionEntity> connections;
        Dictionary<string, WorkflowEventEntity> events;
        Dictionary<string, WorkflowActivityEntity> activities;
        Dictionary<string, WorkflowGatewayEntity> gateways;
        Dictionary<string, WorkflowLaneEntity> lanes;
        Dictionary<string, WorkflowPoolEntity> pools;


        public WorkflowImportExport(WorkflowEntity wf)
        {
            using (HeavyProfiler.Log("WorkflowBuilder"))
            using (new EntityCache())
            {
                this.workflow = wf;

                this.connections = wf.IsNew ? new Dictionary<string, WorkflowConnectionEntity>() : wf.WorkflowConnections().ToDictionaryEx(a => a.BpmnElementId);
                this.events = wf.IsNew ? new Dictionary<string, WorkflowEventEntity>() : wf.WorkflowEvents().ToDictionaryEx(a => a.BpmnElementId);
                this.activities = wf.IsNew ? new Dictionary<string, WorkflowActivityEntity>() : wf.WorkflowActivities().ToDictionaryEx(a => a.BpmnElementId);
                this.gateways = wf.IsNew ? new Dictionary<string, WorkflowGatewayEntity>() : wf.WorkflowGateways().ToDictionaryEx(a => a.BpmnElementId);
                this.lanes = wf.IsNew ? new Dictionary<string, WorkflowLaneEntity>() : wf.WorkflowPools().SelectMany(a => a.WorkflowLanes()).ToDictionaryEx(a => a.BpmnElementId);
                this.pools = wf.IsNew ? new Dictionary<string, WorkflowPoolEntity>() : wf.WorkflowPools().ToDictionaryEx(a => a.BpmnElementId);
            }
        }

        public XElement ToXml(IToXmlContext ctx)
        {
            return new XElement("Workflow",
              new XAttribute("Guid", workflow.Guid),
              new XAttribute("Name", workflow.Name),
              new XAttribute("MainEntityType", ctx.TypeToName(workflow.MainEntityType.ToLite())),
              new XAttribute("MainEntityStrategies", workflow.MainEntityStrategies.ToString(",")),
              workflow.ExpirationDate == null ? null : new XAttribute("ExpirationDate", workflow.ExpirationDate.Value.ToString("o", CultureInfo.InvariantCulture)),
              this.pools.Values.Select(p => new XElement("Pool",
                new XAttribute("BpmnElementId", p.BpmnElementId),
                new XAttribute("Name", p.Name),
                p.Xml.ToXCData())),

               this.lanes.Values.Select(la => new XElement("Lane",
                new XAttribute("BpmnElementId", la.BpmnElementId),
                new XAttribute("Name", la.Name),
                new XAttribute("Pool", la.Pool.BpmnElementId),
                la.Actors.IsEmpty() ? null : new XElement("Actors", la.Actors.Select(a => new XElement("Actor", a.KeyLong()))),
                la.ActorsEval == null ? null : new XElement("ActorsEval", new XCData(la.ActorsEval.Script)),
                la.Xml.ToXCData())),

               this.activities.Values.Select(a => new XElement("Activity",
                new XAttribute("BpmnElementId", a.BpmnElementId),
                new XAttribute("Lane", a.Lane.BpmnElementId),
                new XAttribute("Name", a.Name),
                a.Comments == null ? null : new XAttribute("Comments", a.Comments),
                a.RequiresOpen == false ? null : new XAttribute("RequiresOpen", a.RequiresOpen),
                a.EstimatedDuration == null ? null : new XAttribute("EstimatedDuration", a.EstimatedDuration),
                a.ViewName == null ? null : new XAttribute("ViewName", a.ViewName),
                !a.ViewNameProps.Any() ? null : new XElement("ViewNameProps", a.ViewNameProps
                .Select(vnp => new XElement("ViewNameProp", new XAttribute("Name", vnp.Name), new XCData(vnp.Expression)))),
                a.UserHelp == null ? null : new XElement("UserHelp", new XCData(a.UserHelp)),
                a.SubWorkflow == null ? null : new XElement("SubWorkflow",
                new XAttribute("Workflow", ctx.Include(a.SubWorkflow.Workflow)),
                new XAttribute("SubEntitiesEval", new XCData(a.SubWorkflow.SubEntitiesEval.Script))
                )
              ))


            //TODO: Complete code

            );
        }

        public IDisposable Sync<T>(Dictionary<string, T> entityDic, IEnumerable<XElement> elements, IFromXmlContext ctx, Action<T, XElement> setXml)
            where T : Entity, new()
        {
            var xmlDic = elements.ToDictionaryEx(a => a.Attribute("BpmnElementId").Value);

            Synchronizer.Synchronize(
              xmlDic,
              entityDic,
              createNew: (bpmnId, xml) =>
              {
                  var entity = new T();
                  setXml(entity, xml);
                  entityDic.Add(bpmnId, ctx.SaveMaybe(entity));
              },
              removeOld: null,
              merge: null);


            return new Disposable(() =>
            {
                Synchronizer.Synchronize(
                   xmlDic,
                   entityDic,
                   createNew: null,
                   removeOld: (bpmnId, entity) => ctx.DeleteMaybe(entity),
                   merge: (bpmnId, xml, entity) =>
                   {
                       setXml(entity, xml);
                       ctx.SaveMaybe(entity);
                   });
            });
        }


        public void FromXml(XElement element, IFromXmlContext ctx)
        {
            this.workflow.Name = element.Attribute("Name").Value;
            this.workflow.MainEntityType = ctx.GetType(element.Attribute("MainEntityType").Value).RetrieveAndForget();
            this.workflow.MainEntityStrategies.AssignMList(element.Attribute("MainEntityStrategies").Value.Split(",").Select(a => a.Trim().ToEnum<WorkflowMainEntityStrategy>()).ToMList());
            this.workflow.ExpirationDate = element.Attribute("ExpirationDate")?.Let(ed => DateTime.ParseExact(ed.Value, "o", CultureInfo.InvariantCulture));

            ctx.SaveMaybe(this.workflow);

            using (Sync(this.pools, element.Elements("Pool"), ctx, (pool, xml) =>
             {
                 pool.BpmnElementId = xml.Attribute("bpmnElementId").Value;
                 pool.Name = xml.Attribute("Name").Value;
                 pool.Workflow = this.workflow;
                 SetXmlDiagram(pool, xml);
             }))
            {
                using (Sync(this.lanes, element.Elements("Lane"), ctx, (lane, xml) =>
                {
                    lane.BpmnElementId = xml.Attribute("bpmnElementId").Value;
                    lane.Name = xml.Attribute("Name").Value;
                    lane.Pool = this.pools.GetOrThrow(xml.Attribute("Pool").Value);
                    lane.Actors = (xml.Element("Actors")?.Elements("Actor")).EmptyIfNull().Select(a => Lite.Parse(a.Value)).ToMList();
                    lane.ActorsEval = xml.Element("ActorsEval")?.Let(ae => (lane.ActorsEval ?? new WorkflowLaneActorsEval()).Do(wal => wal.Script = ae.Value));
                    SetXmlDiagram(lane, xml);
                }))
                {
                    //TODO: Complete code
                }
            }
        }

        void SetXmlDiagram(IWorkflowObjectEntity entity, XElement xml)
        {
            if (entity.Xml == null)
                entity.Xml = new WorkflowXmlEmbedded();

            entity.Xml.DiagramXml = xml.Descendants().OfType<XCData>().Single().Value;
        }
    }
}
