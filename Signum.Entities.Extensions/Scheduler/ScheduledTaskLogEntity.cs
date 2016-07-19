using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Basics;
using Signum.Utilities;
using System.Linq.Expressions;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Entities.Scheduler
{
    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class ScheduledTaskLogEntity : Entity
    {
        public ScheduledTaskEntity ScheduledTask { get; set; }

        [NotNullable]
        [NotNullValidator]
        public Lite<IUserEntity> User { get; set; }

        [ImplementedBy(typeof(SimpleTaskSymbol))]
        [NotNullValidator]
        public ITaskEntity Task { get; set; }

        [Format("G")]
        public DateTime StartTime { get; set; }

        [Format("G")]
        public DateTime? EndTime { get; set; }

        static Expression<Func<ScheduledTaskLogEntity, double?>> DurationExpression =
            log => (double?)(log.EndTime - log.StartTime).Value.TotalMilliseconds;
        [ExpressionField("DurationExpression")]
        public double? Duration
        {
            get { return EndTime == null ? null : DurationExpression.Evaluate(this); }
        }

        [NotNullable, SqlDbType(Size = 200)]
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 200)]
        public string MachineName { get; set; }

        [NotNullable, SqlDbType(Size = 200)]
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 200)]
        public string ApplicationName { get; set; }

        [ImplementedByAll]
        public Lite<IEntity> ProductEntity { get; set; }

        public Lite<ExceptionEntity> Exception { get; set; }

        public override string ToString()
        {
            if (EndTime.HasValue)
                return "{0}-{1}".FormatWith(StartTime, EndTime);
            else if (Exception != null)
                return "{0} Error: {1}".FormatWith(StartTime, Exception);
            return StartTime.ToString();
        }
    }
}
