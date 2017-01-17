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

namespace Signum.Engine.Workflow
{

    public static class WorkflowLogic
    {
        public static Action<ICaseMainEntity, WorkflowEvaluationContext> OnTransition;

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

        public class WorkflowGraph
        {
            public DirectedEdgedGraph<IWorkflowNodeEntity, WorkflowConnectionEntity> NextGraph;
            public DirectedEdgedGraph<IWorkflowNodeEntity, WorkflowConnectionEntity> PreviousGraph;
        }

        static ResetLazy<Dictionary<Lite<WorkflowEntity>, WorkflowGraph>> WorkflowGraphLazy;

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<WorkflowEntity>()
                    .WithQuery(dqm, e => new
                    {
                        Entity = e,
                        e.Id,
                        e.Name
                    });

                new Graph<WorkflowEntity>.Execute(WorkflowOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (e, args) => 
                    {
                        WorkflowLogic.ApplyDocument(e, args.GetArg<WorkflowModel>(), args.TryGetArgC<WorkflowReplacementModel>());
                    }
                }.Register();

                sb.Include<WorkflowPoolEntity>()
                    .WithSave(WorkflowPoolOperation.Save)
                    .WithDelete(WorkflowPoolOperation.Delete)
                    .WithExpressionFrom(dqm, (WorkflowEntity p) => p.WorkflowPools())
                    .WithQuery(dqm, e => new
                    {
                        Entity = e,
                        e.Id,
                        e.Name,
                        e.Workflow,
                    });

                sb.Include<WorkflowLaneEntity>()
                    .WithSave(WorkflowLaneOperation.Save)
                    .WithDelete(WorkflowLaneOperation.Delete)
                    .WithExpressionFrom(dqm, (WorkflowPoolEntity p) => p.WorkflowLanes())
                    .WithQuery(dqm, e => new
                    {
                        Entity = e,
                        e.Id,
                        e.Name,
                        e.Pool,
                        e.Pool.Workflow,
                    });

                sb.Include<WorkflowActivityEntity>()
                    .WithIndex(w => new { w.Lane, w.Name })
                    .WithSave(WorkflowActivityOperation.Save)
                    .WithDelete(WorkflowActivityOperation.Delete)
                    .WithExpressionFrom(dqm, (WorkflowEntity p) => p.WorkflowActivities())
                    .WithExpressionFrom(dqm, (WorkflowLaneEntity p) => p.WorkflowActivities())
                    .WithQuery(dqm, e => new
                    {
                        Entity = e,
                        e.Id,
                        e.Name,
                        e.Description,
                        e.Lane,
                        e.Lane.Pool.Workflow,
                    });


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
                             var graph = new DirectedEdgedGraph<IWorkflowNodeEntity, WorkflowConnectionEntity>();

                             events.TryGetC(w).EmptyIfNull().ToList().ForEach(e => graph.Add(e));
                             gateways.TryGetC(w).EmptyIfNull().ToList().ForEach(g => graph.Add(g));
                             activities.TryGetC(w).EmptyIfNull().ToList().ForEach(a => graph.Add(a));
                             connections.TryGetC(w).EmptyIfNull().ToList().ForEach(c => graph.Add(c.From, c.To, c));

                             return new WorkflowGraph
                             {
                                 NextGraph = graph,
                                 PreviousGraph = graph.Inverse(),
                             };
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
                   .WithDelete(WorkflowConditionOperation.Delete)
                   .WithQuery(dqm, e => new
                   {
                       Entity = e,
                       e.Id,
                       e.Name,
                       e.MainEntityType,
                       e.Eval.Script
                   });

                Conditions = sb.GlobalLazy(() => Database.Query<WorkflowConditionEntity>().ToDictionary(a => a.ToLite()),
                    new InvalidateWith(typeof(WorkflowConditionEntity)));

                sb.Include<WorkflowActionEntity>()
                   .WithSave(WorkflowActionOperation.Save)
                   .WithDelete(WorkflowActionOperation.Delete)
                   .WithQuery(dqm, e => new
                   {
                       Entity = e,
                       e.Id,
                       e.Name,
                       e.MainEntityType,
                       e.Eval.Script
                   });

                Actions = sb.GlobalLazy(() => Database.Query<WorkflowActionEntity>().ToDictionary(a => a.ToLite()),
                    new InvalidateWith(typeof(WorkflowActionEntity)));
            }
        }

        public static ResetLazy<Dictionary<Lite<WorkflowConditionEntity>, WorkflowConditionEntity>> Conditions;
        public static ResetLazy<Dictionary<Lite<WorkflowActionEntity>, WorkflowActionEntity>> Actions;

        public static WorkflowConditionEntity RetrieveFromCache(this Lite<WorkflowConditionEntity> wc)
        {
            return WorkflowLogic.Conditions.Value.GetOrThrow(wc);
        }

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
            workflow.FullDiagramXml = new WorkflowXmlEntity { DiagramXml = wb.GetXDocument().ToString() };
            workflow.Save();
        }
    }


    
}
