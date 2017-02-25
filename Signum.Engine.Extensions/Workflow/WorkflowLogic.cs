using Signum.Entities.Workflow;
using Signum.Engine;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Utilities;
using Signum.Utilities.DataStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Signum.Entities.Basics;

namespace Signum.Engine.Workflow
{

    public static class WorkflowLogic
    {
        public static Action<ICaseMainEntity, WorkflowTransitionContext> OnTransition;

        static Expression<Func<WorkflowEntity, IQueryable<WorkflowPoolEntity>>> WorkflowPoolsExpression =
            e => Database.Query<WorkflowPoolEntity>().Where(a => a.Workflow == e);
        [ExpressionField]
        public static IQueryable<WorkflowPoolEntity> WorkflowPools(this WorkflowEntity e)
        {
            return WorkflowPoolsExpression.Evaluate(e);
        }

        static Expression<Func<WorkflowEntity, IQueryable<WorkflowActivityEntity>>> WorkflowActivitiesExpression =
            e => Database.Query<WorkflowActivityEntity>().Where(a => a.Lane.Pool.Workflow == e);
        [ExpressionField]
        public static IQueryable<WorkflowActivityEntity> WorkflowActivities(this WorkflowEntity e)
        {
            return WorkflowActivitiesExpression.Evaluate(e);
        }

        public static IEnumerable<WorkflowActivityEntity> WorkflowActivitiesFromCache(this WorkflowEntity e)
        {
            return WorkflowGraphLazy.Value.GetOrThrow(e.ToLite()).NextGraph.OfType<WorkflowActivityEntity>();
        }

        static Expression<Func<WorkflowEntity, IQueryable<WorkflowEventEntity>>> WorkflowEventsExpression =
            e => Database.Query<WorkflowEventEntity>().Where(a => a.Lane.Pool.Workflow == e);
        [ExpressionField]
        public static IQueryable<WorkflowEventEntity> WorkflowEvents(this WorkflowEntity e)
        {
            return WorkflowEventsExpression.Evaluate(e);
        }

        public static IEnumerable<WorkflowEventEntity> WorkflowEventsFromCache(this WorkflowEntity e)
        {
            return WorkflowGraphLazy.Value.GetOrThrow(e.ToLite()).NextGraph.OfType<WorkflowEventEntity>();
        }

        static Expression<Func<WorkflowEntity, IQueryable<WorkflowGatewayEntity>>> WorkflowGatewaysExpression =
            e => Database.Query<WorkflowGatewayEntity>().Where(a => a.Lane.Pool.Workflow == e);
        [ExpressionField]
        public static IQueryable<WorkflowGatewayEntity> WorkflowGateways(this WorkflowEntity e)
        {
            return WorkflowGatewaysExpression.Evaluate(e);
        }

        public static IEnumerable<WorkflowGatewayEntity> WorkflowGatewaysFromCache(this WorkflowEntity e)
        {
            return WorkflowGraphLazy.Value.GetOrThrow(e.ToLite()).NextGraph.OfType<WorkflowGatewayEntity>();
        }

        static Expression<Func<WorkflowEntity, IQueryable<WorkflowConnectionEntity>>> WorkflowConnectionsExpression =
          e => Database.Query<WorkflowConnectionEntity>().Where(a => a.From.Lane.Pool.Workflow == e && a.To.Lane.Pool.Workflow == e );
        [ExpressionField]
        public static IQueryable<WorkflowConnectionEntity> WorkflowConnections(this WorkflowEntity e)
        {
            return WorkflowConnectionsExpression.Evaluate(e);
        }

        public static IEnumerable<WorkflowConnectionEntity> WorkflowConnectionsFromCache(this WorkflowEntity e)
        {
            return WorkflowGraphLazy.Value.GetOrThrow(e.ToLite()).NextGraph.EdgesWithValue.Select(edge => edge.Value);
        }

        static Expression<Func<WorkflowEntity, IQueryable<WorkflowConnectionEntity>>> WorkflowMessageConnectionsExpression =
         e => e.WorkflowConnections().Where(a => a.From.Lane.Pool != a.To.Lane.Pool);
        [ExpressionField]
        public static IQueryable<WorkflowConnectionEntity> WorkflowMessageConnections(this WorkflowEntity e)
        {
            return WorkflowMessageConnectionsExpression.Evaluate(e);
        }

