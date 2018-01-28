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
        [NotNullValidator]
        public Lite<UserEntity> User { get; set; }

        [StringLengthValidator(AllowNulls = false, Min = 36, Max = 36)]
        public string Ticket { get; set; }

        public DateTime ConnectionDate { get; set; }

                public string Device { get; set; }

        public string StringTicket()
        {
            return "{0}|{1}".FormatWith(User.Id, Ticket);
        }

        public static (PrimaryKey userId, string ticket) ParseTicket(string ticket)
        {
            Match m = Regex.Match(ticket, @"^(?<id>.*)\|(?<ticket>.*)$");
            if (!m.Success) throw new FormatException("The content of the ticket has an invalid format");
            return (userId: PrimaryKey.Parse(m.Groups["id"].Value, typeof(UserEntity)), ticket: m.Groups["ticket"].Value);
        }
    }
}
