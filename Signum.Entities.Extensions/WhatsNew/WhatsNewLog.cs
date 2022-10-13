using Signum.Entities.Authorization;

namespace Signum.Entities.WhatsNew;


[EntityKind(EntityKind.System, EntityData.Transactional)]
public class WhatsNewLogEntity : Entity
{
    public Lite<WhatsNewEntity> WhatsNew { get; set; }

    public Lite<UserEntity> User { get; set; }

    public DateTime ReadOn { get; set; }


    [AutoExpressionField]
    public override string ToString() => As.Expression(() => $"{WhatsNew}: {User}");
}

[AutoInit]
public static class WhatsNewLogOperation
{
    public static readonly DeleteSymbol<WhatsNewLogEntity> Delete;
}
