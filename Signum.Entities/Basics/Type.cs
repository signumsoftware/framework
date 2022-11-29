
namespace Signum.Entities.Basics;

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

    public string ToStringOriginal() => As.Expression(() => this.CleanName);

    static Expression<Func<TypeEntity, string>> ToStringCorrect;
    static void ToStringCorrectInit()
    {
        ToStringCorrect = t => t.CleanName;
    }

    public bool IsType(Type type)
    {
        if (type == null)
            throw new ArgumentException("type");

        return ClassName == type.Name && Namespace == type.Namespace;
    }

    public static Func<Type, TypeEntity> TypeToEntityFunc = t => { throw new InvalidOperationException("TypeEntity.ToTypeEntityFunc is not set"); };
    public static Func<TypeEntity, Type> EntityToTypeFunc = t => { throw new InvalidOperationException("TypeEntity.ToTypeFunc is not set"); };
    public static Func<Lite<TypeEntity>, Type> LiteToTypeFunc = t => { throw new InvalidOperationException("TypeEntity.ToTypeLiteFunc is not set"); };
    public static Func<string, Type?> TryGetType = s => { throw new InvalidOperationException("TypeEntity.TryGetType is not set"); };
    public static Func<Type, string> GetCleanName = s => { throw new InvalidOperationException("TypeEntity.GetCleanName is not set"); };

    public static bool AlreadySet { get; private set; }
    public static void SetTypeNameCallbacks(Func<Type, string> getCleanName, Func<string, Type?> tryGetType)
    {
        TypeEntity.GetCleanName = getCleanName;
        TypeEntity.TryGetType = tryGetType;

        AlreadySet = true;
    }

    public static void SetTypeEntityCallbacks(Func<Type, TypeEntity> typeToEntity, Func<TypeEntity, Type> entityToType, Func<Lite<TypeEntity>, Type> liteToType)
    {
        TypeEntity.TypeToEntityFunc = typeToEntity;
        TypeEntity.EntityToTypeFunc = entityToType;
        TypeEntity.LiteToTypeFunc = liteToType;
    }
}

public static class TypeEntityExtensions
{
    public static Type ToType(this TypeEntity type)
    {
        return TypeEntity.EntityToTypeFunc(type);
    }

    public static Type ToType(this Lite<TypeEntity> type)
    {
        return TypeEntity.LiteToTypeFunc(type);
    }

    public static TypeEntity ToTypeEntity(this Type type)
    {
        return TypeEntity.TypeToEntityFunc(type);
    }
}
