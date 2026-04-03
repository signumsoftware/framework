using Signum.API;
using Signum.Authorization;
using Signum.Authorization.Rules;

namespace Signum.ConcurrentUser;
public static class ConcurrentUserLogic
{
    public static Func<Type, bool> WatchSaveFor = null!; 

    public static void Start(SchemaBuilder sb, Func<Type, bool>? activatedFor = null)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        //Should be in sync with ConcurrentUserCLlient onlyFor!!
        WatchSaveFor = activatedFor ?? (t => EntityKindCache.GetEntityKind(t) is not (EntityKind.System or EntityKind.SystemString)); 

        sb.Include<ConcurrentUserEntity>()
            .WithIndex(a => new { a.SignalRConnectionID })
            .WithUniqueIndex(a => new { a.SignalRConnectionID, a.User, a.StartTime, a.TargetEntity })
            .WithDelete(ConcurrentUserOperation.Delete)
            .WithQuery(() => e => new
            {
                Entity = e,
                e.Id,
                e.TargetEntity,
                e.IsModified,
                e.User,
                e.StartTime,
                e.SignalRConnectionID,
            });

        sb.Schema.EntityEvents<TypeEntity>().PreDeleteSqlSync += type => Administrator.UnsafeDeletePreCommand(Database.Query<ConcurrentUserEntity>().Where(a => a.TargetEntity.EntityType.ToTypeEntity().Is(type)));

        if (sb.WebServerBuilder != null)
            ConcurrentUserServer.Start(sb.WebServerBuilder);
    }
}
