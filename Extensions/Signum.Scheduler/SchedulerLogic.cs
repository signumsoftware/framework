using Signum.Utilities.DataStructures;
using Signum.Basics;
using System.Collections.Concurrent;
using Signum.UserAssets;
using Signum.Authorization;
using Signum.Authorization.Rules;
using Signum.API;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text.Json;

namespace Signum.Scheduler;

public static class SchedulerLogic
{
    [AutoExpressionField]
    public static IQueryable<ScheduledTaskLogEntity> Executions(this ITaskEntity t) =>
        As.Expression(() => Database.Query<ScheduledTaskLogEntity>().Where(a => a.Task == t));

    [AutoExpressionField]
    public static ScheduledTaskLogEntity? LastExecution(this ITaskEntity t) =>
        As.Expression(() => t.Executions().OrderByDescending(a => a.StartTime).FirstOrDefault());

    [AutoExpressionField]
    public static IQueryable<ScheduledTaskLogEntity> Executions(this ScheduledTaskEntity st) =>
        As.Expression(() => Database.Query<ScheduledTaskLogEntity>().Where(a => a.ScheduledTask.Is(st)));


    [AutoExpressionField]
    public static IQueryable<SchedulerTaskExceptionLineEntity> ExceptionLines(this ScheduledTaskLogEntity e) =>
        As.Expression(() => Database.Query<SchedulerTaskExceptionLineEntity>().Where(a => a.SchedulerTaskLog.Is(e)));

    public static Polymorphic<Func<ITaskEntity, ScheduledTaskContext, Lite<IEntity>?>> ExecuteTask =
        new Polymorphic<Func<ITaskEntity, ScheduledTaskContext, Lite<IEntity>?>>();

    public static Action<ScheduledTaskLogEntity>? OnFinally;

    public static ResetLazy<ReadOnlyCollection<ScheduledTaskEntity>> ScheduledTasksLazy = null!;

    public static void Start(SchemaBuilder sb)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        HolidayCalendarLogic.Start(sb);
        AuthLogic.AssertStarted(sb);
        OperationLogic.AssertStarted(sb);

        Implementations imp = sb.Settings.GetImplementations((ScheduledTaskEntity st) => st.Task);
        Implementations imp2 = sb.Settings.GetImplementations((ScheduledTaskLogEntity st) => st.Task);

        if (!imp2.Equals(imp2))
            throw new InvalidOperationException("Implementations of ScheduledTaskEntity.Task should be the same as in ScheduledTaskLogEntity.Task");

        PermissionLogic.RegisterPermissions(SchedulerPermission.ViewSchedulerPanel);

        ExecuteTask.Register((ITaskEntity t, ScheduledTaskContext ctx) => { throw new NotImplementedException("SchedulerLogic.ExecuteTask not registered for {0}".FormatWith(t.GetType().Name)); });

        SimpleTaskLogic.Start(sb);
        sb.Include<ScheduledTaskEntity>()
            .WithQuery(() => st => new
            {
                Entity = st,
                st.Id,
                st.Task,
                st.Rule,
                st.Suspended,
                st.MachineName,
                st.ApplicationName
            });

        sb.Include<ScheduledTaskLogEntity>()
            .WithIndex(s => s.ScheduledTask, includeFields: s => s.StartTime)
            .WithQuery(() => cte => new
            {
                Entity = cte,
                cte.Id,
                cte.Task,
                cte.ScheduledTask,
                cte.StartTime,
                cte.EndTime,
                cte.ProductEntity,
                cte.MachineName,
                cte.User,
                cte.Exception,

            });

        sb.Include<SchedulerTaskExceptionLineEntity>()
            .WithQuery(() => cte => new
            {
                Entity = cte,
                cte.Id,
                cte.ElementInfo,
                cte.Exception,
                cte.SchedulerTaskLog,
            });

        new Graph<ScheduledTaskLogEntity>.Execute(ScheduledTaskLogOperation.CancelRunningTask)
        {
            CanExecute = e => ScheduleTaskRunner.RunningTasks.ContainsKey(e) ? null : SchedulerMessage.TaskIsNotRunning.NiceToString(),
            Execute = (e, _) => { ScheduleTaskRunner.RunningTasks[e].CancellationTokenSource.Cancel(); },
        }.Register();



