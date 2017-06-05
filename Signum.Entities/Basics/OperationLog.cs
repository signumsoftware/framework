using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Linq.Expressions;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Entities.Basics
{
    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional), TicksColumn(false), InTypeScript(Undefined = false)]
    public class OperationLogEntity : Entity
    {
        [ImplementedByAll]
        public Lite<IEntity> Target { get; set; }

        [ImplementedByAll]
        public Lite<IEntity> Origin { get; set; }

        [NotNullValidator]
        public OperationSymbol Operation { get; set; }

        [NotNullValidator]
        public Lite<IUserEntity> User { get; set; }

        public DateTime Start { get; set; }

        public DateTime? End { get; set; }

        static Expression<Func<OperationLogEntity, double?>> DurationExpression =
            log => (double?)(log.End - log.Start).Value.TotalMilliseconds;
        [ExpressionField("DurationExpression"), Unit("ms")]
        public double? Duration
        {
            get { return End == null ? null : DurationExpression.Evaluate(this); }
        }

        public Lite<ExceptionEntity> Exception { get; set; }

        public override string ToString()
        {
            return "{0} {1} {2:d}".FormatWith(Operation, User, Start);
        }

        public void SetTarget(IEntity target)
        {
            this.TemporalTarget = target;
            this.Target = target == null || target.IsNew ? null : target.ToLite();
        }

        public IEntity GetTarget()
        {
            return TemporalTarget;
        }

        [Ignore]
        IEntity TemporalTarget;
    }

 
}
