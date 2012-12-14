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
    public enum CustomTaskOperation
    {
        Execute,
    }

    public interface ITaskDN : IIdentifiable
    {
    }

    [Serializable, EntityType(EntityType.System)]
    public class CustomTaskDN : MultiEnumDN, ITaskDN
    {
        
    }

    [Serializable, EntityType(EntityType.System)]
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

        Lite<ExceptionDN> exception;
        public Lite<ExceptionDN> Exception
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
