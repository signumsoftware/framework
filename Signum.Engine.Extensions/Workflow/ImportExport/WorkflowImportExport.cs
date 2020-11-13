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
using Microsoft.Extensions.Azure;
using System.Reflection.Metadata;

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

        public IEnumerable<IWorkflowNodeEntity> Activities => activities.Values.Cast<IWorkflowNodeEntity>()
            .Concat(events.Values.Where(a => a.Type == WorkflowEventType.IntermediateTimer));


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
              workflow.ExpirationDate == null ? null! : new XAttribute("ExpirationDate", workflow.ExpirationDate.Value.ToString("o", CultureInfo.InvariantCulture)),

               this.pools.Values.Select(p => new XElement("Pool",
                new XAttribute("BpmnElementId", p.BpmnElementId),
                new XAttribute("Name", p.Name),
                p.Xml.ToXml())),

               this.lanes.Values.Select(la => new XElement("Lane",
                new XAttribute("BpmnElementId", la.BpmnElementId),
                new XAttribute("Name", la.Name),
                new XAttribute("Pool", la.Pool.BpmnElementId),
                la.Actors.IsEmpty() ? null! : new XElement("Actors", la.Actors.Select(a => new XElement("Actor", a.KeyLong()!))),
                la.ActorsEval == null ? null! : new XElement("ActorsEval", new XCData(la.ActorsEval.Script)),
                la.Xml.ToXml())),

               this.activities.Values.Select(a => new XElement("Activity",
                new XAttribute("BpmnElementId", a.BpmnElementId),
                new XAttribute("Lane", a.Lane.BpmnElementId),
                new XAttribute("Name", a.Name),
                new XAttribute("Type", a.Type.ToString()),
                a.RequiresOpen == false ? null! : new XAttribute("RequiresOpen", a.RequiresOpen),
                a.EstimatedDuration == null ? null! : new XAttribute("EstimatedDuration", a.EstimatedDuration),
                string.IsNullOrEmpty(a.ViewName) ? null! : new XAttribute("ViewName", a.ViewName),
                string.IsNullOrEmpty(a.Comments) ? null! : new XElement("Comments", a.Comments),
                !a.ViewNameProps.Any() ? null! : new XElement("ViewNameProps",
                    a.ViewNameProps.Select(vnp => new XElement("ViewNameProp", new XAttribute("Name", vnp.Name), new XCData(vnp.Expression!)))
                ),
                string.IsNullOrEmpty(a.UserHelp) ? null! : new XElement("UserHelp", new XCData(a.UserHelp)),
                a.SubWorkflow == null ? null! : new XElement("SubWorkflow",
                    new XAttribute("Workflow", ctx.Include(a.SubWorkflow.Workflow)),
                    new XElement("SubEntitiesEval", new XCData(a.SubWorkflow.SubEntitiesEval.Script))
                ),
                a.Script == null ? null! : new XElement("Script",
                    new XAttribute("Script", ctx.Include(a.Script.Script)),
                    a.Script.RetryStrategy == null ? null! : new XAttribute("RetryStrategy", ctx.Include(a.Script.RetryStrategy))
                ),
                a.Xml.ToXml()
               )),

               this.gateways.Values.Select(g => new XElement("Gateway",
                   new XAttribute("BpmnElementId", g.BpmnElementId),
                   g.Name.HasText() ? new XAttribute("Name", g.Name) : null!,
                   new XAttribute("Lane", g.Lane.BpmnElementId),
                   new XAttribute("Type", g.Type.ToString()),
                   new XAttribute("Direction", g.Direction.ToString()),
                   g.Xml.ToXml())),


               this.events.Values.Select(e => new XElement("Event",
                    new XAttribute("BpmnElementId", e.BpmnElementId),
                    e.Name.HasText() ? new XAttribute("Name", e.Name) : null!,
                    new XAttribute("Lane", e.Lane.BpmnElementId),
                    new XAttribute("Type", e.Type.ToString()),
                    e.Timer == null ? null! : new XElement("Timer",
                        e.Timer.Duration?.ToXml("Duration")!,
                        e.Timer.Condition == null ? null! : new XAttribute("Condition", ctx.Include(e.Timer.Condition))),
                    e.BoundaryOf == null ? null! : new XAttribute("BoundaryOf", this.activities.Values.SingleEx(a => a.Is(e.BoundaryOf)).BpmnElementId),
                     e.Xml.ToXml())
                ),

               this.connections.Values.Select(c => new XElement("Connection",
                    new XAttribute("BpmnElementId", c.BpmnElementId),
                    c.Name.HasText() ? new XAttribute("Name", c.Name) : null!,
                    new XAttribute("Type", c.Type.ToString()),
                    new XAttribute("From", c.From.BpmnElementId),
                    new XAttribute("To", c.To.BpmnElementId),
                    c.Condition == null ? null! : new XAttribute("Condition", ctx.Include(c.Condition)),
                    c.Action == null ? null! : new XAttribute("Action", ctx.Include(c.Action)),
                    c.Order == null ? null! : new XAttribute("Order", c.Order),
                    c.Xml.ToXml()))
               );
         

        }

        public IDisposable Sync<T>(Dictionary<string, T> entityDic, IEnumerable<XElement> elements, IFromXmlContext ctx, ExecuteSymbol<T> saveOperation, DeleteSymbol<T> deleteOperation, Action<T, XElement> setXml)
            where T : Entity, IWorkflowObjectEntity, new()
        {
            var xmlDic = elements.ToDictionaryEx(a => a.Attribute("BpmnElementId")!.Value);

            Synchronizer.Synchronize(
              xmlDic,
              entityDic,
              createNew: (bpmnId, xml) =>
              {
                  var entity = new T();
                  entity.BpmnElementId = xml.Attribute("BpmnElementId")!.Value;
                  setXml(entity, xml);
                  SaveOrMark<T>(entity, saveOperation, ctx);
                  entityDic.Add(bpmnId, entity);
              },
              removeOld: null,
              merge: null);


            return new Disposable(() =>
            {
                Synchronizer.Synchronize(
                   xmlDic,
                   entityDic,
                   createNew: null,
                   removeOld: (bpmnId, entity) => 
                   {
                       entityDic.Remove(bpmnId);
                       DeleteOrMark<T>(entity, deleteOperation, ctx);
                    },
                   merge: (bpmnId, xml, entity) =>
                   {
                       setXml(entity, xml);
                       SaveOrMark<T>(entity, saveOperation, ctx);
                   });
            });
        }


        public bool HasChanges;

        public WorkflowReplacementModel? ReplacementModel { get; internal set; }

        public void SaveOrMark<T>(T entity, ExecuteSymbol<T> saveOperation, IFromXmlContext ctx)
            where T : Entity
        {
            if (ctx.IsPreview)
            {
                if (GraphExplorer.IsGraphModified(entity))
                    HasChanges = true;

            }
            else
            {
                if (GraphExplorer.IsGraphModified(entity))
                    entity.Execute(saveOperation);
            }
        }

        public void DeleteOrMark<T>(T entity, DeleteSymbol<T> deleteOperation, IFromXmlContext ctx)
            where T : Entity
        {

            IWorkflowNodeEntity? act = entity is WorkflowActivityEntity wa ? wa :
                                 entity is WorkflowEventEntity we && we.Type == WorkflowEventType.IntermediateTimer ? we :
                                 (IWorkflowNodeEntity?)null;

            if (ctx.IsPreview)
            {
                HasChanges = true;

                if (act != null && act.CaseActivities().Any())
                {
                    var rm = this.ReplacementModel ?? (this.ReplacementModel = new WorkflowReplacementModel());

                    this.ReplacementModel.Replacements.Add(new WorkflowReplacementItemEmbedded
                    {
                        OldNode = act.ToLite(),
                        SubWorkflow = act is WorkflowActivityEntity wae ? wae.SubWorkflow?.Workflow.ToLite() : null,
                    });
                }
            }
            else
            {
                if (act != null && act.CaseActivities().Any())
                {
                    var replacementItem = ReplacementModel?.Replacements.SingleOrDefaultEx(a => a.OldNode.Is(act.ToLite()));

                    if (replacementItem == null)
                        throw new InvalidOperationException($"Unable to delete '{entity}' without a replacement for the Case Activities");

                    var replacement = this.activities.GetOrThrow(replacementItem.NewNode);

                    act.CaseActivities()
                        .Where(a => a.State == CaseActivityState.Done)
                        .UnsafeUpdate()
                        .Set(ca => ca.WorkflowActivity, ca => replacement)
                        .Execute();

                    var running = act.CaseActivities().Where(a => a.State == CaseActivityState.PendingDecision || a.State == CaseActivityState.PendingNext).ToList();

                    running.ForEach(a =>
                    {
                        a.Notifications().UnsafeDelete();
                        a.WorkflowActivity = replacement;
                        a.Save();
                        CaseActivityLogic.InsertCaseActivityNotifications(a);
                    });
                }

                entity.Delete(deleteOperation);


            }
        }


        public void FromXml(XElement element, IFromXmlContext ctx)
        {
            this.workflow.Name = element.Attribute("Name")!.Value;
            this.workflow.MainEntityType = ctx.GetType(element.Attribute("MainEntityType")!.Value);
            this.workflow.MainEntityStrategies.Synchronize(element.Attribute("MainEntityStrategies")!.Value.Split(",").Select(a => a.Trim().ToEnum<WorkflowMainEntityStrategy>()).ToList());
            this.workflow.ExpirationDate = element.Attribute("ExpirationDate")?.Let(ed => DateTime.ParseExact(ed.Value, "o", CultureInfo.InvariantCulture));

            if(!ctx.IsPreview)
            {
                if (this.workflow.IsNew)
                {
                    using (OperationLogic.AllowSave<WorkflowEntity>())
                        this.workflow.Save();
                }
            }
            else
            {
                if (GraphExplorer.HasChanges(this.workflow))
                    HasChanges = true;
            }

            using (Sync(this.pools, element.Elements("Pool"), ctx, WorkflowPoolOperation.Save, WorkflowPoolOperation.Delete, (pool, xml) =>
            { 
                 pool.Name = xml.Attribute("Name")!.Value;
                 pool.Workflow = this.workflow;
                 SetXmlDiagram(pool, xml);
             }))
            {
                using (Sync(this.lanes, element.Elements("Lane"), ctx, WorkflowLaneOperation.Save, WorkflowLaneOperation.Delete, (lane, xml) =>
                {
                    lane.Name = xml.Attribute("Name")!.Value;
                    lane.Pool = this.pools.GetOrThrow(xml.Attribute("Pool")!.Value);
                    lane.Actors.Synchronize((xml.Element("Actors")?.Elements("Actor")).EmptyIfNull().Select(a => Lite.Parse(a.Value)).ToMList());
                    lane.ActorsEval = lane.ActorsEval.CreateOrAssignEmbedded(xml.Element("ActorsEval"), (ae, aex) => { ae.Script = aex.Value; });
                    SetXmlDiagram(lane, xml);
                }))
                {
                    using (Sync(this.activities, element.Elements("Activity"), ctx, WorkflowActivityOperation.Save, WorkflowActivityOperation.Delete, (activity, xml) =>
                    {
                        activity.Lane = this.lanes.GetOrThrow(xml.Attribute("Lane")!.Value);
                        activity.Name = xml.Attribute("Name")!.Value;
                        activity.Type = xml.Attribute("Type")!.Value.ToEnum<WorkflowActivityType>();
                        activity.Comments = xml.Element("Comments")?.Value;
                        activity.RequiresOpen = (bool?)xml.Attribute("RequiresOpen") ?? false;
                        activity.EstimatedDuration = (double?)xml.Attribute("EstimatedDuration");
                        activity.ViewName = (string?)xml.Attribute("ViewName");
                        activity.ViewNameProps.Synchronize(xml.Element("ViewNameProps")?.Elements("ViewNameProp").ToList(), (vnpe, elem) =>
                        {
                            vnpe.Name = elem.Value;
                        });
                        activity.UserHelp = xml.Element("UserHelp")?.Value;
                        activity.SubWorkflow = activity.SubWorkflow.CreateOrAssignEmbedded(xml.Element("SubWorkflow"), (swe, elem) =>
                        {
                            swe.Workflow = (WorkflowEntity)ctx.GetEntity((Guid)elem.Attribute("Workflow")!);
                            swe.SubEntitiesEval = swe.SubEntitiesEval.CreateOrAssignEmbedded(elem.Element("SubEntitiesEval"), (se, x) =>
                            {
                                se.Script = x.Value;
                            })!;
                        });
                        activity.Script = activity.Script.CreateOrAssignEmbedded(xml.Element("Script"), (swe, elem) =>
                        {
                            swe.Script = ((WorkflowScriptEntity)ctx.GetEntity((Guid)elem.Attribute("Script")!)).ToLite();
                            swe.RetryStrategy = elem.Attribute("RetryStrategy")?.Let(a => (WorkflowScriptRetryStrategyEntity)ctx.GetEntity((Guid)a));
                        });
                        SetXmlDiagram(activity, xml);
                    }))
                    {
                        using (Sync(this.events, element.Elements("Event"), ctx, WorkflowEventOperation.Save, WorkflowEventOperation.Delete, (ev, xml) =>
                        {
                            ev.Name = xml.Attribute("Name")?.Value;
                            ev.Lane = this.lanes.GetOrThrow(xml.Attribute("Lane")!.Value);
                            ev.Type = xml.Attribute("Type")!.Value.ToEnum<WorkflowEventType>();
                            ev.Timer = ev.Timer.CreateOrAssignEmbedded(xml.Element("Timer"), (time, xml) =>
                            {
                                time.Duration = time.Duration.CreateOrAssignEmbedded(xml.Element("Duration"), (ts, xml) => ts.FromXml(xml));
                                time.Condition = xml.Attribute("Condition")?.Let(a => ((WorkflowTimerConditionEntity)ctx.GetEntity((Guid)a)).ToLite());
                            });
                            ev.BoundaryOf = xml.Attribute("BoundaryOf")?.Let(a =>activities.GetOrThrow(a.Value).ToLite());

                            SetXmlDiagram(ev, xml);
                        }))
                        {
                            using (Sync(this.gateways, element.Elements("Gateway"), ctx, WorkflowGatewayOperation.Save, WorkflowGatewayOperation.Delete, (gw, xml) =>
                            {
                                gw.Name = xml.Attribute("Name")?.Value;
                                gw.Lane = this.lanes.GetOrThrow(xml.Attribute("Lane")!.Value);
                                gw.Type = xml.Attribute("Type")!.Value.ToEnum<WorkflowGatewayType>();
                                gw.Direction = xml.Attribute("Direction")!.Value.ToEnum<WorkflowGatewayDirection>();

                                SetXmlDiagram(gw, xml);
                            }))
                            {
                                using (Sync(this.connections, element.Elements("Connection"), ctx, WorkflowConnectionOperation.Save, WorkflowConnectionOperation.Delete, (conn, xml) =>
                                {
                                    conn.Name = xml.Attribute("Name")?.Value;
                                    conn.Type = xml.Attribute("Type")!.Value.ToEnum<ConnectionType>();
                                    conn.From = GetNode(xml.Attribute("From")!.Value);
                                    conn.To = GetNode(xml.Attribute("To")!.Value);
                                    conn.Condition = xml.Attribute("Condition")?.Let(a => ((WorkflowConditionEntity)ctx.GetEntity((Guid)a)).ToLite());
                                    conn.Action = xml.Attribute("Action")?.Let(a => ((WorkflowActionEntity)ctx.GetEntity((Guid)a)).ToLite());
                                    conn.Order = (int?)xml.Attribute("Order");
                                    SetXmlDiagram(conn, xml);
                                }))
                                {
                                    //Identation vertigo :)
                                }
                            }
                        }
                    }
                }
            }

            if (this.HasChanges && !ctx.IsPreview)
                this.workflow.Execute(WorkflowOperation.Save);
        }

        public IWorkflowNodeEntity GetNode(string bpmnElementId)
        {
            return (IWorkflowNodeEntity?)this.activities.TryGetC(bpmnElementId)
                ?? (IWorkflowNodeEntity?)this.events.TryGetC(bpmnElementId)
                ?? (IWorkflowNodeEntity?)this.gateways.TryGetC(bpmnElementId)
                ?? throw new InvalidOperationException("No Workflow node wound with BpmnElementId: " + bpmnElementId);
        }

        void SetXmlDiagram(IWorkflowObjectEntity entity, XElement xml)
        {
            if (entity.Xml == null)
                entity.Xml = new WorkflowXmlEmbedded();

            var newValue = xml.Element("DiagramXml")!.Value;

            if (!Enumerable.SequenceEqual((entity.Xml.DiagramXml ?? "").Lines(), newValue.Lines()))
                entity.Xml.DiagramXml = newValue;
        }
    }
}
