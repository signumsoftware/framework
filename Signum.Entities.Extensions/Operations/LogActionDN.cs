using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Authorization;
using Signum.Utilities;

namespace Signum.Entities.Operations
{
    [Serializable]
    public class LogActionDN : IdentifiableEntity
    {
        [ImplementedByAll, NotNullable]
        Lazy<IdentifiableEntity> entity;
        [NotNullValidator]
        public Lazy<IdentifiableEntity> Entity
        {
            get { return entity; }
            set { Set(ref entity, value, "Entity"); }
        }

        [ImplementedBy, NotNullable]
        ActionDN action;
        [NotNullValidator]
        public ActionDN Action
        {
            get { return action; }
            set { Set(ref action, value, "Action"); }
        }

        UserDN user;
        [NotNullValidator]
        public UserDN User
        {
            get { return user; }
            set { Set(ref user, value, "User"); }
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

        string exception;
        public string Exception
        {
            get { return exception; }
            set { Set(ref exception, value, "Exception"); }
        }
    }  
}
