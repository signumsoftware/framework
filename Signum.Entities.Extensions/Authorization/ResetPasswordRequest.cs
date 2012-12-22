using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;

namespace Signum.Entities.Authorization
{
    [Serializable, EntityType(EntityType.System)]
    public class ResetPasswordRequestDN : Entity
    {
        string code;
        public string Code
        {
            get { return code; }
            set { Set(ref code, value, () => Code); }
        }

        UserDN user;
        [NotNullValidator]
        public UserDN User
        {
            get { return user; }
            set { Set(ref user, value, () => User); }
        }

        DateTime requestDate;
        public DateTime RequestDate
        {
            get { return requestDate; }
            set { Set(ref requestDate, value, () => RequestDate); }
        }
    }
}
