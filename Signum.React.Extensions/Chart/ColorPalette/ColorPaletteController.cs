using Microsoft.AspNetCore.Mvc;
using Signum.Entities.Chart;
using Signum.Engine.Chart;
using System.ComponentModel.DataAnnotations;
using Signum.Engine.Authorization;
using HtmlAgilityPack;
using Signum.Entities.Basics;

namespace Signum.React.Chart;

public class ColorPaletteController : ControllerBase
{
    [HttpGet("api/colorPalette/{typeName}")]
    public ColorPaletteTS? ColorPelette(string typeName)
    {
        Type type = TypeLogic.GetType(typeName);

        Schema.Current.AssertAllowed(type, true);

        var palette =  ColorPaletteLogic.ColorPaletteCache.Value.TryGetC(type);
        if (palette == null)
            return null;

        return new ColorPaletteTS
        {
            Lite = palette.ToLite(),
            TypeName = TypeLogic.GetCleanName(type),
            CategoryName = palette.CategoryName,
            Seed = palette.Seed,
            SpecificColors = EnumEntity.Extract(type) != null ?
            palette.SpecificColors.ToDictionary(a => EnumEntity.ToEnum(a.Entity).ToString(), a => a.Color) :
            palette.SpecificColors.ToDictionary(a => a.Entity.Id.ToString(), a => a.Color)
        };
    }
}

public class ColorPaletteTS
{
    public required Lite<ColorPaletteEntity> Lite { get; set; }
    public required string TypeName { get; set; }
    public required string CategoryName { get; set; }
    public required int Seed { get; set; }
    public required Dictionary<string, string> SpecificColors { get; set; }
}
