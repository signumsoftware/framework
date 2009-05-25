using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Authorization;
using Signum.Utilities;

namespace Signum.Entities.Operations
{
    [Serializable]
    public class LogStateDN : IdentifiableEntity
    {
        [ImplementedByAll, NotNullable]
        Lazy<IdentifiableEntity> entity;
        [NotNullValidator]
        public Lazy<IdentifiableEntity> Entity
        {
            get { return entity; }
            set { Set(ref entity, value, "Entity"); }
        }

        [ImplementedByAll, NotNullable]
        IdentifiableEntity state;
        [NotNullValidator]
        public IdentifiableEntity State
        {
            get { return state; }
            set { Set(ref state, value, "State"); }
        }

        DateTime start;
        public DateTime Start
        {
            get { return start; }
            set { Set(ref start, value, "Start"); }
        }

        DateTime? end;
        public DateTime? End
        {
            get { return end; }
            set { Set(ref end, value, "End"); }
        }
    }

  
}
