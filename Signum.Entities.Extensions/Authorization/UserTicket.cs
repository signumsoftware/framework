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
using Signum.Utilities.DataStructures;
using System.Text.RegularExpressions;

namespace Signum.Entities.Authorization
{
    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional), TicksColumn(false)]
    public class UserTicketEntity : Entity
    {
        Lite<UserEntity> user;
        [NotNullValidator]
        public Lite<UserEntity> User
        {
            get { return user; }
            set { Set(ref user, value); }
        }

        [NotNullable, SqlDbType(Size = 38)]
        string ticket;
        [StringLengthValidator(AllowNulls = false, Min = 36, Max = 36)]
        public string Ticket
        {
            get { return ticket; }
            set { Set(ref ticket, value); }
        }

        DateTime connectionDate;
        public DateTime ConnectionDate
        {
            get { return connectionDate; }
            set { Set(ref connectionDate, value); }
        }

        [NotNullable, SqlDbType(Size = 100)]
        string device;
        public string Device
        {
            get { return device; }
            set { Set(ref device, value); }
        }

        public string StringTicket()
        {
            return "{0}|{1}".FormatWith(user.Id, ticket);
        }

        public static Tuple<PrimaryKey, string> ParseTicket(string ticket)
        {
            Match m = Regex.Match(ticket, @"^(?<id>.*)\|(?<ticket>.*)$");
            if (!m.Success) throw new FormatException("The content of the ticket has an invalid format");
            return new Tuple<PrimaryKey, string>(PrimaryKey.Parse(m.Groups["id"].Value, typeof(UserEntity)), m.Groups["ticket"].Value);
        }
    }
}
