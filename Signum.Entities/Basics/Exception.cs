using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Threading;
using System.Collections.Specialized;
using System.Collections;
using Signum.Entities;
using System.Linq.Expressions;
using Signum.Entities.Basics;

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

        public DateTime CreationDate { get; private set; } = TimeZoneManager.Now;

        [NotNullable, SqlDbType(Size = 100)]
        public string ExceptionType { get; set; }

        [NotNullable, SqlDbType(Size = int.MaxValue)]
        string exceptionMessage;
        public string ExceptionMessage
        {
            get { return exceptionMessage; }
            set
            {
                if (Set(ref exceptionMessage, value))
                    ExceptionMessageHash = value == null ? 0 : value.GetHashCode();
            }
        }

        public int ExceptionMessageHash { get; private set; }

        [NotNullable, SqlDbType(Size = int.MaxValue)]
        string stackTrace;
        public string StackTrace
        {
            get { return stackTrace; }
            set
            {
                if (Set(ref stackTrace, value))
                    StackTraceHash = value == null ? 0 : value.GetHashCode();
            }
        }

        public int StackTraceHash { get; private set; }

        public int ThreadId { get; set; }

        public Lite<IUserEntity> User { get; set; }

        [SqlDbType(Size = 100)]
        public string Environment { get; set; }

        [SqlDbType(Size = 100)]
        public string Version { get; set; }

        [SqlDbType(Size = 300)]
        public string UserAgent { get; set; }

        [SqlDbType(Size = int.MaxValue)]
        public string RequestUrl { get; set; }

        [SqlDbType(Size = 100)]
        public string ControllerName { get; set; }

        [SqlDbType(Size = 100)]
        public string ActionName { get; set; }

        [SqlDbType(Size = int.MaxValue)]
        public string UrlReferer { get; set; }

        [SqlDbType(Size = 100)]
        public string MachineName { get; set; }

        [SqlDbType(Size = 100)]
        public string ApplicationName { get; set; }

        [SqlDbType(Size = 100)]
        public string UserHostAddress { get; set; }

        [SqlDbType(Size = 100)]
        public string UserHostName { get; set; }

        [SqlDbType(Size = int.MaxValue)]
        public string Form { get; set; }

        [SqlDbType(Size = int.MaxValue)]
        public string QueryString { get; set; }

        [SqlDbType(Size = int.MaxValue)]
        public string Session { get; set; }

        [SqlDbType(Size = int.MaxValue)]
        public string Data { get; set; }

        public int HResult { get; internal set; }

        public bool Referenced { get; set; }

        public override string ToString()
        {
            return "{0}: {1}".FormatWith(ExceptionType, exceptionMessage).Etc(200);
        }

        public static string Dump(NameValueCollection nameValueCollection)
        {
            return nameValueCollection.Cast<string>().ToString(key => key + ": " + nameValueCollection[key], "\r\n");
        }
    }


    [Serializable]
    public class DeleteLogParametersEmbedded : EmbeddedEntity
    {
        [PreserveOrder]
        [NotNullValidator, NoRepeatValidator]
        public MList<DeleteLogsTypeOverridesEmbedded> DeleteLogs { get; set; } = new MList<DeleteLogsTypeOverridesEmbedded>();

        public DateTime? GetDateLimitDelete(TypeEntity type)
        {
            var moreThan = DeleteLogs.SingleOrDefaultEx(a => a.Type.Is(type))?.DeleteLogsWithMoreThan;

            if (moreThan == null)
                return null;

            return moreThan == 0 ? TimeZoneManager.Now.TrimToHours() : TimeZoneManager.Now.Date.AddDays(-moreThan.Value);
        }

        public DateTime? GetDateLimitClean(TypeEntity type)
        {
            var moreThan = DeleteLogs.SingleOrDefaultEx(a => a.Type.Is(type))?.CleanLogsWithMoreThan;

            if (moreThan == null)
                return null;

            return moreThan.Value == 0 ? TimeZoneManager.Now.TrimToHours() : TimeZoneManager.Now.Date.AddDays(-moreThan.Value);
        }

        public int ChunkSize { get; set; } = 1000;

        public int MaxChunks { get; set; } = 20;

        [Unit("ms")]
        public int? PauseTime { get; set; } = 5000;
    }

    [Serializable]
    public class DeleteLogsTypeOverridesEmbedded : EmbeddedEntity
    {
        [NotNullValidator]
        public Lite<TypeEntity> Type { get; set; }

        [Unit("Days"), NumberIsValidator(ComparisonType.GreaterThanOrEqualTo, 0)]
        public int? DeleteLogsWithMoreThan { get; set; } = 30 * 6;

        [Unit("Days"), NumberIsValidator(ComparisonType.GreaterThanOrEqualTo, 0)]
        public int? CleanLogsWithMoreThan { get; set; } = 30 * 6;
    }
}
