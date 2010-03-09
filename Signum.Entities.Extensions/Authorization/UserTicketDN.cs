using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Principal;
using Signum.Utilities;
using System.Threading;
using System.Security.Cryptography;
using System.ComponentModel;
using System.Reflection;
using Signum.Entities.Extensions.Properties;
using Signum.Utilities.DataStructures;
using System.Text.RegularExpressions;

namespace Signum.Entities.Authorization
{
    [Serializable]
    public class UserTicketDN : Entity
    {
        Lite<UserDN> user;
        [NotNullValidator]
        public Lite<UserDN> User
        {
            get { return user; }
            set { Set(ref user, value, () => User); }
        }

        [NotNullable, SqlDbType(Size = 38)]
        string ticket;
        [StringLengthValidator(AllowNulls = false, Min = 38, Max = 38)]
        public string Ticket
        {
            get { return ticket; }
            set { Set(ref ticket, value, () => Ticket); }
        }

        DateTime connectionDate;
        public DateTime ConnectionDate
        {
            get { return connectionDate; }
            set { Set(ref connectionDate, value, () => ConnectionDate); }
        }

        [NotNullable, SqlDbType(Size = 100)]
        string device;
        public string Device
        {
            get { return device; }
            set { Set(ref device, value, () => Device); }
        }

        public string StringTicket()
        {
            return "{0};{1}".Formato(user.Id, ticket);
        }

        public static Tuple<int, string> ParseTicket(string ticket)
        {
            Match m = Regex.Match(ticket, @"^(?<id>\d+);(?<ticket>.*)$");
            return new Tuple<int, string>(int.Parse(m.Groups["id"].Value), m.Groups["ticket"].Value);
        }
    }
}
