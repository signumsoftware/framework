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

        public static bool IsStarted { get; private set; }

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                IsStarted = true;

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

                dqm.RegisterExpression((UserDN u) => u.UserTickets(), () => typeof(UserTicketDN).NicePluralName());
            }
        }

        static Expression<Func<UserDN, IQueryable<UserTicketDN>>> UserTicketsExpression =
            u => Database.Query<UserTicketDN>().Where(ut => ut.User == u.ToLite());
        public static IQueryable<UserTicketDN> UserTickets(this UserDN u)
        {
            return UserTicketsExpression.Evaluate(u);
        }

        public static string NewTicket(string device)
        {
            using (AuthLogic.Disable())
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
            using (AuthLogic.Disable())
            using (Transaction tr = new Transaction())
            {
                Tuple<PrimaryKey, string> pair = UserTicketDN.ParseTicket(ticket);

                UserDN user = Database.Retrieve<UserDN>(pair.Item1);
                CleanExpiredTickets(user);

                UserTicketDN userTicket = user.UserTickets().SingleOrDefaultEx(t => t.Ticket == pair.Item2);
                if (userTicket == null)
                {
                    throw new UnauthorizedAccessException("User attempted to log-in with an invalid ticket");
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


        static int CleanExpiredTickets(UserDN user)
        {
            DateTime min = TimeZoneManager.Now.Subtract(ExpirationInterval);

            int expired = user.UserTickets().Where(d => d.ConnectionDate < min).UnsafeDelete();

            int tooMuch = user.UserTickets().OrderByDescending(t => t.ConnectionDate).Skip(MaxTicketsPerUser).UnsafeDelete();

            return expired + tooMuch;
        }
    }
}
