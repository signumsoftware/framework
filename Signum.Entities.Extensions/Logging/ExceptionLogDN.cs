using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Authorization;
using Signum.Utilities;

namespace Signum.Entities.Logging
{
    [Serializable]
    public class ExceptionLogDN : Entity
    {
        DateTime creationDate = TimeZoneManager.Now;
        public DateTime CreationDate
        {
            get { return creationDate; }
            private set { Set(ref creationDate, value, () => CreationDate); }
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
        public string ExceptionMessage
        {
            get { return exceptionMessage; }
            set
            {
                if (Set(ref exceptionMessage, value, () => ExceptionMessage))
                {
                    ExceptionMessageHash = value == null ? 0 : value.GetHashCode();
                }
            }
        }

        int exceptionMessageHash;
        public int ExceptionMessageHash
        {
            get { return exceptionMessageHash; }
            internal set { Set(ref exceptionMessageHash, value, () => ExceptionMessageHash); }
        }

        [NotNullable, SqlDbType(Size = int.MaxValue)]
        string stackTrace;
        [StringLengthValidator(AllowNulls = false, Min = 1, Max = int.MaxValue)]
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

        [NotNullable, SqlDbType(Size = 300)]
        string userAgent;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 300)]
        public string UserAgent
        {
            get { return userAgent; }
            set { Set(ref userAgent, value, () => UserAgent); }
        }

        [NotNullable, SqlDbType(Size = int.MaxValue)]
        string requestUrl;
        [StringLengthValidator(AllowNulls = false, Min = 3)]
        public string RequestUrl
        {
            get { return requestUrl; }
            set { Set(ref requestUrl, value, () => RequestUrl); }
        }

        [NotNullable, SqlDbType(Size = 100)]
        string controllerName;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string ControllerName
        {
            get { return controllerName; }
            set { Set(ref controllerName, value, () => ControllerName); }
        }

        [NotNullable, SqlDbType(Size = 100)]
        string actionName;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string ActionName
        {
            get { return actionName; }
            set { Set(ref actionName, value, () => ActionName); }
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


        [SqlDbType(Size = int.MaxValue)]
        string data;
        [StringLengthValidator(AllowNulls = true, Max = int.MaxValue)]
        public string Data
        {
            get { return data; }
            set { Set(ref data, value, () => Data); }
        }

        public override string ToString()
        {
            return "{0}: {1}".Formato(exceptionType, exceptionMessage).Etc(200);
        }
    }
}
