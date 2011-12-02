using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Authorization;
using Signum.Utilities;

namespace Signum.Entities.Operations
{
    [Serializable]
    public class LogOperationDN : IdentifiableEntity
    {
        [ImplementedByAll]
        Lite<IIdentifiable> target;
        [NotNullValidator]
        public Lite<IIdentifiable> Target
        {
            get { return target; }
            set { Set(ref target, value, () => Target); }
        }

        OperationDN operation;
        [NotNullValidator]
        public OperationDN Operation
        {
            get { return operation; }
            set { SetToStr(ref operation, value, () => Operation); }
        }

        Lite<UserDN> user;
        [NotNullValidator]
        public Lite<UserDN> User
        {
            get { return user; }
            set { SetToStr(ref user, value, () => User); }
        }

        DateTime start;
        public DateTime Start
        {
            get { return start; }
            set { SetToStr(ref start, value, () => Start); }
        }

        DateTime? end;
        public DateTime? End
        {
            get { return end; }
            set { Set(ref end, value, () => End); }
        }

        [SqlDbType(Size=int.MaxValue)]
        string exception;
        public string Exception
        {
            get { return exception; }
            set { Set(ref exception, value, () => Exception); }
        }

        public override string ToString()
        {
            return "{0} {1} {2:d}".Formato(operation, user, start);
        }
    }
}
