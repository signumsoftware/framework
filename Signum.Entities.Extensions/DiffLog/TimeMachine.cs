using Signum.Entities.Authorization;
using System.ComponentModel;

namespace Signum.Entities.DiffLog
{
    public enum TimeMachineMessage
    {
        TimeMachine,
        [Description("[Entity deleted]")]
        EntityDeleted,
    }

    [AutoInit]
    public static class TimeMachinePermission
    {
        public static PermissionSymbol ShowTimeMachine;
    }
}
