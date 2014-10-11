using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Linq.Expressions;

namespace Signum.Entities.Basics
{
    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional), TicksColumn(false)]
    public class OperationLogDN : Entity
    {
        [ImplementedByAll]
        Lite<IEntity> target;
        public Lite<IEntity> Target
        {
            get { return target; }
            set { Set(ref target, value); }
        }

        [ImplementedByAll]
        Lite<IEntity> origin;
        public Lite<IEntity> Origin
        {
            get { return origin; }
            set { Set(ref origin, value); }
        }

        OperationSymbol operation;
        [NotNullValidator]
        public OperationSymbol Operation
        {
            get { return operation; }
            set { SetToStr(ref operation, value); }
        }

        Lite<IUserDN> user;
        [NotNullValidator]
        public Lite<IUserDN> User
        {
            get { return user; }
            set { SetToStr(ref user, value); }
        }

        DateTime start;
        public DateTime Start
        {
            get { return start; }
            set { SetToStr(ref start, value); }
        }

        DateTime? end;
        public DateTime? End
        {
            get { return end; }
            set { Set(ref end, value); }
        }


        static Expression<Func<OperationLogDN, double?>> DurationExpression =
            log => (double?)(log.End - log.Start).Value.TotalMilliseconds;
        public double? Duration
        {
            get { return end == null ? null : DurationExpression.Evaluate(this); }
        }

        Lite<ExceptionDN> exception;
        public Lite<ExceptionDN> Exception
        {
            get { return exception; }
            set { Set(ref exception, value); }
        }

        public override string ToString()
        {
            return "{0} {1} {2:d}".Formato(operation, user, start);
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
