using Signum.Entities.Basics;
using Signum.Utilities;
using System;
using System.Linq.Expressions;

namespace Signum.Entities.Migrations
{
    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional), TicksColumn(false)]
    public class LoadMethodLogEntity : Entity
    {
        [StringLengthValidator(Min = 3, Max = 400)]
        public string? MethodName { get; set; }

        [StringLengthValidator(Min = 3, Max = 400)]
        public string? ClassName { get; set; }

        [StringLengthValidator(Min = 3, Max = 400)]
        public string? Description { get; set; }

        public DateTime Start { get; set; }

        public DateTime? End { get; set; }

        static Expression<Func<LoadMethodLogEntity, double?>> DurationExpression =
            log => (double?)(log.End - log.Start).Value.TotalMilliseconds;
#pragma warning disable SF0002 // Use ExpressionFieldAttribute in non-trivial method or property
        [ExpressionField("DurationExpression"), Unit("ms")]
#pragma warning restore SF0002 // Use ExpressionFieldAttribute in non-trivial method or property
        public double? Duration
        {
            get { return End == null ? null : DurationExpression.Evaluate(this); }
        }

        public Lite<ExceptionEntity>? Exception { get; set; }

        static Expression<Func<LoadMethodLogEntity, string>> ToStringExpression = e => e.MethodName;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }
}
