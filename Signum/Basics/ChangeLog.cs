using System.ComponentModel;

namespace Signum.Basics;

[EntityKind(EntityKind.System, EntityData.Transactional)]
public class ChangeLogViewLogEntity : Entity
{
    [UniqueIndex]
    public Lite<IUserEntity> User { get; set; }
    
    public DateTime LastDate { get; set; }
}

[AutoInit]
public static class ChangeLogViewLogOperation
{
    public static readonly DeleteSymbol<ChangeLogViewLogEntity> Delete;
}

public enum ChangeLogMessage
{
    [Description("There is not any new changes from {0}")]
    ThereIsNotAnyNewChangesFrom0,
    [Description("See more…")]
    SeeMore,
}
