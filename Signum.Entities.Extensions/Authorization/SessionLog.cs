using System;
using Signum.Utilities;
using System.Linq.Expressions;

namespace Signum.Entities.Authorization
{
    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class SessionLogEntity : Entity
    {
        
        public Lite<UserEntity> User { get; set; }

        [SecondsPrecisionValidator]
        public DateTime SessionStart { get; set; }

        [SecondsPrecisionValidator]
        public DateTime? SessionEnd { get; set; }

        public bool SessionTimeOut { get; set; }

        [StringLengthValidator(Max = 100)]
        public string? UserHostAddress { get; set; }

        [StringLengthValidator(Max = 300)]
        public string? UserAgent { get; set; }

        public override string ToString()
        {
            return "{0} ({1}-{2})".FormatWith(
                User?.ToString(), SessionStart, SessionEnd);
        }

        static Expression<Func<SessionLogEntity, double?>> DurationExpression =
            sl => sl.SessionEnd != null ? (sl.SessionEnd.Value - sl.SessionStart).TotalSeconds : (double?)null;
        [ExpressionField, Unit("s")]
        public double? Duration
        {
            get { return DurationExpression.Evaluate(this); }
        }
    }

    [AutoInit]
    public static class SessionLogPermission
    {
        public static PermissionSymbol TrackSession;
    }
}
