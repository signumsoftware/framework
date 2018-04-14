using Signum.Engine;
using Signum.Engine.Authorization;
using Signum.Engine.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Engine.Processes;
using Signum.Engine.Scheduler;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Entities.Processes;
using Signum.Entities.Reflection;
using Signum.Entities.Scheduler;
using Signum.Entities.Workflow;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Signum.Engine.Workflow
{

    public static class WorkflowEventTaskLogic
    {

        static Expression<Func<WorkflowEventTaskEntity, IQueryable<WorkflowEventTaskConditionResultEntity>>> ConditionResultsExpression =
        e => Database.Query<WorkflowEventTaskConditionResultEntity>().Where(a => a.WorkflowEventTask.RefersTo(e));
        [ExpressionField]
        public static IQueryable<WorkflowEventTaskConditionResultEntity> ConditionResults(this WorkflowEventTaskEntity e)
        {
            return ConditionResultsExpression.Evaluate(e);
        }


        static Expression<Func<WorkflowEventEntity, ScheduledTaskEntity>> ScheduledTaskExpression =
        e => Database.Query<ScheduledTaskEntity>()
                        .SingleOrDefault(s => ((WorkflowEventTaskEntity)s.Task).Event.RefersTo(e));
        [ExpressionField]
        public static ScheduledTaskEntity ScheduledTask(this WorkflowEventEntity e)
        {
            return ScheduledTaskExpression.Evaluate(e);
        }

        static Expression<Func<WorkflowEventEntity, WorkflowEventTaskEntity>> WorkflowEventTaskExpression =
        e => Database.Query<WorkflowEventTaskEntity>()
                        .SingleOrDefault(et => et.Event.RefersTo(e));
        [ExpressionField]
        public static WorkflowEventTaskEntity WorkflowEventTask(this WorkflowEventEntity e)
        {
            return WorkflowEventTaskExpression.Evaluate(e);
        }


        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                var ib = sb.Schema.Settings.FieldAttribute<ImplementedByAttribute>(PropertyRoute.Construct((ScheduledTaskEntity e) => e.Rule));
                sb.Schema.Settings.FieldAttributes((WorkflowEventTaskModel a) => a.Rule).Replace(new ImplementedByAttribute(ib.ImplementedTypes));

                sb.Include<WorkflowEventTaskEntity>()
                    .WithDelete(WorkflowEventTaskOperation.Delete)
                    .WithQuery(dqm, () => e => new
                    {
                        Entity = e,
                        e.Id,
                        e.Workflow,
                        e.TriggeredOn,
                        e.Event,
                    });

                new Graph<WorkflowEventTaskEntity>.Execute(WorkflowEventTaskOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (e, _) => {

                        if (e.TriggeredOn == TriggeredOn.Always)
                            e.Condition = null;

                        e.Save();
                    },
                }.Register();

                sb.Schema.EntityEvents<WorkflowEventTaskEntity>().PreUnsafeDelete += tasks =>
                {
                    tasks.SelectMany(a => a.ConditionResults()).UnsafeDelete();
                    return null;
                };

                ExceptionLogic.DeleteLogs += ExceptionLogic_DeleteLogs;

                sb.Include<WorkflowEventTaskConditionResultEntity>()
                    .WithQuery(dqm, () => e => new
                    {
                        Entity = e,
                        e.Id,
                        e.CreationDate,
                        e.WorkflowEventTask,
                        e.Result,
                    });

                SchedulerLogic.ExecuteTask.Register((WorkflowEventTaskEntity wet, ScheduledTaskContext ctx) => ExecuteTask(wet));
                sb.AddIndex((WorkflowEventTaskConditionResultEntity e) => e.CreationDate);

                WorkflowEventTaskModel.GetModel = (@event) =>
                {
                    if (!@event.Type.IsTimerStart())
                        return null;

                    var schedule = @event.ScheduledTask();
                    var task = (schedule?.Task as WorkflowEventTaskEntity);
                    var triggeredOn = task?.TriggeredOn ?? TriggeredOn.Always;

                    return new WorkflowEventTaskModel
                    {
                        Suspended = schedule?.Suspended ?? true,
                        Rule = schedule?.Rule,
                        TriggeredOn = triggeredOn,
                        Condition = triggeredOn == TriggeredOn.Always ? null : new WorkflowEventTaskConditionEval() { Script = task.Condition.Script },
                        Action = new WorkflowEventTaskActionEval() { Script = task?.Action.Script ?? "" }
                    };
                };

                WorkflowEventTaskModel.ApplyModel = (@event, model) =>
                {
                    var schedule = @event.IsNew ? null : @event.ScheduledTask();

                    if (!@event.Type.IsTimerStart())
                    {
                        if (schedule != null)
                            DeleteWorkflowEventScheduledTask(schedule);
                        return;
                    }

                    if (schedule != null)
                    {
                        var task = (schedule.Task as WorkflowEventTaskEntity);
                        schedule.Suspended = model.Suspended;
                        if (!object.ReferenceEquals(schedule.Rule, model.Rule))
                        {
                            schedule.Rule = null;
                            schedule.Rule = model.Rule;
                        }
                        task.TriggeredOn = model.TriggeredOn;


                        if (model.TriggeredOn == TriggeredOn.Always)
                            task.Condition = null;
                        else
                        {
                            if (task.Condition == null)
                                task.Condition = new WorkflowEventTaskConditionEval();
                            task.Condition.Script = model.Condition.Script;
                        };

                        task.Action.Script = model.Action.Script;
                        if (GraphExplorer.IsGraphModified(schedule))
                        {
                            task.Execute(WorkflowEventTaskOperation.Save);
                            schedule.Execute(ScheduledTaskOperation.Save);
                        }
                    }
                    else
                    {
                        var newTask = new WorkflowEventTaskEntity()
                        {
                            Workflow = @event.Lane.Pool.Workflow.ToLite(),
                            Event = @event.ToLite(),
                            TriggeredOn = model.TriggeredOn,
                            Condition = model.TriggeredOn == TriggeredOn.Always ? null : new WorkflowEventTaskConditionEval() { Script = model.Condition.Script },
                            Action = new WorkflowEventTaskActionEval() { Script = model.Action.Script },
                        }.Execute(WorkflowEventTaskOperation.Save);

                        schedule = new ScheduledTaskEntity()
                        {
                            Suspended = model.Suspended,
                            Rule = model.Rule,
                            Task = newTask,
                            User = AuthLogic.SystemUser.ToLite(),
                        }.Execute(ScheduledTaskOperation.Save);
                    }
                };
            }
        }

        internal static void CloneScheduledTasks(WorkflowEventEntity oldEvent, WorkflowEventEntity newEvent)
        {
            var task = Database.Query<WorkflowEventTaskEntity>().SingleOrDefault(a => a.Event.RefersTo(oldEvent));
            if (task == null)
                return;

            var st = Database.Query<ScheduledTaskEntity>().SingleOrDefaultEx(a => a.Task == task);
            if (st == null)
                return;

            var newTask = new WorkflowEventTaskEntity()
            {
                Workflow = newEvent.Lane.Pool.Workflow.ToLite(),
                fullWorkflow = newEvent.Lane.Pool.Workflow,
                Event = newEvent.ToLite(),
                TriggeredOn = task.TriggeredOn,
                Condition = task.Condition != null ? new WorkflowEventTaskConditionEval() { Script = task.Condition.Script } : null,
                Action = new WorkflowEventTaskActionEval() { Script = task.Action.Script },
            }.Execute(WorkflowEventTaskOperation.Save);

            new ScheduledTaskEntity()
            {
                Suspended = st.Suspended,
                Rule = st.Rule.Clone(),
                Task = newTask,
                User = AuthLogic.SystemUser.ToLite(),
            }.Execute(ScheduledTaskOperation.Save);
        }

        public static void DeleteWorkflowEventScheduledTask(ScheduledTaskEntity schedule)
        {
            var workflowEventTask = (schedule.Task as WorkflowEventTaskEntity);
            schedule.Delete(ScheduledTaskOperation.Delete);
            workflowEventTask.Delete(WorkflowEventTaskOperation.Delete);
        }

        public static void ExceptionLogic_DeleteLogs(DeleteLogParametersEmbedded parameters, StringBuilder sb, CancellationToken token)
        {
            var dateLimit = parameters.GetDateLimitDelete(typeof(WorkflowEventTaskConditionResultEntity).ToTypeEntity());

            if (dateLimit == null)
                return;

            Database.Query<WorkflowEventTaskConditionResultEntity>()
               .Where(a => a.CreationDate < dateLimit.Value)
               .UnsafeDeleteChunksLog(parameters, sb, token);
        }

        public static Lite<IEntity> ExecuteTask(WorkflowEventTaskEntity wet)
        {
            using (Transaction tr = new Transaction())
            {
                if (!EvaluateCondition(wet))
                    return tr.Commit<Lite<IEntity>>(null);

                var mainEntities = wet.Action.Algorithm.EvaluateUntyped();
                var caseActivities = new List<Lite<CaseActivityEntity>>();
                foreach (var me in mainEntities)
                {
                    var @case = wet.ConstructFrom(CaseActivityOperation.CreateCaseFromWorkflowEventTask, me);
                    caseActivities.AddRange(@case.CaseActivities().Select(a => a.ToLite()).ToList());
                    caseActivities.AddRange(@case.SubCases().SelectMany(sc => sc.CaseActivities()).Select(a => a.ToLite()).ToList());
                }

                var result =
                    caseActivities.Count == 0 ? null :
                    caseActivities.Count == 1 ? (Lite<IEntity>)caseActivities.SingleEx() :
                    new PackageEntity { Name = wet.Event.ToString() + " " + TimeZoneManager.Now.ToString()}
                    .CreateLines(caseActivities).ToLite();

                return tr.Commit(result);
            }
        }

        private static bool EvaluateCondition(WorkflowEventTaskEntity task)
        {
            if (task.TriggeredOn == TriggeredOn.Always)
                return true;
            
            var result = task.Condition.Algorithm.Evaluate();
            if (task.TriggeredOn == TriggeredOn.ConditionIsTrue)
                return result;

            var last = task.ConditionResults().OrderByDescending(a => a.CreationDate).FirstOrDefault();

            new WorkflowEventTaskConditionResultEntity
            {
                WorkflowEventTask = task.ToLite(),
                Result = result
            }.Save();

            return result && (last == null || !last.Result);
        }
    }
}
