using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signum.Entities.Scheduler
{
    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class SystemEventLogEntity : Entity
    {
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string MachineName { get; set; }

        public DateTime Date { get; set; }

        public Lite<IUserEntity> User { get; set; }

        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string EventType { get; set; }
        
        public Lite<ExceptionEntity> Exception { get; set; }
    }
}
