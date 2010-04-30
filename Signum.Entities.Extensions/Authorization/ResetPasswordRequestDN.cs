using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;

namespace Signum.Entities.Authorization
{
    [Serializable]
    public class ResetPasswordRequestDN : Entity
    {
        string code;
        public string Code
        {
            get { return code; }
            set { Set(ref code, value, () => Code); }
        }

        string email;
        [NotNullValidator]
        public string Email
        {
            get { return email; }
            set { Set(ref email, value, () => Email); }
        }

        DateTime requestDate;
        public DateTime RequestDate
        {
            get { return requestDate; }
            set { Set(ref requestDate, value, () => RequestDate); }
        }
    }
}
