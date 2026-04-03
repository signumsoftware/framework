namespace Signum.Authorization.UserTicket;

public static class UserTicketLogic
{
    public static TimeSpan ExpirationInterval = TimeSpan.FromDays(60);
    public static int MaxTicketsPerUser = 4;

    public static bool IsStarted { get; private set; }

    public static void Start(SchemaBuilder sb)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        IsStarted = true;

        AuthLogic.AssertStarted(sb);
        sb.Include<UserTicketEntity>()
            .WithQuery(() => ut => new
            {
                Entity = ut,
                ut.Id,
                ut.User,
                ut.Ticket,
                ut.ConnectionDate,
                ut.Device,
            });

        QueryLogic.Expressions.Register((UserEntity u) => u.UserTickets(), () => typeof(UserTicketEntity).NicePluralName());

        sb.Schema.EntityEvents<UserEntity>().Saving += UserTicketLogic_Saving;
    }

    static void UserTicketLogic_Saving(UserEntity user)
    {
        if (!user.IsNew && user.IsGraphModified)
        {

            if (!user.InDB(u => u.PasswordHash).EmptyIfNull().SequenceEqual(user.PasswordHash.EmptyIfNull()))
            {
                using (AuthLogic.Disable())
                    user.UserTickets().UnsafeDelete();
            }
        }
    }

    [AutoExpressionField]
    public static IQueryable<UserTicketEntity> UserTickets(this UserEntity u) =>
        As.Expression(() => Database.Query<UserTicketEntity>().Where(ut => ut.User.Is(u.ToLite())));

    public static string NewTicket(string device)
    {
        using (AuthLogic.Disable())
        using (var tr = new Transaction())
        {
            var user = UserEntity.Current.Retrieve();

            CleanExpiredTickets(user);

            AuthLogic.CheckUserActive(user);

            UserTicketEntity result = new UserTicketEntity
            {
                User = user.ToLite(),
                Device = device,
                ConnectionDate = Clock.Now,
                Ticket = Guid.NewGuid().ToString(),
            };

            result.Save();

            return tr.Commit(result.StringTicket());
        }
    }

    public static UserEntity UpdateTicket(string device, ref string ticket)
    {
        using (AuthLogic.Disable())
        using (var tr = new Transaction())
        {
            var pair = UserTicketEntity.ParseTicket(ticket);

            UserEntity user = Database.Retrieve<UserEntity>(pair.userId);

            CleanExpiredTickets(user);

            AuthLogic.CheckUserActive(user);

            UserTicketEntity? userTicket = user.UserTickets().SingleOrDefaultEx(t => t.Ticket == pair.ticket);
            if (userTicket == null)
            {
                throw new UnauthorizedAccessException("User attempted to log-in with an invalid ticket");
            }

            UserTicketEntity result = new UserTicketEntity
            {
                User = user.ToLite(),
                Device = device,
                ConnectionDate = Clock.Now,
                Ticket = Guid.NewGuid().ToString(),
            }.Save();

            ticket = result.StringTicket();

            return tr.Commit(user);
        }
    }

    static int CleanExpiredTickets(UserEntity user)
    {
        DateTime min = Clock.Now.Subtract(ExpirationInterval);

        var rt = RemoveTickets(user);

        if (rt != null)
            return rt.Value;

        int expired = user.UserTickets().Where(d => d.ConnectionDate < min).UnsafeDelete();

        int tooMuch = user.UserTickets().OrderByDescending(t => t.ConnectionDate).Skip(MaxTicketsPerUser).UnsafeDelete();

        return expired + tooMuch;
    }

    public static int? RemoveTickets(UserEntity user)
    {
        if (user.State != UserState.Active)
            return user.UserTickets().UnsafeDelete();

        return null;
    }
}
