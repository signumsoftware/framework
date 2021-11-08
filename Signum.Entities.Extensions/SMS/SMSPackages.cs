using Signum.Entities.Processes;

namespace Signum.Entities.SMS
{
    [EntityKind(EntityKind.System, EntityData.Transactional)]
    public class SMSSendPackageEntity : SMSPackageEntity
    {

    }

    [EntityKind(EntityKind.System, EntityData.Transactional)]
    public class SMSUpdatePackageEntity : SMSPackageEntity
    {

    }

    public abstract class SMSPackageEntity : Entity, IProcessDataEntity
    {
        public SMSPackageEntity()
        {
            this.Name = GetType().NiceName() + ": " + TimeZoneManager.Now.ToString();
        }

        [StringLengthValidator(Max = 200)]
        public string? Name { get; set; }

        [AutoExpressionField]
        public override string ToString() => As.Expression(() => Name!);
    }
}
