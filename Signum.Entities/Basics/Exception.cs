using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Threading;
using System.Collections.Specialized;
using System.Collections;

namespace Signum.Entities.Basics
{
    [Serializable, EntityKind(EntityKind.System)]
    public class ExceptionDN : Entity
    {
        public const string ExceptionDataKey = "exceptionEntity";

        public ExceptionDN() { }

        public ExceptionDN(Exception ex)
        {
            this.ExceptionType = ex.GetType().Name;
            this.ExceptionMessage = ex.Message;
            this.StackTrace = ex.StackTrace;
            this.ThreadId = Thread.CurrentThread.ManagedThreadId;
            ex.Data[ExceptionDataKey] = this;
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
            private set { Set(ref exceptionType, value, () => ExceptionType); }
        }

        [NotNullable, SqlDbType(Size = int.MaxValue)]
        string exceptionMessage;
        public string ExceptionMessage
        {
            get { return exceptionMessage; }
            private set
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
            private set { Set(ref exceptionMessageHash, value, () => ExceptionMessageHash); }
        }

        [NotNullable, SqlDbType(Size = int.MaxValue)]
        string stackTrace;
        [StringLengthValidator(AllowNulls = false, Min = 1, Max = int.MaxValue)]
        public string StackTrace
        {
            get { return stackTrace; }
            private set
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
            private set { Set(ref stackTraceHash, value, () => StackTraceHash); }
        }

        int threadId;
        public int ThreadId
        {
            get { return threadId; }
            private set { Set(ref threadId, value, () => ThreadId); }
        }

        Lite<IUserDN> user;
        public Lite<IUserDN> User
        {
            get { return user; }
            set { Set(ref user, value, () => User); }
        }

        [SqlDbType(Size = 100)]
        string environment;
        [StringLengthValidator(AllowNulls = true, Min = 3, Max = 100)]
        public string Environment
        {
            get { return environment; }
            set { Set(ref environment, value, () => Environment); }
        }

        [SqlDbType(Size = 100)]
        string version;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Version
        {
            get { return version; }
            set { Set(ref version, value, () => Version); }
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

        [SqlDbType(Size = int.MaxValue)]
        string data;
        public string Data
        {
            get { return data; }
            set { Set(ref data, value, () => Data); }
        }

        public override string ToString()
        {
            return "{0}: {1}".Formato(exceptionType, exceptionMessage).Etc(200);
        }

        public static string Dump(NameValueCollection nameValueCollection)
        {
            return nameValueCollection.Cast<string>().ToString(key => key + ": " + nameValueCollection[key], "\r\n");
        }
    }
}
