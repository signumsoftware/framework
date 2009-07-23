using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Utilities;

namespace Signum.Entities.Scheduler
{
    [Serializable]
    public class ScheduledTaskDN : IdentifiableEntity
    {
        IScheduleRule rule;
        public IScheduleRule Rule
        {
            get { return rule; }
            set { Set(ref rule, value, "Rule"); }
        }

        CustomTaskDN task;
        public CustomTaskDN Task
        {
            get { return task; }
            set { Set(ref task, value, "Task"); }
        }

        DateTime? nextDate = DateTime.Now;
        public DateTime? NextDate
        {
            get { return nextDate; }
            private set { Set(ref nextDate, value, "NextDate"); }
        }

        bool suspended;
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
            NextDate = suspended ? (DateTime?)null : rule.Next(DateTime.Now); 
        }

        public override string ToString()
        {
            return "{0} {1}".Formato(task, rule) + (suspended ? " [Suspended]" : "");
        }
    }
}