using Signum.Processes;

namespace Signum.Printing;

[EntityKind(EntityKind.System, EntityData.Transactional)]
public class PrintPackageEntity : Entity, IProcessDataEntity
{
    [StringLengthValidator(Max = 200)]
    public string? Name { get; set; }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => Name ?? "- No Name -");
}
