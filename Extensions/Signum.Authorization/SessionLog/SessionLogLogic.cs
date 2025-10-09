using Signum.Authorization.Rules;

namespace Signum.Authorization.SessionLog;

public static class SessionLogLogic
{
    public static bool IsStarted { get; private set; }

    public static void Start(SchemaBuilder sb)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        AuthLogic.AssertStarted(sb);

        sb.Include<SessionLogEntity>()
            .WithQuery(() => sl => new
            {
                Entity = sl,
                sl.Id,
                sl.User,
                sl.SessionStart,
                sl.SessionEnd,
                sl.SessionTimeOut
            });

        PermissionLogic.RegisterPermissions(SessionLogPermission.TrackSession);

        ExceptionLogic.DeleteLogs += ExceptionLogic_DeleteLogs;

        IsStarted = true;
    }

    public static void ExceptionLogic_DeleteLogs(DeleteLogParametersEmbedded parameters, StringBuilder sb, CancellationToken token)
    {
        var dateLimit = parameters.GetDateLimitDelete(typeof(SessionLogEntity).ToTypeEntity());
        if (dateLimit != null)
            Database.Query<SessionLogEntity>().Where(a => a.SessionStart < dateLimit.Value).UnsafeDeleteChunksLog(parameters, sb, token);
    }

    static bool RoleTracked(Lite<RoleEntity> role)
    {
        return PermissionAuthLogic.IsAuthorized(SessionLogPermission.TrackSession, role);
    }

    public static void SessionStart(string userHostAddress, string? userAgent)
    {
        if (SessionLogLogic.RoleTracked(RoleEntity.Current))
        {
            using (AuthLogic.Disable())
            {
                new SessionLogEntity
                {
                    User = UserEntity.Current,
                    SessionStart = Clock.Now.TruncSeconds(),
                    UserHostAddress = userHostAddress,
                    UserAgent = userAgent
                }.Save();
            }
        }
    }

    public static void SessionEnd(UserEntity user, TimeSpan? timeOut)
    {
        if (user == null || !RoleTracked(user.Role))
            return;

        using (AuthLogic.Disable())
        {
            var sessionEnd = timeOut.HasValue ? Clock.Now.Subtract(timeOut.Value).TruncSeconds() : Clock.Now.TruncSeconds();

            var rows = Database.Query<SessionLogEntity>()
                .Where(sl => sl.User.Is(user))
                .OrderByDescending(sl => sl.SessionStart)
                .Take(1)
                .Where(sl => sl.SessionEnd == null)
                .UnsafeUpdate()
                .Set(a => a.SessionEnd, a => sessionEnd)
                .Set(a => a.SessionTimeOut, a => timeOut.HasValue)
                .Execute();
        }
    }
}
