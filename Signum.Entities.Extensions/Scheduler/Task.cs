using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Basics;
using Signum.Entities;
using Signum.Entities.Processes;
using Signum.Utilities;
using Signum.Entities.Authorization;

namespace Signum.Entities.Scheduler
{
    public enum TaskOperation
    {
        ExecutePrivate
    }

    public enum CustomTaskOperation
    {
        Execute,
    }

    [ImplementedBy(typeof(CustomTaskDN), typeof(ProcessDN)), ]
    public interface ITaskDN : IIdentifiable
    {
    }

    [Serializable]
    public class CustomTaskDN : EnumDN, ITaskDN
    {
        
    }

    [Serializable]
    public class CustomTaskExecutionDN : IdentifiableEntity
    {
        CustomTaskDN customTask;
        public CustomTaskDN CustomTask
        {
            get { return customTask; }
            set { Set(ref customTask, value, () => CustomTask); }
        }

        DateTime startTime;
        public DateTime StartTime
        {
            get { return startTime; }
            set { Set(ref startTime, value, () => StartTime); }
        }

        DateTime? endTime;
        public DateTime? EndTime
        {
            get { return endTime; }
            set { Set(ref endTime, value, () => EndTime); }
        }

        [SqlDbType(Size = int.MaxValue)]
        string exception;
        public string Exception
        {
            get { return exception; }
            set { Set(ref exception, value, () => Exception); }
        }

        public override string ToString()
        {
            if (endTime.HasValue)
                return "{0}-{1}".Formato(startTime, endTime);
            else if (exception != null)
                return "{0} Error: {1}".Formato(startTime, exception);
            return startTime.ToString(); 
        }
    }
}
