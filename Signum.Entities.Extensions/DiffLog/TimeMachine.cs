using System.ComponentModel;

namespace Signum.Entities.DiffLog
{
    public enum TimeMachineMessage
    {
        TimeMachine,
        [Description("[Entity deleted]")]
        EntityDeleted,
    }
}
