using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Linq.Expressions;

namespace Signum.Entities.Authorization
{
    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class SessionLogDN : Entity
    {
        Lite<UserDN> user;
        [NotNullValidator]
        public Lite<UserDN> User
        {
            get { return user; }
            set { Set(ref user, value, () => User); }
        }

        DateTime sessionStart;
        [SecondsPrecissionValidator]
        public DateTime SessionStart
        {
            get { return sessionStart; }
            set { Set(ref sessionStart, value, () => SessionStart); }
        }

        DateTime? sessionEnd;
        [SecondsPrecissionValidator]
        public DateTime? SessionEnd
        {
            get { return sessionEnd; }
            set { Set(ref sessionEnd, value, () => SessionEnd); }
        }

        bool sessionTimeOut;
        public bool SessionTimeOut
        {
            get { return sessionTimeOut; }
            set { Set(ref sessionTimeOut, value, () => SessionTimeOut); }
        }

        [SqlDbType(Size = 100)]
        string userHostAddress;
        [StringLengthValidator(AllowNulls = true, Max = 100)]
        public string UserHostAddress
        {
            get { return userHostAddress; }
            set { Set(ref userHostAddress, value, () => UserHostAddress); }
        }

        [SqlDbType(Size = 300)]
        string userAgent;
        [StringLengthValidator(AllowNulls = true, Max = 300)]
        public string UserAgent
        {
            get { return userAgent; }
            set { Set(ref userAgent, value, () => UserAgent); }
        }

        public override string ToString()
        {
            return "{0} ({1}-{2})".Formato(
                user.TryToString(), sessionStart.TryToString(), sessionEnd.TryToString());
        }

        static Expression<Func<SessionLogDN, int?>> DurationExpression = 
            sl => sl.SessionEnd != null ? (sl.SessionEnd.Value - sl.SessionStart).Seconds : (int?)null;
        [Unit("s")]
        public int? Duration
        {
            get { return DurationExpression.Evaluate(this); }
        }
    }

    public enum SessionLogPermission
    { 
        TrackSession
    }
}
