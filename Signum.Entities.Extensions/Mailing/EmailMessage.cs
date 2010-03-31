using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Authorization;
using Signum.Entities.Operations;

namespace Signum.Entities.Mailing
{
    [Serializable]
    public class EmailMessageDN : Entity
    {
        Lite<UserDN> user;
        [NotNullValidator]
        public Lite<UserDN> User
        {
            get { return user; }
            set { Set(ref user, value, () => User); }
        }

        DateTime sent;
        public DateTime Sent
        {
            get { return sent; }
            set { SetToStr(ref sent, value, () => Sent); }
        }

        DateTime? received;
        public DateTime? Received
        {
            get { return received; }
            set { Set(ref received, value, () => Received); }
        }

        [NotNullable, SqlDbType(Size = 100)]
        string subject;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Subject
        {
            get { return subject; }
            set { Set(ref subject, value, () => Subject); }
        }

        [NotNullable, SqlDbType(Size = int.MaxValue)]
        string body;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = int.MaxValue)]
        public string Body
        {
            get { return body; }
            set { Set(ref body, value, () => Body); }
        }

        string exception;
        public string Exception
        {
            get { return exception; }
            set { Set(ref exception, value, () => Exception); }
        }        
    }
}
