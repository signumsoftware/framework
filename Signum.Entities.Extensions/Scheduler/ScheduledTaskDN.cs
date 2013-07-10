using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using Signum.Entities.Processes;

namespace Signum.Entities.Scheduler
{
    [Serializable, EntityKind(EntityKind.Main)]
    public class ScheduledTaskDN : IdentifiableEntity
    {
        [ImplementedBy(typeof(ScheduleRuleDailyDN), typeof(ScheduleRuleWeeklyDN), typeof(ScheduleRuleWeekDaysDN), typeof(ScheduleRuleMinutelyDN), typeof(ScheduleRuleHourlyDN))]
        IScheduleRuleDN rule;
        [NotNullValidator]
        public IScheduleRuleDN Rule
        {
            get { return rule; }
            set { SetToStr(ref rule, value, () => Rule); }
        }

        [ImplementedBy(typeof(SimpleTaskDN))]
        ITaskDN task;
        [NotNullValidator]
        public ITaskDN Task
        {
            get { return task; }
            set { SetToStr(ref task, value, () => Task); }
        }

        DateTime? nextDate = TimeZoneManager.Now;
        public DateTime? NextDate
        {
            get { return nextDate; }
            private set { Set(ref nextDate, value, () => NextDate); }
        }

        bool suspended;
        public bool Suspended
        {
            get { return suspended; }
            set { Set(ref suspended, value, () => Suspended); }
        }

        protected override void PreSaving(ref bool graphModified)
        {
            base.PreSaving(ref graphModified);
            NewPlan();
        }

        public void NewPlan()
        {
            NextDate = suspended ? (DateTime?)null : rule.Next(); 
        }

        public override string ToString()
        {
            return "{0} {1}".Formato(task, rule) + (suspended ? " [{0}]".Formato(ReflectionTools.GetPropertyInfo(() => Suspended).NiceName()) : "");
        }
    }

    public enum ScheduledTaskOperation
    { 
        Save
    }

    public enum TaskMessage
    {
        Execute
    }

    public enum TaskOperation
    {
        ExecuteSync,
        ExecuteAsync,
    }

    public interface ITaskDN : IIdentifiable
    {
    }
}