
namespace Signum.Word;

[EntityKind(EntityKind.SystemString, EntityData.Master), TicksColumn(false)]
public class WordModelEntity : Entity
{
    [StringLengthValidator(Max = 200), UniqueIndex]
    public string ClassName { get; set; }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => ClassName);
}

