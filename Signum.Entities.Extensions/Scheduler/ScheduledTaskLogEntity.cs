using System;
using Signum.Entities.Basics;
using Signum.Utilities;
using System.Linq.Expressions;

namespace Signum.Entities.Scheduler
{
    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class ScheduledTaskLogEntity : Entity
    {
        [ImplementedBy(typeof(SimpleTaskSymbol))]
        
        public ITaskEntity Task { get; set; }

        public ScheduledTaskEntity? ScheduledTask { get; set; }

        
        public Lite<IUserEntity> User { get; set; }

        [Format("G")]
        public DateTime StartTime { get; set; }

        [Format("G")]
        public DateTime? EndTime { get; set; }

        static Expression<Func<ScheduledTaskLogEntity, double?>> DurationExpression =
            log => (double?)(log.EndTime - log.StartTime)!.Value.TotalMilliseconds;
        [ExpressionField("DurationExpression"), Unit("ms")]
        public double? Duration
        {
            get { return EndTime == null ? null : DurationExpression.Evaluate(this); }
        }

        [StringLengthValidator(Min = 3, Max = 200)]
        public string MachineName { get; set; }

        [StringLengthValidator(Min = 3, Max = 200)]
        public string ApplicationName { get; set; }


        [ImplementedByAll]
        public Lite<IEntity>? ProductEntity { get; set; }

        public Lite<ExceptionEntity>? Exception { get; set; }

        [StringLengthValidator(MultiLine = true)]
        public string? Remarks { get; set; }

        public override string ToString()
        {
            if (EndTime.HasValue)
                return "{0}-{1}".FormatWith(StartTime, EndTime);
            else if (Exception != null)
                return "{0} Error: {1}".FormatWith(StartTime, Exception);
            return StartTime.ToString();
        }
    }

    [AutoInit]
    public static class ScheduledTaskLogOperation
    {
        public static readonly ExecuteSymbol<ScheduledTaskLogEntity> CancelRunningTask;
    }

    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class SchedulerTaskExceptionLineEntity : Entity
    {
        [SqlDbType(Size = int.MaxValue)]
        public string? ElementInfo { get; set; }

        public Lite<ScheduledTaskLogEntity>? SchedulerTaskLog { get; set; }

        
        public Lite<ExceptionEntity> Exception { get; set; }
    }
}
