using Signum.Mailing;
using Signum.Processes;

namespace Signum.Mailing.Package;

[EntityKind(EntityKind.System, EntityData.Transactional), TicksColumn(false)]
public class EmailPackageEntity : Entity, IProcessDataEntity
{
    [StringLengthValidator(Max = 200)]
    public string? Name { get; set; }

    public override string ToString()
    {
        return "EmailPackage {0}".FormatWith(Name);
    }
}

[AutoInit]
public static class EmailMessagePackageOperation
{
    public static ConstructSymbol<ProcessEntity>.FromMany<EmailMessageEntity> ReSendEmails;
}

