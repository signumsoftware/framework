using Signum.Entities.Chart;
using Signum.Utilities.Reflection;
using Signum.Entities.Basics;

namespace Signum.Engine.Chart;

public static class ColorPaletteLogic
{
    public static ResetLazy<Dictionary<Type, ColorPaletteEntity>> ColorPaletteCache = null!;

    public static readonly int Limit = 360;

    internal static void Start(SchemaBuilder sb)
    {
        if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
        {
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
                    .ToDictionaryEx(cc => cc.Type.ToType()),
                new InvalidateWith(typeof(ColorPaletteEntity)));
        }
    }

    public static string? ColorFor(Entity entity)
    {
        return ColorPaletteCache.Value.TryGetC(entity.GetType())?.SpecificColors.SingleEx(a => a.Entity.Is(entity))?.Color;
    }
}
