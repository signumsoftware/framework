using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signum.Entities.Scheduler
{
    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class ApplicationEventLogDN : Entity
    {
        [SqlDbType(Size = 100), NotNullable]
        string machineName;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string MachineName
        {
            get { return machineName; }
            set { Set(ref machineName, value); }
        }

        DateTime date;
        public DateTime Date
        {
            get { return date; }
            set { Set(ref date, value); }
        }

        TypeEvent globalEvent;
        public TypeEvent GlobalEvent
        {
            get { return globalEvent; }
            set { Set(ref globalEvent, value); }
        }

    }

    public enum TypeEvent
    {
        Start,
        Stop
    }
}
