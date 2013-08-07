using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Maps;
using Signum.Engine.DynamicQuery;
using System.Reflection;
using Signum.Entities.Authorization;
using System.Linq.Expressions;
using Signum.Utilities.DataStructures;
using Signum.Entities;
using Signum.Utilities;
using System.IO;

namespace Signum.Engine.Authorization
{
    public static class UserTicketLogic
    {
        public static TimeSpan ExpirationInterval = TimeSpan.FromDays(60);
        public static int MaxTicketsPerUser = 4;

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                AuthLogic.AssertStarted(sb);
                sb.Include<UserTicketDN>();

                dqm.RegisterQuery(typeof(UserTicketDN), () =>
                    from ut in Database.Query<UserTicketDN>()
                    select new
                    {
                        Entity = ut,
                        ut.Id,
                        ut.User,
                        ut.Ticket,
                        ut.ConnectionDate,
                        ut.Device,
                    });

                dqm.RegisterExpression((UserDN u) => u.Tickets());
            }
        }

        static Expression<Func<UserDN, IQueryable<UserTicketDN>>> TicketsExpression =
            u => Database.Query<UserTicketDN>().Where(ut => ut.User == u.ToLite());
        public static IQueryable<UserTicketDN> Tickets(this UserDN u)
        {
            return TicketsExpression.Evaluate(u);
        }

        public static string NewTicket(string device)
        {
            using (Transaction tr = new Transaction())
            {
                CleanExpiredTickets(UserDN.Current);

                UserTicketDN result = new UserTicketDN
                {
                    User = UserDN.Current.ToLite(),
                    Device = device,
                    ConnectionDate = TimeZoneManager.Now,
                    Ticket = Guid.NewGuid().ToString(),
                };

                result.Save();

                return tr.Commit(result.StringTicket());
            }

        }

        public static UserDN UpdateTicket(string device, ref string ticket)
        {
            using (Transaction tr = new Transaction())
            {
                Tuple<int, string> pair = UserTicketDN.ParseTicket(ticket);

                UserDN user = Database.Retrieve<UserDN>(pair.Item1);
                CleanExpiredTickets(user);


                UserTicketDN userTicket = user.Tickets().SingleOrDefaultEx(t => t.Ticket == pair.Item2);
                if (userTicket == null)
                {
                    throw new UnauthorizedAccessException("User attempted to log in with an invalid ticket");
                }

                UserTicketDN result = new UserTicketDN
                {
                    User = user.ToLite(),
                    Device = device,
                    ConnectionDate = TimeZoneManager.Now,
                    Ticket = Guid.NewGuid().ToString(),
                }.Save();

                ticket = result.StringTicket();

                return tr.Commit(user);
            }
        }

        public static int RemoveTickets(UserDN user)
        {
            return user.Tickets().UnsafeDelete();
        }

        public static int CleanExpiredTickets(UserDN user)
        {
            DateTime min = TimeZoneManager.Now.Subtract(ExpirationInterval);

            int expired = user.Tickets().Where(d => d.ConnectionDate < min).UnsafeDelete();

            int tooMuch = user.Tickets().OrderByDescending(t => t.ConnectionDate).Skip(MaxTicketsPerUser).UnsafeDelete();

            return expired + tooMuch;
        }

        public static int CleanAllExpiredTickets()
        {
            DateTime min = TimeZoneManager.Now.Subtract(ExpirationInterval);
            return Database.Query<UserTicketDN>().Where(a => a.ConnectionDate < min).UnsafeDelete();
        }
    }
}
