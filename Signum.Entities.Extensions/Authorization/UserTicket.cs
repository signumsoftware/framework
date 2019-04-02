using System;
using Signum.Utilities;
using System.Text.RegularExpressions;

namespace Signum.Entities.Authorization
{
    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional), TicksColumn(false)]
    public class UserTicketEntity : Entity
    {   
        public Lite<UserEntity> User { get; set; }

        [StringLengthValidator(Min = 36, Max = 36)]
        public string Ticket { get; set; }

        public DateTime ConnectionDate { get; set; }

        [StringLengthValidator(Max = 200)]
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
