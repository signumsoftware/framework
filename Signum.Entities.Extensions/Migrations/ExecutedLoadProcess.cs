using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Entities.Migrations
{
    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional), TicksColumn(false)]
    public class LoadMethodLogEntity : Entity
    {
        [StringLengthValidator(AllowNulls = true, Min = 3, Max = 400)]
        public string MethodName { get; set; }

        [StringLengthValidator(AllowNulls = true, Min = 3, Max = 400)]
        public string ClassName { get; set; }

        [StringLengthValidator(AllowNulls = true, Min = 3, Max = 400)]
        public string Description { get; set; }

        public DateTime Start { get; set; }

        public DateTime? End { get; set; }

        static Expression<Func<LoadMethodLogEntity, double?>> DurationExpression =
            log => (double?)(log.End - log.Start).Value.TotalMilliseconds;
        [ExpressionField("DurationExpression"), Unit("ms")]
        public double? Duration
        {
            get { return End == null ? null : DurationExpression.Evaluate(this); }
        }

        public Lite<ExceptionEntity> Exception{ get; set; }

        static Expression<Func<LoadMethodLogEntity, string>> ToStringExpression = e => e.MethodName;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }
}
