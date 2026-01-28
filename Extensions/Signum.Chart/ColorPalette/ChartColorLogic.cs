using System.Collections.Frozen;

namespace Signum.Chart.ColorPalette;

public static class ColorPaletteLogic
{
    public static ResetLazy<FrozenDictionary<Type, ColorPaletteEntity>> ColorPaletteCache = null!;

    public static readonly int Limit = 360;

    internal static void Start(SchemaBuilder sb)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        sb.Include<ColorPaletteEntity>()
            .WithSave(ColorPaletteOperation.Save)
            .WithDelete(ColorPaletteOperation.Delete)
            .WithQuery(() => cc => new
            {
                Entity = cc,
                cc.Id,
                cc.Type,
                cc.CategoryName,
                cc.Seed,
            });

        ColorPaletteCache = sb.GlobalLazy(() =>
            Database.Query<ColorPaletteEntity>()
                .ToFrozenDictionaryEx(cc => cc.Type.ToType()),
            new InvalidateWith(typeof(ColorPaletteEntity)));
    }

    public static string? ColorFor(Entity entity)
    {
        return ColorPaletteCache.Value.TryGetC(entity.GetType())?.SpecificColors.SingleEx(a => a.Entity.Is(entity))?.Color;
    }

    public static string? ColorFor(Lite<Entity> lite)
    {
        return ColorPaletteCache.Value.TryGetC(lite.EntityType)?.SpecificColors.SingleEx(a => a.Entity.Is(lite))?.Color;
    }
}
