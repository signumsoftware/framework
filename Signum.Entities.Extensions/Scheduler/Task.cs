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

    [ImplementedBy(typeof(CustomTaskDN), typeof(ProcessDN))]
    public interface ITaskDN : IIdentifiable
    {
    }

    [Serializable]
    public class CustomTaskDN : EnumDN
    {
        
    }
}