        QueryLogic.Expressions.Register((ITaskEntity ct) => ct.Executions(), ITaskMessage.Executions);
        QueryLogic.Expressions.Register((ITaskEntity ct) => ct.LastExecution(), ITaskMessage.LastExecution);
        QueryLogic.Expressions.Register((ScheduledTaskEntity ct) => ct.Executions(), ITaskMessage.Executions);
        QueryLogic.Expressions.Register((ScheduledTaskLogEntity ct) => ct.ExceptionLines(), ITaskMessage.ExceptionLines);



        new Graph<ScheduledTaskEntity>.Execute(ScheduledTaskOperation.Save)
        {
            CanBeNew = true,
            CanBeModified = true,
            Execute = (st, _) => { },
        }.Register();

        new Graph<ScheduledTaskEntity>.Delete(ScheduledTaskOperation.Delete)
        {
            Delete = (st, _) =>
            {
                st.Executions().UnsafeUpdate().Set(l => l.ScheduledTask, l => null).Execute();
                var rule = st.Rule;
                st.Delete();
                rule.Delete();
            },
        }.Register();


        new Graph<ScheduledTaskLogEntity>.ConstructFrom<ITaskEntity>(ITaskOperation.ExecuteSync)
        {
            Construct = (task, _) => ScheduleTaskRunner.ExecuteSync(task, null, UserHolder.Current.User.Retrieve())
        }.Register();

        ScheduledTasksLazy = sb.GlobalLazy(() =>
            Database.Query<ScheduledTaskEntity>().Where(a => !a.Suspended &&
                (a.MachineName == ScheduledTaskEntity.None || a.MachineName == Schema.Current.MachineName && a.ApplicationName == Schema.Current.ApplicationName)).ToReadOnly(),
            new InvalidateWith(typeof(ScheduledTaskEntity)));

        ScheduledTasksLazy.OnReset += ScheduleTaskRunner.ScheduledTasksLazy_OnReset;

        sb.Schema.EntityEvents<ScheduledTaskLogEntity>().PreUnsafeDelete += query =>
        {
            query.SelectMany(e => e.ExceptionLines()).UnsafeDelete();
            return null;
        };

        UserAssetsImporter.Register<ScheduleRuleMinutelyEntity>("ScheduleRuleMinutely", e => e.Save());
        UserAssetsImporter.Register<ScheduleRuleMonthsEntity>("ScheduleRuleMonths", e => e.Save());
        UserAssetsImporter.Register<ScheduleRuleWeekDaysEntity>("ScheduleRuleWeekDays", e => e.Save());
        UserAssetsImporter.Register<HolidayCalendarEntity>("HolidayCalendar", HolidayCalendarOperation.Save);

        ExceptionLogic.DeleteLogs += ExceptionLogic_DeleteLogs;

        if (sb.WebServerBuilder != null)
            SchedulerServer.Start(sb.WebServerBuilder);
    }

    public static void ExceptionLogic_DeleteLogs(DeleteLogParametersEmbedded parameters, StringBuilder sb, CancellationToken token)
    {
        void Remove(DateTime? dateLimit, bool withExceptions)
        {
            if (dateLimit == null)
                return;

            var query = Database.Query<ScheduledTaskLogEntity>().Where(a => a.StartTime < dateLimit);

            if (withExceptions)
                query = query.Where(a => a.Exception != null);

            query.SelectMany(a => a.ExceptionLines()).UnsafeDeleteChunksLog(parameters, sb, token);
            query.Where(a => !a.ExceptionLines().Any()).UnsafeDeleteChunksLog(parameters, sb, token);
        }

        Database.Query<SchedulerTaskExceptionLineEntity>().Where(a => a.SchedulerTaskLog == null).UnsafeDeleteChunksLog(parameters, sb, token);
        Remove(parameters.GetDateLimitDelete(typeof(ScheduledTaskLogEntity).ToTypeEntity()), withExceptions: false);
        Remove(parameters.GetDateLimitDeleteWithExceptions(typeof(ScheduledTaskLogEntity).ToTypeEntity()), withExceptions: true);
    }
}
