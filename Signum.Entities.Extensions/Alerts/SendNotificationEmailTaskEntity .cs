using Signum.Entities.Mailing;
using Signum.Entities.Scheduler;
using System.Threading.Tasks;

namespace Signum.Entities.Alerts
{
    [EntityKind(EntityKind.Shared, EntityData.Master)]
    public class SendNotificationEmailTaskEntity : Entity, ITaskEntity
    {
        [Unit("mins"), NumberIsValidator(ComparisonType.GreaterThanOrEqualTo, 0)]
        public int SendNotificationsOlderThan { get; set; }

        public SendAlertTypeBehavior SendBehavior { get; set; }

        [PreserveOrder, NoRepeatValidator]
        public MList<AlertTypeSymbol> AlertTypes { get; set; } = new MList<AlertTypeSymbol>();
    }

    public enum SendAlertTypeBehavior
    {
        All,
        Include,
        Exclude,
    }


    [AutoInit]
    public static class SendNotificationEmailTaskOperation
    {
        public static ExecuteSymbol<SendNotificationEmailTaskEntity> Save;
    }
}
