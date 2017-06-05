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

        [NotNullable, SqlDbType(Size = 38)]
        [StringLengthValidator(AllowNulls = false, Min = 36, Max = 36)]
        public string Ticket { get; set; }

        public DateTime ConnectionDate { get; set; }

        [NotNullable, SqlDbType(Size = 100)]
        public string Device { get; set; }
        public string DeviceKey { get; set; }

        public static Func<UserTicketEntity, string> StringTicketFunc = ute => StringTicketDefauld(ute);

        public static string StringTicketDefauld(UserTicketEntity ute)
        {
            return "{0}|{1}".FormatWith(ute.User.Id, ute.Ticket);
        }

        public string StringTicket()
        {
            return StringTicketFunc(this);
        }


        public static Func<string, string,(PrimaryKey, string)> ParseTicket = (ticket, device) => ParseTicketDefauld(ticket, device);
        public static (PrimaryKey, string) ParseTicketDefauld(string ticket, string device)
        {
            Match m = Regex.Match(ticket, @"^(?<id>.*)\|(?<ticket>.*)$");
            if (!m.Success) throw new FormatException("The content of the ticket has an invalid format");
            return ( PrimaryKey.Parse(m.Groups["id"].Value, typeof(UserEntity)), m.Groups["ticket"].Value);
        }
    }
}
