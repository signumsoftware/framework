using Signum.Engine.Maps;

namespace Signum.Basics;

public static class ChangeLogLogic
{
    public static void Start(SchemaBuilder sb)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

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

    public static DateTime? GetLastDate()
    {
        using (ExecutionMode.Global())
        {
            var lastLog = Database.Query<ChangeLogViewLogEntity>().SingleOrDefault(cl => cl.User.Is(UserHolder.Current.User));
            return lastLog?.LastDate;
       
        }
    }

    
    public static void UpdateLastDate()
    {
        using (ExecutionMode.Global())
        {
            var lastLog = Database.Query<ChangeLogViewLogEntity>().SingleOrDefault(cl => cl.User.Is(UserHolder.Current.User)) ?? 
            new ChangeLogViewLogEntity { User = UserHolder.Current.User };
            lastLog.LastDate = Clock.Now;
            lastLog.Save();
        }
    }
}
