using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Authorization;

namespace Signum.Entities.Logging
{
    [Serializable]
    public class ExceptionLogDN : Entity
    {
        DateTime creationDate;
        public DateTime CreationDate
        {
            get { return creationDate; }
            set { Set(ref creationDate, value, () => CreationDate); }
        }

        [NotNullable, SqlDbType(Size = 100)]
        string exceptionType;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string ExceptionType
        {
            get { return exceptionType; }
            set { Set(ref exceptionType, value, () => ExceptionType); }
        }

        [NotNullable, SqlDbType(Size = int.MaxValue)]
        string exceptionMessage;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = int.MaxValue)]
        public string ExcepcionMessage
        {
            get { return exceptionMessage; }
            set
            {
                if (Set(ref exceptionMessage, value, () => ExcepcionMessage))
                {
                    ExceptionMessageHash = value == null ? 0 : value.GetHashCode();
                }
            }
        }

        int exceptionMessageHash;
        [Format("X")]
        public int ExceptionMessageHash
        {
            get { return exceptionMessageHash; }
            internal set { Set(ref exceptionMessageHash, value, () => ExceptionMessageHash); }
        }

        [NotNullable, SqlDbType(Size = int.MaxValue)]
        string stackTrace;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = int.MaxValue)]
        public string StackTrace
        {
            get { return stackTrace; }
            set
            {
                if (Set(ref stackTrace, value, () => StackTrace))
                {
                    StackTraceHash = value == null ? 0 : value.GetHashCode();
                }
            }
        }

        int stackTraceHash;
        [Format("X")]
        public int StackTraceHash
        {
            get { return stackTraceHash; }
            internal set { Set(ref stackTraceHash, value, () => StackTraceHash); }
        }

        int threadId;
        public int ThreadId
        {
            get { return threadId; }
            set { Set(ref threadId, value, () => ThreadId); }
        }

        UserDN user;
        public UserDN User
        {
            get { return user; }
            set { Set(ref user, value, () => User); }
        }

        [ImplementedBy()]
        IdentifiableEntity context;
        public IdentifiableEntity Context
        {
            get { return context; }
            set { Set(ref context, value, () => Context); }
        }
    }
}
