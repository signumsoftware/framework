using Signum.Mailing;

namespace Signum.Mailing.Package;


public class EmailMessagePackageMixin : MixinEntity
{
    EmailMessagePackageMixin(ModifiableEntity mainEntity, MixinEntity? next)
        : base(mainEntity, next)
    {
    }

    public Lite<EmailPackageEntity>? Package { get; set; }
}