        static Expression<Func<WorkflowPoolEntity, IQueryable<WorkflowLaneEntity>>> PoolLanesExpression =
            e => Database.Query<WorkflowLaneEntity>().Where(a => a.Pool == e);
        [ExpressionField]
        public static IQueryable<WorkflowLaneEntity> WorkflowLanes(this WorkflowPoolEntity e)
        {
            return PoolLanesExpression.Evaluate(e);
        }

        static Expression<Func<WorkflowPoolEntity, IQueryable<WorkflowConnectionEntity>>> PoolConnectionsExpression =
            e => Database.Query<WorkflowConnectionEntity>().Where(a => a.From.Lane.Pool == e && a.To.Lane.Pool == e);
        [ExpressionField]
        public static IQueryable<WorkflowConnectionEntity> WorkflowConnections(this WorkflowPoolEntity e)
        {
            return PoolConnectionsExpression.Evaluate(e);
        }

        static Expression<Func<WorkflowLaneEntity, IQueryable<WorkflowGatewayEntity>>> LaneGatewaysExpression =
            e => Database.Query<WorkflowGatewayEntity>().Where(a => a.Lane == e);
        [ExpressionField]
        public static IQueryable<WorkflowGatewayEntity> WorkflowGateways(this WorkflowLaneEntity e)
        {
            return LaneGatewaysExpression.Evaluate(e);
        }

        static Expression<Func<WorkflowLaneEntity, IQueryable<WorkflowEventEntity>>> LaneEventsExpression =
            e => Database.Query<WorkflowEventEntity>().Where(a => a.Lane == e);
        [ExpressionField]
        public static IQueryable<WorkflowEventEntity> WorkflowEvents(this WorkflowLaneEntity e)
        {
            return LaneEventsExpression.Evaluate(e);
        }

        static Expression<Func<WorkflowLaneEntity, IQueryable<WorkflowActivityEntity>>> LaneActivitiesExpression =
            e => Database.Query<WorkflowActivityEntity>().Where(a => a.Lane == e);
        [ExpressionField]
        public static IQueryable<WorkflowActivityEntity> WorkflowActivities(this WorkflowLaneEntity e)
        {
            return LaneActivitiesExpression.Evaluate(e);
        }

        static Expression<Func<IWorkflowNodeEntity, IQueryable<WorkflowConnectionEntity>>> NextConnectionsExpression =
            e => Database.Query<WorkflowConnectionEntity>().Where(a => a.From == e);
        [ExpressionField]
        public static IQueryable<WorkflowConnectionEntity> NextConnections(this IWorkflowNodeEntity e)
        {
            return NextConnectionsExpression.Evaluate(e);
        }

        public static IEnumerable<WorkflowConnectionEntity> NextConnectionsFromCache(this IWorkflowNodeEntity e)
        {
            return WorkflowGraphLazy.Value.GetOrThrow(e.Lane.Pool.Workflow.ToLite()).NextGraph.RelatedTo(e).Values;
        }

        static Expression<Func<IWorkflowNodeEntity, IQueryable<WorkflowConnectionEntity>>> PreviousConnectionsExpression =
            e => Database.Query<WorkflowConnectionEntity>().Where(a => a.To == e);
        [ExpressionField]
        public static IQueryable<WorkflowConnectionEntity> PreviousConnections(this IWorkflowNodeEntity e)
        {
            return PreviousConnectionsExpression.Evaluate(e);
        }

        public static IEnumerable<WorkflowConnectionEntity> PreviousConnectionsFromCache(this IWorkflowNodeEntity e)
        {
            return WorkflowGraphLazy.Value.GetOrThrow(e.Lane.Pool.Workflow.ToLite()).PreviousGraph.RelatedTo(e).Values;
        }


        static ResetLazy<Dictionary<Lite<WorkflowEntity>, WorkflowNodeGraph>> WorkflowGraphLazy;

