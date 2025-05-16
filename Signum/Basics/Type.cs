namespace Signum.Basics;

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
[EntityKind(EntityKind.SystemString, EntityData.Master), TicksColumn(false)]
public class TypeEntity : Entity
{
    [StringLengthValidator(Max = 200), UniqueIndex]
    public string TableName { get; set; }

    [StringLengthValidator(Max = 200), UniqueIndex]
    public string CleanName { get; set; }

    [StringLengthValidator(Max = 200)]
    public string Namespace { get; set; }

    [StringLengthValidator(Max = 200)]
    public string ClassName { get; set; }

    [AutoExpressionField]
    public string FullClassName => As.Expression(() => Namespace + "." + ClassName);

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => this.CleanName);
}

