
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Utilities;
using Signum.Entities.Extensions.Properties;
using Signum.Utilities.Reflection;

namespace Signum.Entities.Scheduler
{
    [Serializable]
    public class ScheduledTaskDN : IdentifiableEntity
    {
        IScheduleRuleDN rule;
        [NotNullValidator]
        public IScheduleRuleDN Rule
        {
            get { return rule; }
            set { SetToStr(ref rule, value, () => Rule); }
        }

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
}