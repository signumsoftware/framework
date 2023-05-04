
namespace Signum.Processes;

[EntityKind(EntityKind.Part, EntityData.Transactional)]
public class PackageEntity : Entity, IProcessDataEntity
{
    [StringLengthValidator(Max = 200)]
    public string? Name { get; set; }

    [DbType(Size = int.MaxValue)]
    public byte[]? OperationArguments { get; set; }

    public override string ToString()
    {
        return "Package {0}".FormatWith(Name);
    }
}

[EntityKind(EntityKind.System, EntityData.Transactional)]
public class PackageOperationEntity : PackageEntity
{
    public OperationSymbol Operation { get; set; }

    public override string ToString()
    {
        return "Package {0} {1}".FormatWith(Operation, Name);
    }
}

[AutoInit]
public static class PackageOperationProcess
{
    public static ProcessAlgorithmSymbol PackageOperation;
}


[EntityKind(EntityKind.System, EntityData.Transactional), TicksColumn(false)]
public class PackageLineEntity : Entity
{   
    public Lite<PackageEntity> Package { get; set; }

    [ImplementedByAll]
    public Entity Target { get; set; }

    [ImplementedByAll]
    public Lite<Entity>? Result { get; set; } //ConstructFrom only!

    public DateTime? FinishTime { get; set; }

    static Expression<Func<PackageLineEntity, string>> ToStringExpression = pel => "PackageLine (" + pel.Id + ")";
    [ExpressionField("ToStringExpression")]
    public override string ToString() => "PackageLine (" + (this.IdOrNull == null ? "New" : this.Id.ToString()) + ")";
}

public enum PackageQuery
{
    PackageLineLastProcess,
    PackageLastProcess,
    PackageOperationLastProcess,
}
