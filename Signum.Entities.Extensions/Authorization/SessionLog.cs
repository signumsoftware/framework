using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Linq.Expressions;

namespace Signum.Entities.Authorization
{
    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class SessionLogEntity : Entity
    {
        Lite<UserEntity> user;
        [NotNullValidator]
        public Lite<UserEntity> User
        {
            get { return user; }
            set { Set(ref user, value); }
        }

        DateTime sessionStart;
        [SecondsPrecissionValidator]
        public DateTime SessionStart
        {
            get { return sessionStart; }
            set { Set(ref sessionStart, value); }
        }

        DateTime? sessionEnd;
        [SecondsPrecissionValidator]
        public DateTime? SessionEnd
        {
            get { return sessionEnd; }
            set { Set(ref sessionEnd, value); }
        }

        bool sessionTimeOut;
        public bool SessionTimeOut
        {
            get { return sessionTimeOut; }
            set { Set(ref sessionTimeOut, value); }
        }

        [SqlDbType(Size = 100)]
        string userHostAddress;
        [StringLengthValidator(AllowNulls = true, Max = 100)]
        public string UserHostAddress
        {
            get { return userHostAddress; }
            set { Set(ref userHostAddress, value); }
        }

        [SqlDbType(Size = 300)]
        string userAgent;
        [StringLengthValidator(AllowNulls = true, Max = 300)]
        public string UserAgent
        {
            get { return userAgent; }
            set { Set(ref userAgent, value); }
        }

        public override string ToString()
        {
            return "{0} ({1}-{2})".FormatWith(
                user.TryToString(), sessionStart.TryToString(), sessionEnd.TryToString());
        }

        static Expression<Func<SessionLogEntity, double?>> DurationExpression = 
            sl => sl.SessionEnd != null ? (sl.SessionEnd.Value - sl.SessionStart).TotalSeconds : (double?)null;
        [Unit("s")]
        public double? Duration
        {
            get { return DurationExpression.Evaluate(this); }
        }
    }

    public static class SessionLogPermission
    {
        public static readonly PermissionSymbol TrackSession = new PermissionSymbol();
    }
}
