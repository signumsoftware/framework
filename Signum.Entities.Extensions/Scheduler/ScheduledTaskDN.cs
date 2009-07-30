
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Utilities;
using Signum.Entities.Extensions.Properties;

namespace Signum.Entities.Scheduler
{
    [Serializable, LocDescription]
    public class ScheduledTaskDN : IdentifiableEntity
    {
        IScheduleRule rule;
        [NotNullValidator, LocDescription]
        public IScheduleRule Rule
        {
            get { return rule; }
            set { Set(ref rule, value, "Rule"); }
        }

        ITaskDN task;
        [NotNullValidator, LocDescription]
        public ITaskDN Task
        {
            get { return task; }
            set { Set(ref task, value, "Task"); }
        }

        DateTime? nextDate = DateTime.Now;
        [LocDescription]
        public DateTime? NextDate
        {
            get { return nextDate; }
            private set { Set(ref nextDate, value, "NextDate"); }
        }

        bool suspended;
        [LocDescription]
        public bool Suspended
        {
            get { return suspended; }
            set { Set(ref suspended, value, "Suspended"); }
        }

        protected override void PreSaving()
        {
            base.PreSaving();
            NewPlan();
        }

        public void NewPlan()
        {
            NextDate = suspended ? (DateTime?)null : rule.Next(); 
        }

        public override string ToString()
        {
            return "{0} {1}".Formato(task, rule) + (suspended ? " [{0}]".Formato(Resources.ScheduledTaskDN_Suspended) : "");
        }
    }
}