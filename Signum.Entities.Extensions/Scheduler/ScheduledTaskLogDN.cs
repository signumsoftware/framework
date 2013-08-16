using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Basics;
using Signum.Utilities;
using System.Linq.Expressions;

namespace Signum.Entities.Scheduler
{
    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class ScheduledTaskLogDN : IdentifiableEntity
    {
        [ImplementedBy(typeof(SimpleTaskDN))]
        ITaskDN task;
        [NotNullValidator]
        public ITaskDN Task
        {
            get { return task; }
            set { Set(ref task, value, () => Task); }
        }

        DateTime startTime;
        [Format("G")]
        public DateTime StartTime
        {
            get { return startTime; }
            set { Set(ref startTime, value, () => StartTime); }
        }

        DateTime? endTime;
        [Format("G")]
        public DateTime? EndTime
        {
            get { return endTime; }
            set { Set(ref endTime, value, () => EndTime); }
        }

        static Expression<Func<ScheduledTaskLogDN, double?>> DurationExpression =
            log => (double?)(log.EndTime - log.StartTime).Value.TotalMilliseconds;
        public double? Duration
        {
            get { return EndTime == null ? null : DurationExpression.Evaluate(this); }
        }

        [NotNullable, SqlDbType(Size = 200)]
        string machineName;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 200)]
        public string MachineName
        {
            get { return machineName; }
            set { Set(ref machineName, value, () => MachineName); }
        }

        [ImplementedByAll]
        Lite<IIdentifiable> entity;
        public Lite<IIdentifiable> Entity
        {
            get { return entity; }
            set { Set(ref entity, value, () => Entity); }
        }

        Lite<ExceptionDN> exception;
        public Lite<ExceptionDN> Exception
        {
            get { return exception; }
            set { Set(ref exception, value, () => Exception); }
        }

        public override string ToString()
        {
            if (endTime.HasValue)
                return "{0}-{1}".Formato(startTime, endTime);
            else if (exception != null)
                return "{0} Error: {1}".Formato(startTime, exception);
            return startTime.ToString();
        }
    }
}
