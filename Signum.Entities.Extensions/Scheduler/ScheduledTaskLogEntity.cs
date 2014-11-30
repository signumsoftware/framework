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
    public class ScheduledTaskLogEntity : Entity
    {
        ScheduledTaskEntity scheduledTask;
        public ScheduledTaskEntity ScheduledTask
        {
            get { return scheduledTask; }
            set { Set(ref scheduledTask, value); }
        }

        Lite<IUserEntity> user;
        public Lite<IUserEntity> User
        {
            get { return user; }
            set { Set(ref user, value); }
        }

        [ImplementedBy(typeof(SimpleTaskSymbol))]
        ITaskEntity task;
        [NotNullValidator]
        public ITaskEntity Task
        {
            get { return task; }
            set { Set(ref task, value); }
        }

        DateTime startTime;
        [Format("G")]
        public DateTime StartTime
        {
            get { return startTime; }
            set { Set(ref startTime, value); }
        }

        DateTime? endTime;
        [Format("G")]
        public DateTime? EndTime
        {
            get { return endTime; }
            set { Set(ref endTime, value); }
        }

        static Expression<Func<ScheduledTaskLogEntity, double?>> DurationExpression =
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
            set { Set(ref machineName, value); }
        }

        [NotNullable, SqlDbType(Size = 200)]
        string applicationName;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 200)]
        public string ApplicationName
        {
            get { return applicationName; }
            set { Set(ref applicationName, value); }
        }

        [ImplementedByAll]
        Lite<IEntity> productEntity;
        public Lite<IEntity> ProductEntity
        {
            get { return productEntity; }
            set { Set(ref productEntity, value); }
        }

        Lite<ExceptionEntity> exception;
        public Lite<ExceptionEntity> Exception
        {
            get { return exception; }
            set { Set(ref exception, value); }
        }

        public override string ToString()
        {
            if (endTime.HasValue)
                return "{0}-{1}".FormatWith(startTime, endTime);
            else if (exception != null)
                return "{0} Error: {1}".FormatWith(startTime, exception);
            return startTime.ToString();
        }
    }
}
