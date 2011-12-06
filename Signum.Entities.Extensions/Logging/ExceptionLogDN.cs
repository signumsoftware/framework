using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Authorization;
using Signum.Utilities;
using System.Threading;

namespace Signum.Entities.Logging
{
    [Serializable]
    public class ExceptionLogDN : Entity
    {
        public ExceptionLogDN() { }

        public ExceptionLogDN(Exception ex)
        {
            this.ExceptionType = ex.GetType().Name;
            this.ExceptionMessage = ex.Message;
            this.StackTrace = ex.StackTrace;
            this.ThreadId = Thread.CurrentThread.ManagedThreadId;
        }

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

        Lite<UserDN> user;
        public Lite<UserDN> User
        {
            get { return user; }
            set { Set(ref user, value, () => User); }
        }

        [SqlDbType(Size = 300)]
        string userAgent;
        [StringLengthValidator(AllowNulls = true, Max = 300)]
        public string UserAgent
        {
            get { return userAgent; }
            set { Set(ref userAgent, value, () => UserAgent); }
        }

        [SqlDbType(Size = int.MaxValue)]
        string requestUrl;
        public string RequestUrl
        {
            get { return requestUrl; }
            set { Set(ref requestUrl, value, () => RequestUrl); }
        }

        [SqlDbType(Size = 100)]
        string enviroment;
        [StringLengthValidator(AllowNulls = false, Max = 100)]
        public string Enviroment
        {
            get { return enviroment; }
            set { Set(ref enviroment, value, () => Enviroment); }
        }
     
        [SqlDbType(Size = 100)]
        string controllerName;
        [StringLengthValidator(AllowNulls = true, Max = 100)]
        public string ControllerName
        {
            get { return controllerName; }
            set { Set(ref controllerName, value, () => ControllerName); }
        }

        [SqlDbType(Size = 100)]
        string actionName;
        [StringLengthValidator(AllowNulls = true,  Max = 100)]
        public string ActionName
        {
            get { return actionName; }
            set { Set(ref actionName, value, () => ActionName); }
        }

        [SqlDbType(Size = int.MaxValue)]
        string urlReferer;
        public string UrlReferer
        {
            get { return urlReferer; }
            set { Set(ref urlReferer, value, () => UrlReferer); }
        }

        [SqlDbType(Size = 100)]
        string userHostAddress;
        [StringLengthValidator(AllowNulls = true,  Max = 100)]
        public string UserHostAddress
        {
            get { return userHostAddress; }
            set { Set(ref userHostAddress, value, () => UserHostAddress); }
        }

        [SqlDbType(Size = 100)]
        string userHostName;
        [StringLengthValidator(AllowNulls = true, Min = 3, Max = 100)]
        public string UserHostName
        {
            get { return userHostName; }
            set { Set(ref userHostName, value, () => UserHostName); }
        }

        [SqlDbType(Size = int.MaxValue)]
        string form;
        public string Form
        {
            get { return form; }
            set { Set(ref form, value, () => Form); }
        }

        [SqlDbType(Size = int.MaxValue)]
        string queryString;
        public string QueryString
        {
            get { return queryString; }
            set { Set(ref queryString, value, () => QueryString); }
        }

        [SqlDbType(Size = int.MaxValue)]
        string session;
        public string Session
        {
            get { return session; }
            set { Set(ref session, value, () => Session); }
        }

        public override string ToString()
        {
            return "{0}: {1}".Formato(exceptionType, exceptionMessage).Etc(200);
        }
    }
}
