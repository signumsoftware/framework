using Signum.Authorization;
using Signum.Processes;
using Signum.Scheduler;

namespace Signum.Workflow;


public static class WorkflowEventTaskLogic
{
    [AutoExpressionField]
    public static IQueryable<WorkflowEventTaskConditionResultEntity> ConditionResults(this WorkflowEventTaskEntity e) => 
        As.Expression(() => Database.Query<WorkflowEventTaskConditionResultEntity>().Where(a => a.WorkflowEventTask.Is(e)));

    [AutoExpressionField]
    public static ScheduledTaskEntity? ScheduledTask(this WorkflowEventEntity e) =>
        As.Expression(() => Database.Query<ScheduledTaskEntity>().SingleOrDefault(s => ((WorkflowEventTaskEntity)s.Task).Event.Is(e)));

    [AutoExpressionField]
    public static WorkflowEventTaskEntity? WorkflowEventTask(this WorkflowEventEntity e) =>
        As.Expression(() => Database.Query<WorkflowEventTaskEntity>().SingleOrDefault(et => et.Event.Is(e)));

    public static void Start(SchemaBuilder sb)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        var ib = sb.Schema.Settings.FieldAttribute<ImplementedByAttribute>(PropertyRoute.Construct((ScheduledTaskEntity e) => e.Rule))!;
        sb.Schema.Settings.FieldAttributes((WorkflowEventTaskModel a) => a.Rule).Replace(new ImplementedByAttribute(ib.ImplementedTypes));

        sb.Include<WorkflowEventTaskEntity>()
            .WithDelete(WorkflowEventTaskOperation.Delete)
            .WithQuery(() => e => new
            {
                Entity = e,
                e.Id,
                e.Workflow,
                e.TriggeredOn,
                e.Event,
            });

        new Graph<WorkflowEventTaskEntity>.Execute(WorkflowEventTaskOperation.Save)
        {
            CanBeNew = true,
            CanBeModified = true,
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
            .WithQuery(() => e => new
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
            if (!@event.Type.IsScheduledStart())
                return null;

            var schedule = @event.ScheduledTask();
            var task = (schedule?.Task as WorkflowEventTaskEntity);
            var triggeredOn = task?.TriggeredOn ?? TriggeredOn.Always;

            return new WorkflowEventTaskModel
            {
                Suspended = schedule?.Suspended ?? true,
                Rule = schedule?.Rule,
                TriggeredOn = triggeredOn,
                Condition = triggeredOn == TriggeredOn.Always ? null : new WorkflowEventTaskConditionEval() { Script = task!.Condition!.Script },
                Action = new WorkflowEventTaskActionEval() { Script = task?.Action!.Script ?? "" }
            };
        };

        WorkflowEventTaskModel.ApplyModel = (@event, model) =>
        {
            var schedule = @event.IsNew ? null : @event.ScheduledTask();

            if (!@event.Type.IsScheduledStart())
            {
                if (schedule != null)
                    DeleteWorkflowEventScheduledTask(schedule);
                return;
            }

            if (model == null)
                throw new ArgumentNullException(nameof(model));
            
            if (schedule != null)
            {
                var task = (WorkflowEventTaskEntity)schedule.Task;
                schedule.Suspended = model.Suspended;
                if (!object.ReferenceEquals(schedule.Rule, model.Rule))
                {
                    schedule.Rule = null!;
                    schedule.Rule = model.Rule!;
                }
                task.TriggeredOn = model.TriggeredOn;


                if (model.TriggeredOn == TriggeredOn.Always)
                    task.Condition = null;
                else
                {
                    if (task.Condition == null)
                        task.Condition = new WorkflowEventTaskConditionEval();
                    task.Condition.Script = model.Condition!.Script;
                };

                task.Action!.Script = model.Action!.Script;
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
                    Condition = model.TriggeredOn == TriggeredOn.Always ? null : new WorkflowEventTaskConditionEval() { Script = model.Condition!.Script },
                    Action = new WorkflowEventTaskActionEval() { Script = model.Action!.Script },
                }.Execute(WorkflowEventTaskOperation.Save);

                schedule = new ScheduledTaskEntity
                {
                    Suspended = model.Suspended,
                    Rule = model.Rule!,
                    Task = newTask,
                    User = AuthLogic.SystemUser!.ToLite(),
                }.Execute(ScheduledTaskOperation.Save);
            }
        };
    }

    internal static void CloneScheduledTasks(WorkflowEventEntity oldEvent, WorkflowEventEntity newEvent)
    {
        var task = Database.Query<WorkflowEventTaskEntity>().SingleOrDefault(a => a.Event.Is(oldEvent));
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
            Action = new WorkflowEventTaskActionEval() { Script = task.Action!.Script },
        }.Execute(WorkflowEventTaskOperation.Save);

        new ScheduledTaskEntity()
        {
            Suspended = st.Suspended,
            Rule = st.Rule.Clone(),
            Task = newTask,
            User = AuthLogic.SystemUser!.ToLite(),
        }.Execute(ScheduledTaskOperation.Save);
    }

    public static void DeleteWorkflowEventScheduledTask(ScheduledTaskEntity schedule)
    {
        var workflowEventTask = ((WorkflowEventTaskEntity)schedule.Task);
        schedule.Delete(ScheduledTaskOperation.Delete);
        workflowEventTask.Delete(WorkflowEventTaskOperation.Delete);
    }

    public static void ExceptionLogic_DeleteLogs(DeleteLogParametersEmbedded parameters, StringBuilder sb, CancellationToken token)
    {
        var dateLimit = parameters.GetDateLimitDelete(typeof(WorkflowEventTaskConditionResultEntity).ToTypeEntity());
        if (dateLimit != null)
            Database.Query<WorkflowEventTaskConditionResultEntity>()
               .Where(a => a.CreationDate < dateLimit.Value)
               .UnsafeDeleteChunksLog(parameters, sb, token);
    }

    public static Lite<IEntity>? ExecuteTask(WorkflowEventTaskEntity wet)
    {
        var workflow = wet.GetWorkflow();

        if (workflow.HasExpired())
            throw new InvalidOperationException(WorkflowMessage.Workflow0HasExpiredOn1.NiceToString(workflow, workflow.ExpirationDate!.Value.ToString()));

        using (var tr = new Transaction())
        {
            if (!EvaluateCondition(wet))
                return tr.Commit<Lite<IEntity>?>(null);

            var mainEntities = wet.Action!.Algorithm.EvaluateUntyped();
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
                new PackageEntity { Name = wet.Event.ToString() + " " + Clock.Now.ToString()}
                .CreateLines(caseActivities).ToLite();

            return tr.Commit(result);
        }
    }

    private static bool EvaluateCondition(WorkflowEventTaskEntity task)
    {
        if (task.TriggeredOn == TriggeredOn.Always)
            return true;
        
        var result = task.Condition!.Algorithm.Evaluate();
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
