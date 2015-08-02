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
    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional), TicksColumn(false)]
    public class ExceptionEntity : Entity
    {
        public const string ExceptionDataKey = "exceptionEntity";

        public ExceptionEntity() { }

        public ExceptionEntity(Exception ex)
        {
            this.ExceptionType = ex.GetType().Name;
            this.ExceptionMessage = ex.Message;
            this.StackTrace = ex.StackTrace;
            this.ThreadId = Thread.CurrentThread.ManagedThreadId;
            ex.Data[ExceptionDataKey] = this;
            this.MachineName = System.Environment.MachineName;
            this.ApplicationName = AppDomain.CurrentDomain.FriendlyName;
        }

        DateTime creationDate = TimeZoneManager.Now;
        public DateTime CreationDate
        {
            get { return creationDate; }
            private set { Set(ref creationDate, value); }
        }

        [NotNullable, SqlDbType(Size = 100)]
        string exceptionType;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string ExceptionType
        {
            get { return exceptionType; }
            set { Set(ref exceptionType, value); }
        }

        [NotNullable, SqlDbType(Size = int.MaxValue)]
        string exceptionMessage;
        public string ExceptionMessage
        {
            get { return exceptionMessage; }
            set
            {
                if (Set(ref exceptionMessage, value))
                {
                    ExceptionMessageHash = value == null ? 0 : value.GetHashCode();
                }
            }
        }

        int exceptionMessageHash;
        public int ExceptionMessageHash
        {
            get { return exceptionMessageHash; }
            private set { Set(ref exceptionMessageHash, value); }
        }

        [NotNullable, SqlDbType(Size = int.MaxValue)]
        string stackTrace;
        [StringLengthValidator(AllowNulls = false, Min = 1, MultiLine = true)]
        public string StackTrace
        {
            get { return stackTrace; }
            set
            {
                if (Set(ref stackTrace, value))
                {
                    StackTraceHash = value == null ? 0 : value.GetHashCode();
                }
            }
        }

        int stackTraceHash;
        public int StackTraceHash
        {
            get { return stackTraceHash; }
            private set { Set(ref stackTraceHash, value); }
        }

        int threadId;
        public int ThreadId
        {
            get { return threadId; }
            set { Set(ref threadId, value); }
        }

        Lite<IUserEntity> user;
        public Lite<IUserEntity> User
        {
            get { return user; }
            set { Set(ref user, value); }
        }

        [SqlDbType(Size = 100)]
        string environment;
        [StringLengthValidator(AllowNulls = true, Min = 3, Max = 100)]
        public string Environment
        {
            get { return environment; }
            set { Set(ref environment, value); }
        }

        [SqlDbType(Size = 100)]
        string version;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Version
        {
            get { return version; }
            set { Set(ref version, value); }
        }

        [SqlDbType(Size = 300)]
        string userAgent;
        [StringLengthValidator(AllowNulls = true, Max = 300)]
        public string UserAgent
        {
            get { return userAgent; }
            set { Set(ref userAgent, value); }
        }

        [SqlDbType(Size = int.MaxValue)]
        string requestUrl;
        public string RequestUrl
        {
            get { return requestUrl; }
            set { Set(ref requestUrl, value); }
        }

        [SqlDbType(Size = 100)]
        string controllerName;
        [StringLengthValidator(AllowNulls = true, Max = 100)]
        public string ControllerName
        {
            get { return controllerName; }
            set { Set(ref controllerName, value); }
        }

        [SqlDbType(Size = 100)]
        string actionName;
        [StringLengthValidator(AllowNulls = true, Max = 100)]
        public string ActionName
        {
            get { return actionName; }
            set { Set(ref actionName, value); }
        }

        [SqlDbType(Size = int.MaxValue)]
        string urlReferer;
        public string UrlReferer
        {
            get { return urlReferer; }
            set { Set(ref urlReferer, value); }
        }

        [SqlDbType(Size = 100)]
        string machineName;
        [StringLengthValidator(AllowNulls = true, Max = 100)]
        public string MachineName
        {
            get { return machineName; }
            set { Set(ref machineName, value); }
        }

        [SqlDbType(Size = 100)]
        string applicationName;
        [StringLengthValidator(AllowNulls = true, Max = 100)]
        public string ApplicationName
        {
            get { return applicationName; }
            set { Set(ref applicationName, value); }
        }

        [SqlDbType(Size = 100)]
        string userHostAddress;
        [StringLengthValidator(AllowNulls = true, Max = 100)]
        public string UserHostAddress
        {
            get { return userHostAddress; }
            set { Set(ref userHostAddress, value); }
        }

        [SqlDbType(Size = 100)]
        string userHostName;
        [StringLengthValidator(AllowNulls = true, Min = 3, Max = 100)]
        public string UserHostName
        {
            get { return userHostName; }
            set { Set(ref userHostName, value); }
        }

        [SqlDbType(Size = int.MaxValue)]
        string form;
        public string Form
        {
            get { return form; }
            set { Set(ref form, value); }
        }

        [SqlDbType(Size = int.MaxValue)]
        string queryString;
        public string QueryString
        {
            get { return queryString; }
            set { Set(ref queryString, value); }
        }

        [SqlDbType(Size = int.MaxValue)]
        string session;
        public string Session
        {
            get { return session; }
            set { Set(ref session, value); }
        }

        [SqlDbType(Size = int.MaxValue)]
        string data;
        public string Data
        {
            get { return data; }
            set { Set(ref data, value); }
        }

        bool referenced;
        public bool Referenced
        {
            get { return referenced; }
            set { Set(ref referenced, value); }
        }

        public override string ToString()
        {
            return "{0}: {1}".FormatWith(exceptionType, exceptionMessage).Etc(200);
        }

        public static string Dump(NameValueCollection nameValueCollection)
        {
            return nameValueCollection.Cast<string>().ToString(key => key + ": " + nameValueCollection[key], "\r\n");
        }
    }


    [Serializable]
    public class DeleteLogParametersEntity : EmbeddedEntity
    {
        int deleteLogsWithMoreThan = 30 * 6;
        [Unit("Days"), NumberIsValidator(ComparisonType.GreaterThan, 0)]
        public int DeleteLogsWithMoreThan
        {
            get { return deleteLogsWithMoreThan; }
            set { Set(ref deleteLogsWithMoreThan, value); }
        }

        public DateTime DateLimit
        {
            get { return DateTime.Today.AddDays(-DeleteLogsWithMoreThan); }
        }

        int chunkSize = 1000;
        public int ChunkSize
        {
            get { return chunkSize; }
            set { Set(ref chunkSize, value); }
        }

        int maxChunks;
        public int MaxChunks
        {
            get { return maxChunks; }
            set { Set(ref maxChunks, value); }
        }
    }
}