        public static List<Lite<IWorkflowNodeEntity>> AutocompleteNodes(Lite<WorkflowEntity> workflow, string subString, int count, List<Lite<IWorkflowNodeEntity>> excludes)
        {
            return WorkflowGraphLazy.Value.GetOrThrow(workflow).Autocomplete(subString, count, excludes);
        }

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<WorkflowEntity>()
                    .WithQuery(dqm, e => new
                    {
                        Entity = e,
                        e.Id,
                        e.Name,
                        e.MainEntityType,
                    });
                
                WorkflowGraph.Register();
                

                sb.Include<WorkflowPoolEntity>()
                    .WithUniqueIndex(wp => new { wp.Workflow, wp.Name })
                    .WithSave(WorkflowPoolOperation.Save)
                    .WithDelete(WorkflowPoolOperation.Delete)
                    .WithExpressionFrom(dqm, (WorkflowEntity p) => p.WorkflowPools())
                    .WithQuery(dqm, e => new
                    {
                        Entity = e,
                        e.Id,
                        e.Name,
                        e.BpmnElementId,
                        e.Workflow,
                    });

                sb.Include<WorkflowLaneEntity>()
                    .WithUniqueIndex(wp => new { wp.Pool, wp.Name })
                    .WithSave(WorkflowLaneOperation.Save)
                    .WithDelete(WorkflowLaneOperation.Delete)
                    .WithExpressionFrom(dqm, (WorkflowPoolEntity p) => p.WorkflowLanes())
                    .WithQuery(dqm, e => new
                    {
                        Entity = e,
                        e.Id,
                        e.Name,
                        e.BpmnElementId,
                        e.Pool,
                        e.Pool.Workflow,
                    });

                sb.Include<WorkflowActivityEntity>()
                    .WithUniqueIndex(w => new { w.Lane, w.Name })
                    .WithSave(WorkflowActivityOperation.Save)
                    .WithDelete(WorkflowActivityOperation.Delete)
                    .WithExpressionFrom(dqm, (WorkflowEntity p) => p.WorkflowActivities())
                    .WithExpressionFrom(dqm, (WorkflowLaneEntity p) => p.WorkflowActivities())
                    .WithQuery(dqm, e => new
                    {
                        Entity = e,
                        e.Id,
                        e.Name,
                        e.BpmnElementId,
                        e.Comments,
                        e.Lane,
                        e.Lane.Pool.Workflow,
                    });

                sb.AddUniqueIndexMList((WorkflowActivityEntity a) => a.Jumps, mle => new { mle.Parent, mle.Element.To });

                sb.Include<WorkflowEventEntity>()
                    .WithSave(WorkflowEventOperation.Save)
                    .WithDelete(WorkflowEventOperation.Delete)
                    .WithExpressionFrom(dqm, (WorkflowEntity p) => p.WorkflowEvents())
                    .WithExpressionFrom(dqm, (WorkflowLaneEntity p) => p.WorkflowEvents())
                    .WithQuery(dqm, e => new
                    {
                        Entity = e,
                        e.Id,
                        e.Type,
                        e.Name,
                        e.BpmnElementId,
                        e.Lane,
                        e.Lane.Pool.Workflow,
                    });

                sb.Include<WorkflowGatewayEntity>()
                    .WithSave(WorkflowGatewayOperation.Save)
                    .WithDelete(WorkflowGatewayOperation.Delete)
                    .WithExpressionFrom(dqm, (WorkflowEntity p) => p.WorkflowGateways())
                    .WithExpressionFrom(dqm, (WorkflowLaneEntity p) => p.WorkflowGateways())
                    .WithQuery(dqm, e => new
                    {
                        Entity = e,
                        e.Id,
                        e.Type,
                        e.Name,
                        e.BpmnElementId,
                        e.Lane,
                        e.Lane.Pool.Workflow,
                    });

                sb.Include<WorkflowConnectionEntity>()
                    .WithSave(WorkflowConnectionOperation.Save)
                    .WithDelete(WorkflowConnectionOperation.Delete)
                    .WithExpressionFrom(dqm, (WorkflowEntity p) => p.WorkflowConnections())
                    .WithExpressionFrom(dqm, (WorkflowEntity p) => p.WorkflowMessageConnections(), null)
                    .WithExpressionFrom(dqm, (WorkflowPoolEntity p) => p.WorkflowConnections())
                    .WithExpressionFrom(dqm, (IWorkflowNodeEntity p) => p.NextConnections(), null)
                    .WithExpressionFrom(dqm, (IWorkflowNodeEntity p) => p.PreviousConnections(), null)
                    .WithQuery(dqm, e => new
                    {
                        Entity = e,
                        e.Id,
                        e.Name,
                        e.BpmnElementId,
                        e.From,
                        e.To,
                    });

