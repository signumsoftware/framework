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
    public string? Namespace { get; set; }

    [StringLengthValidator(Max = 200)]
    public string? Package { get; set; }

    [StringLengthValidator(Max = 200)]
    public string ClassName { get; set; }

    //A Part entity exists only as part of the one entity that owns it, so it is never a sensible answer in a type picker. Not maintained by TypeLogic, like Package.
    public bool? IsPart { get; set; }

    [AutoExpressionField]
    public string FullClassName => As.Expression(() => Namespace + "." + ClassName);

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => this.CleanName);
}

