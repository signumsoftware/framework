using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using Signum.Entities.Processes;
using Signum.Entities.Authorization;
using Signum.Entities.Basics;

namespace Signum.Entities.Scheduler
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class ScheduledTaskEntity : Entity
    {
        [ImplementedBy(typeof(ScheduleRuleDailyEntity), typeof(ScheduleRuleWeeklyEntity), typeof(ScheduleRuleWeekDaysEntity), typeof(ScheduleRuleMinutelyEntity), typeof(ScheduleRuleHourlyEntity))]
        IScheduleRuleEntity rule;
        [NotNullValidator]
        public IScheduleRuleEntity Rule
        {
            get { return rule; }
            set { Set(ref rule, value); }
        }

        [ImplementedBy(typeof(SimpleTaskSymbol))]
        ITaskEntity task;
        [NotNullValidator]
        public ITaskEntity Task
        {
            get { return task; }
            set { Set(ref task, value); }
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

        [NotNullable]
        Lite<IUserEntity> user;
        [NotNullValidator]
        public Lite<IUserEntity> User
        {
            get { return user; }
            set { Set(ref user, value); }
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
            return "{0} {1}".FormatWith(task, rule) + (suspended ? " [{0}]".FormatWith(ReflectionTools.GetPropertyInfo(() => Suspended).NiceName()) : "");
        }

        public const string None = "none";
    }

    [AutoInit]
    public static class ScheduledTaskOperation
    {
        public static ExecuteSymbol<ScheduledTaskEntity> Save;
        public static DeleteSymbol<ScheduledTaskEntity> Delete;
    }

    public enum TaskMessage
    {
        Execute,
        Executions,
        LastExecution
    }

    [AutoInit]
    public static class TaskOperation
    {
        public static ConstructSymbol<IEntity>.From<ITaskEntity> ExecuteSync;
        public static ExecuteSymbol<ITaskEntity> ExecuteAsync;
    }



    [AutoInit]
    public static class SchedulerPermission
    {
        public static PermissionSymbol ViewSchedulerPanel;
    }

    public interface ITaskEntity : IEntity
    {
    }
}