                WorkflowGraphLazy = sb.GlobalLazy(() =>
                {
                    using (new EntityCache())
                    {
                        var events = Database.RetrieveAll<WorkflowEventEntity>().GroupToDictionary(a => a.Lane.Pool.Workflow.ToLite());
                        var gateways = Database.RetrieveAll<WorkflowGatewayEntity>().GroupToDictionary(a => a.Lane.Pool.Workflow.ToLite());
                        var activities = Database.RetrieveAll<WorkflowActivityEntity>().GroupToDictionary(a => a.Lane.Pool.Workflow.ToLite());
                        var connections = Database.RetrieveAll<WorkflowConnectionEntity>().GroupToDictionary(a => a.From.Lane.Pool.Workflow.ToLite());

                        var result = Database.RetrieveAllLite<WorkflowEntity>().ToDictionary(w => w, w =>
                        {
                            var nodeGraph = new WorkflowNodeGraph
                            {
                                Workflow = w,
                                Events = events.TryGetC(w).EmptyIfNull().ToDictionary(e => e.ToLite()),
                                Gateways = gateways.TryGetC(w).EmptyIfNull().ToDictionary(g => g.ToLite()),
                                Activities = activities.TryGetC(w).EmptyIfNull().ToDictionary(a => a.ToLite()),
                                Connections = connections.TryGetC(w).EmptyIfNull().ToDictionary(c => c.ToLite()),
                            };

                            nodeGraph.FillGraphs();
                            return nodeGraph;
                        });

                        return result;
                    }
                }, new InvalidateWith(typeof(WorkflowConnectionEntity)));

                Validator.PropertyValidator((WorkflowConnectionEntity c) => c.Condition).StaticPropertyValidation = (e, pi) =>
                {
                    if (e.Condition != null && e.From != null)
                    {
                        var conditionType = Conditions.Value.GetOrThrow(e.Condition).MainEntityType;
                        var workflowType = e.From.Lane.Pool.Workflow.MainEntityType;

                        if (!conditionType.Is(workflowType))
                            return WorkflowMessage.Condition0IsDefinedFor1Not2.NiceToString(conditionType, workflowType);
                    }

                    return null;
                };

                sb.Include<WorkflowConditionEntity>()
                   .WithSave(WorkflowConditionOperation.Save)
                   .WithQuery(dqm, e => new
                   {
                       Entity = e,
                       e.Id,
                       e.Name,
                       e.MainEntityType,
                       e.Eval.Script
                   });


                new Graph<WorkflowConditionEntity>.Delete(WorkflowConditionOperation.Delete)
                {
                    Delete = (e, _) =>
                    {
                        ThrowConnectionError(Database.Query<WorkflowConnectionEntity>().Where(a => a.Condition == e.ToLite()), e);
                        e.Delete();
                    },
                }.Register();

 

                Conditions = sb.GlobalLazy(() => Database.Query<WorkflowConditionEntity>().ToDictionary(a => a.ToLite()),
                    new InvalidateWith(typeof(WorkflowConditionEntity)));

                sb.Include<WorkflowActionEntity>()
                   .WithSave(WorkflowActionOperation.Save)
                   .WithQuery(dqm, e => new
                   {
                       Entity = e,
                       e.Id,
                       e.Name,
                       e.MainEntityType,
                       e.Eval.Script
                   });

                new Graph<WorkflowActionEntity>.Delete(WorkflowActionOperation.Delete)
                {
                    Delete = (e, _) =>
                    {
                        ThrowConnectionError(Database.Query<WorkflowConnectionEntity>().Where(a => a.Action == e.ToLite()), e);
                        e.Delete();
                    },
                }.Register();

                Actions = sb.GlobalLazy(() => Database.Query<WorkflowActionEntity>().ToDictionary(a => a.ToLite()),
                    new InvalidateWith(typeof(WorkflowActionEntity)));


