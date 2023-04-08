using Signum.Entities.Mailing;

namespace Signum.MailPackage;


public class EmailMessagePackageMixin : MixinEntity
{
    EmailMessagePackageMixin(ModifiableEntity mainEntity, MixinEntity? next)
        : base(mainEntity, next)
    {
    }

    public Lite<EmailPackageEntity>? Package { get; set; }
}
