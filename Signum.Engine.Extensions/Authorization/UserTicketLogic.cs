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
                AuthLogic.AssertIsStarted(sb);
                sb.Include<UserTicketDN>();

                dqm[typeof(UserTicketDN)] = (from ut in Database.Query<UserTicketDN>()
                                             select new
                                             {
                                                 Entity = ut.ToLite(),
                                                 ut.IdOrNull,
                                                 ut.User,
                                                 ut.Ticket,
                                                 ut.ConnectionDate,
                                                 ut.Device,
                                             }).ToDynamic();

                sb.Schema.EntityEvents<UserDN>().Saved += new EntityEventHandler<UserDN>(UserTicketLogic_Saved);
            }
        }

        static void UserTicketLogic_Saved(UserDN ident, bool isRoot)
        {
            CleanExpiredTickets(ident);
        }

        static Expression<Func<UserDN, IQueryable<UserTicketDN>>> TicketsExpression = 
            u => Database.Query<UserTicketDN>().Where(ut=>ut.User == u.ToLite()) ; 
        public static IQueryable<UserTicketDN> Tickets(this UserDN u)
        {
            return TicketsExpression.Invoke(u);
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
                    ConnectionDate = DateTime.Now,
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

                UserDN result = Database.Retrieve<UserDN>(pair.First);
                CleanExpiredTickets(result); 
                
                UserTicketDN userTicket = result.Tickets().SingleOrDefault(t => t.Ticket == pair.Second);
                if (userTicket == null)
                    throw new UnauthorizedAccessException("User attempted to log in with an invalid ticket");

                userTicket.Ticket = Guid.NewGuid().ToString();
                userTicket.Device = device;
                userTicket.ConnectionDate = DateTime.Now;
                userTicket.Save();

                ticket = userTicket.StringTicket(); 

                return tr.Commit(result);
            }
        }

        public static int RemoveTickets(UserDN user)
        {
            return user.Tickets().UnsafeDelete(); 
        }

        public static int CleanExpiredTickets(UserDN user)
        {
            DateTime min = DateTime.Now.Subtract(ExpirationInterval);
            int result = user.Tickets().Where(d => d.ConnectionDate < min).UnsafeDelete();

            List<Lite<UserTicketDN>> tooMuch = user.Tickets().OrderByDescending(t => t.ConnectionDate).Select(t => t.ToLite()).ToList().Skip(MaxTicketsPerUser).ToList();

            if (tooMuch.Empty()) return result;

            Database.Delete<UserTicketDN>(tooMuch);

            return result + tooMuch.Count; 
        }

        public static int CleanAllExpiredTickets()
        {
            DateTime min = DateTime.Now.Subtract(ExpirationInterval);
            return Database.Query<UserTicketDN>().Where(a => a.ConnectionDate < min).UnsafeDelete();  
        }
    }
}
