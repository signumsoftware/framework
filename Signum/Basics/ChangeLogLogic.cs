using Signum.Engine.Maps;

namespace Signum.Basics;

public static class ChangeLogLogic
{
    public static void Start(SchemaBuilder sb)
    {
        if (sb.NotDefined(MethodBase.GetCurrentMethod()))
        {
            sb.Include<ChangeLogViewLogEntity>()
                .WithDelete(ChangeLogViewLogOperation.Delete)
                .WithQuery(() => e => new
                {
                    Entity = e,
                    e.Id,
                    e.User,
                    e.LastDate
                });
        }
    }

    public static DateTime? GetLastDateAndUpdate()
    {
        using (ExecutionMode.Global())
        {
            var lastLog = Database.Query<ChangeLogViewLogEntity>().Where(cl => cl.User.Is(UserHolder.Current.User)).SingleOrDefault();
            var result = lastLog?.LastDate;
            lastLog ??= new ChangeLogViewLogEntity { User = UserHolder.Current.User, LastDate = Clock.Now };
            lastLog.LastDate = Clock.Now;
            lastLog.Save();
            return result;
        }
    }
}
