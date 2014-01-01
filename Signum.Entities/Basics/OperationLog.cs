using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Linq.Expressions;

namespace Signum.Entities.Basics
{
    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class OperationLogDN : IdentifiableEntity
    {
        [ImplementedByAll]
        Lite<IIdentifiable> target;
        public Lite<IIdentifiable> Target
        {
            get { return target; }
            set { Set(ref target, value, () => Target); }
        }

        [ImplementedByAll]
        Lite<IIdentifiable> origin;
        public Lite<IIdentifiable> Origin
        {
            get { return origin; }
            set { Set(ref origin, value, () => Origin); }
        }

        OperationDN operation;
        [NotNullValidator]
        public OperationDN Operation
        {
            get { return operation; }
            set { SetToStr(ref operation, value, () => Operation); }
        }

        Lite<IUserDN> user;
        [NotNullValidator]
        public Lite<IUserDN> User
        {
            get { return user; }
            set { SetToStr(ref user, value, () => User); }
        }

        DateTime start;
        public DateTime Start
        {
            get { return start; }
            set { SetToStr(ref start, value, () => Start); }
        }

        DateTime? end;
        public DateTime? End
        {
            get { return end; }
            set { Set(ref end, value, () => End); }
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
            set { Set(ref exception, value, () => Exception); }
        }

        public override string ToString()
        {
            return "{0} {1} {2:d}".Formato(operation, user, start);
        }
    }
}
