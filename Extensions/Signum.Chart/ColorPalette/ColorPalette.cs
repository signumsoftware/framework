using System.ComponentModel;

namespace Signum.Chart.ColorPalette;

[EntityKind(EntityKind.Main, EntityData.Master)]
public class ColorPaletteEntity : Entity
{
    public ColorPaletteEntity()
    {
        BindParent();
    }


    [UniqueIndex]
    public TypeEntity Type { get; set; }

    [StringLengthValidator(Max = 100)]
    public string CategoryName { get; set; }

    public int Seed { get; set; }

    [PreserveOrder, NoRepeatValidator, BindParent]
    public MList<SpecificColorEmbedded> SpecificColors { get; set; } = new MList<SpecificColorEmbedded>();

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => IsNew ? GetType().NewNiceName() : GetType().NiceName() + " " + Type.ToString());
}

public class SpecificColorEmbedded : EmbeddedEntity
{
    [ImplementedByAll, UniqueIndex]
    public Lite<Entity> Entity { get; set; }

    [StringLengthValidator(Max = 100)]
    [Format(FormatAttribute.Color)]
    public string Color { get; set; }

    protected override string? PropertyValidation(PropertyInfo pi)
    {
        if (pi.Name == nameof(Entity))
        {
            var cp = GetParentEntity<ColorPaletteEntity>();

            if (cp.SpecificColors.Any(a => a != this && a.Entity.Is(Entity)))
                return ValidationMessage._0IsRepeated.NiceToString(Entity);
        }

        return base.PropertyValidation(pi);
    }
}

[AutoInit]
public static class ColorPaletteOperation
{
    public static readonly ExecuteSymbol<ColorPaletteEntity> Save;
    public static readonly DeleteSymbol<ColorPaletteEntity> Delete;
}

public enum ColorPaletteMessage
{
    FillAutomatically,
    [Description("Select {0} only if you want to override the automatic color")]
    Select0OnlyIfYouWantToOverrideTheAutomaticColor,
    ShowPalette,
    ShowList
}
