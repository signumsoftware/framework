namespace Signum.Authorization.AzureAD.ADGroup;

[EntityKind(EntityKind.String, EntityData.Master), PrimaryKey(typeof(Guid))]
public class ADGroupEntity : Entity
{
    [UniqueIndex]
    [StringLengthValidator(Max = 100)]
    public string DisplayName { get; set; }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => DisplayName);
}


[AutoInit]
public static class ADGroupOperation
{
    public static readonly ExecuteSymbol<ADGroupEntity> Save;
    public static readonly DeleteSymbol<ADGroupEntity> Delete;
}


