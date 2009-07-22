using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Basics;
using Signum.Entities;
using Signum.Entities.Processes;

namespace Signum.Entities.Scheduler
{
    public enum TaskOperation
    {
        Execute
    }

    [ImplementedBy(typeof(CustomTaskDN), typeof(ProcessTaskDN))]
    public interface ITask: IIdentifiable
    {

    }

    [Serializable]
    public class CustomTaskDN : EnumDN, ITask
    {
        
    }

    [Serializable]
    public class ProcessTaskDN: IdentifiableEntity
    {
        ProcessDN process;
        [NotNullValidator]
        public ProcessDN Process
        {
            get { return process; }
            set { Set(ref process, value, "Process"); }
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }
}
