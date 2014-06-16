using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using Signum.Entities.Processes;
using Signum.Entities.Authorization;

namespace Signum.Entities.Scheduler
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class ScheduledTaskDN : IdentifiableEntity
    {
        public static readonly TimeSpan MinimumSpan = TimeSpan.FromMinutes(2);


        [ImplementedBy(typeof(ScheduleRuleDailyDN), typeof(ScheduleRuleWeeklyDN), typeof(ScheduleRuleWeekDaysDN), typeof(ScheduleRuleMinutelyDN), typeof(ScheduleRuleHourlyDN))]
        IScheduleRuleDN rule;
        [NotNullValidator]
        public IScheduleRuleDN Rule
        {
            get { return rule; }
            set { SetToStr(ref rule, value); }
        }

        [ImplementedBy(typeof(SimpleTaskSymbol))]
        ITaskDN task;
        [NotNullValidator]
        public ITaskDN Task
        {
            get { return task; }
            set { SetToStr(ref task, value); }
        }

        bool suspended;
        public bool Suspended
        {
            get { return suspended; }
            set { Set(ref suspended, value); }
        }

        [NotNullable, SqlDbType(Size = 100)]
        string machineName = None;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string MachineName
        {
            get { return machineName; }
            set { Set(ref machineName, value); }
        }

        [NotNullable, SqlDbType(Size = 100)]
        string applicationName = None;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string ApplicationName
        {
            get { return applicationName; }
            set { Set(ref applicationName, value); }
        }

        public override string ToString()
        {
            return "{0} {1}".Formato(task, rule) + (suspended ? " [{0}]".Formato(ReflectionTools.GetPropertyInfo(() => Suspended).NiceName()) : "");
        }

        public const string None = "none";
    }

    public static class ScheduledTaskOperation
    {
        public static readonly ExecuteSymbol<ScheduledTaskDN> Save = OperationSymbol.Execute<ScheduledTaskDN>();
        public static readonly DeleteSymbol<ScheduledTaskDN> Delete = OperationSymbol.Delete<ScheduledTaskDN>();
    }

    public enum TaskMessage
    {
        Execute,
        Executions,
        LastExecution
    }

    public static class TaskOperation
    {
        public static readonly ConstructSymbol<IIdentifiable>.From<ITaskDN> ExecuteSync = OperationSymbol.Construct<IIdentifiable>.From<ITaskDN>();
        public static readonly ExecuteSymbol<ITaskDN> ExecuteAsync = OperationSymbol.Execute<ITaskDN>();
    }


    public static class SchedulerPermission
    {
        public static readonly PermissionSymbol ViewSchedulerPanel = new PermissionSymbol();
    }

    public interface ITaskDN : IIdentifiable
    {
    }
}