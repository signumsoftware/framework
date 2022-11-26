using Signum.Entities.Basics;

namespace Signum.Entities.Chart;

[EntityKind(EntityKind.String, EntityData.Master)]
public class ColorPaletteEntity : Entity
{
    [UniqueIndex]
    [StringLengthValidator(Max = 100)]
    public TypeEntity Type { get; set; }

    [StringLengthValidator(Max = 100)]
    public string CategoryName { get; set; }

    public int Seed { get; set; }

    [PreserveOrder, NoRepeatValidator]
    public MList<SpecificColorEmbedded> SpecificColors { get; set; } = new MList<SpecificColorEmbedded>();

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => Type.ToString()!);
}

public class SpecificColorEmbedded : EmbeddedEntity
{
    [ImplementedByAll, UniqueIndex]
    public Lite<Entity> Entity { get; set; }

    [StringLengthValidator(Max = 100)]
    public string Color { get; set; }
}

[AutoInit]
public static class ColorPaletteOperation
{
    public static readonly ExecuteSymbol<ColorPaletteEntity> Save;
    public static readonly DeleteSymbol<ColorPaletteEntity> Delete;
}

