using System.ComponentModel;

namespace Signum.Entities.DiffLog;

public enum TimeMachineMessage
{
    TimeMachine,
    [Description("[Entity deleted]")]
    EntityDeleted,
    CompareVersions,
    AllVersions,
    SelectedVersions,
    UIDifferences,
    DataDifferences,
    UISnapshot,
    DataSnapshot,
    ShowDiffs,
    YouCanNotSelectMoreThanTwoVersionToCompare,

    [Description("(between this time range)")]
    BetweenThisTimeRange, 

    [Description("This version was CREATED")]
    ThisVersionWasCreated,
    [Description("This version was DELETED")]
    ThisVersionWasDeleted,
    [Description("This version was CREATED and DELETED")]
    ThisVersionWasCreatedAndDeleted,
    [Description("This version DID NOT CHANGE")]
    ThisVersionDidNotChange
}

[AutoInit]
public static class TimeMachinePermission
{
    public static PermissionSymbol ShowTimeMachine;
}
