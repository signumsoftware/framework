using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Authorization;
using Signum.Utilities;

namespace Signum.Entities.Operations
{
    [Serializable, PluralLocDescription()]
    public class LogOperationDN : IdentifiableEntity
    {
        [ImplementedByAll]
        Lite<IIdentifiable> target;
        [NotNullValidator, LocDescription]
        public Lite<IIdentifiable> Target
        {
            get { return target; }
            set { Set(ref target, value, () => Target); }
        }

        OperationDN operation;
        [NotNullValidator, LocDescription]
        public OperationDN Operation
        {
            get { return operation; }
            set { Set(ref operation, value, () => Operation); }
        }

        UserDN user;
        [NotNullValidator, LocDescription]
        public UserDN User
        {
            get { return user; }
            set { Set(ref user, value, () => User); }
        }

        DateTime start;
        [LocDescription]
        public DateTime Start
        {
            get { return start; }
            set { Set(ref start, value, () => Start); }
        }

        DateTime? end;
        [LocDescription]
        public DateTime? End
        {
            get { return end; }
            set { Set(ref end, value, () => End); }
        }

        string exception;
        [LocDescription]
        public string Exception
        {
            get { return exception; }
            set { Set(ref exception, value, () => Exception); }
        }
    }
}
