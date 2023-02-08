using Signum.Entities.Authorization;
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
    YouCanNotSelectMoreThanTwoVersionToCompare
}

[AutoInit]
public static class TimeMachinePermission
{
    public static PermissionSymbol ShowTimeMachine;
}
