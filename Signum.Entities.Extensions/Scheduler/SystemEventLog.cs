using Signum.Entities.Basics;
using System;

namespace Signum.Entities.Scheduler
{
    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class SystemEventLogEntity : Entity
    {
        [StringLengthValidator(Min = 3, Max = 100)]
        public string MachineName { get; set; }

        public DateTime Date { get; set; }

        public Lite<IUserEntity>? User { get; set; }

        [StringLengthValidator(Min = 3, Max = 100)]
        public string EventType { get; set; }

        public Lite<ExceptionEntity>? Exception { get; set; }
    }
}