                sb.Include<WorkflowScriptRetryStrategyEntity>()
                    .WithSave(WorkflowScriptRetryStrategyOperation.Save)
                    .WithDelete(WorkflowScriptRetryStrategyOperation.Delete)
                    .WithQuery(dqm, e => new
                    {
                        Entity = e,
                        e.Id,
                        e.Rule
                    });
            }
        }


        private static void ThrowConnectionError(IQueryable<WorkflowConnectionEntity> queryable, Entity toDelete)
        {
            if (queryable.Count() == 0)
                return;

            var errors = queryable.Select(a => new { Connection = a.ToLite(), From = a.From.ToLite(), To = a.To.ToLite(), Workflow = a.From.Lane.Pool.Workflow.ToLite() }).ToList();

            var formattedErrors = errors.GroupBy(a => a.Workflow).ToString(gr => $"Workflow '{gr.Key}':" +
                  gr.ToString(a => $"Connection {a.Connection.Id} ({a.Connection}): {a.From} -> {a.To}", "\r\n").Indent(4),
                "\r\n\r\n").Indent(4);

            throw new ApplicationException($"Impossible to delete '{toDelete}' because is used in some connections: \r\n" + formattedErrors);
        }

        public class WorkflowGraph : Graph<WorkflowEntity>
        {
            public static void Register()
            {
                new Execute(WorkflowOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (e, args) =>
                    {
                        WorkflowLogic.ApplyDocument(e, args.GetArg<WorkflowModel>(), args.TryGetArgC<WorkflowReplacementModel>());
                    }
                }.Register();

                new ConstructFrom<WorkflowEntity>(WorkflowOperation.Clone)
                {
                    Construct = (w, args) =>
                    {
                        WorkflowBuilder wb = new WorkflowBuilder(w);

                        var result = wb.Clone();

                        return result;
                    }
                }.Register();
            }
        }

        public static ResetLazy<Dictionary<Lite<WorkflowConditionEntity>, WorkflowConditionEntity>> Conditions;
        public static WorkflowConditionEntity RetrieveFromCache(this Lite<WorkflowConditionEntity> wc)
        {
            return WorkflowLogic.Conditions.Value.GetOrThrow(wc);
        }

        public static ResetLazy<Dictionary<Lite<WorkflowActionEntity>, WorkflowActionEntity>> Actions;
        public static WorkflowActionEntity RetrieveFromCache(this Lite<WorkflowActionEntity> wa)
        {
            return WorkflowLogic.Actions.Value.GetOrThrow(wa);
        }

        public static Expression<Func<Lite<Entity>, UserEntity,  bool>> IsCurrentUserActor = (actor, user) =>
            actor.RefersTo(user) ||
            actor.Is(user.Role);

        public static List<Lite<WorkflowEntity>> GetAllowedStarts()
        {
            return (from w in Database.Query<WorkflowEntity>()
                    let s = w.WorkflowEvents().Single(a => a.Type == WorkflowEventType.Start)
                    let a = (WorkflowActivityEntity)s.NextConnections().Single().To
                    where a.Lane.Actors.Any(a => IsCurrentUserActor.Evaluate(a, UserEntity.Current))
                    select w.ToLite())
                    .ToList();
        }

        public static WorkflowModel GetWorkflowModel(WorkflowEntity workflow)
        {
            var wb = new WorkflowBuilder(workflow);
            return wb.GetWorkflowModel();
        }

        public static PreviewResult PreviewChanges(WorkflowEntity workflow, WorkflowModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            var document = XDocument.Parse(model.DiagramXml);
            var wb = new WorkflowBuilder(workflow);
            return wb.PreviewChanges(document);
        }

        public static void ApplyDocument(WorkflowEntity workflow, WorkflowModel model, WorkflowReplacementModel replacements)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

         
            var wb = new WorkflowBuilder(workflow);
            if (workflow.IsNew)
                workflow.Save();

            wb.ApplyChanges(model, replacements);
            wb.ValidateGraph();
            workflow.FullDiagramXml = new WorkflowXmlEntity { DiagramXml = wb.GetXDocument().ToString() };
            workflow.Save();
        }
    }

    public class WorkflowNodeGraph
    {
        public Lite<WorkflowEntity> Workflow { get; internal set; }
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
    }


}
