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
    [Serializable, EntityKind(EntityKind.SystemString)]
    public class ActionTaskDN : MultiEnumDN, ITaskDN
    {
        
    }
}
