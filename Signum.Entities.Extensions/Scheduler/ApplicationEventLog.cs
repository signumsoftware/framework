using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signum.Entities.Scheduler
{
    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class ApplicationEventLogEntity : Entity
    {
        [SqlDbType(Size = 100), NotNullable]
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string MachineName { get; set; }

        public DateTime Date { get; set; }

        public TypeEvent GlobalEvent { get; set; }
    }

    public enum TypeEvent
    {
        Start,
        Stop
    }
}
