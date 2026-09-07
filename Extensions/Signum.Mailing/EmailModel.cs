
namespace Signum.Mailing;

[EntityKind(EntityKind.SystemString, EntityData.Master), TicksColumn(false)]
public class EmailModelEntity : Entity
{
    [UniqueIndex]
    public string ClassName { get; set; }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => ClassName);
}
