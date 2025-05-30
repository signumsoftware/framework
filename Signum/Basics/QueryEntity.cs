namespace Signum.Basics;

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
[EntityKind(EntityKind.SystemString, EntityData.Master), TicksColumn(false)]
public class QueryEntity : Entity
{
    [UniqueIndex]
    [StringLengthValidator(Min = 3, Max = 100)]
    public string Key { get; set; }

    public static Func<QueryEntity, Implementations> GetEntityImplementations;

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => this.Key);
}
