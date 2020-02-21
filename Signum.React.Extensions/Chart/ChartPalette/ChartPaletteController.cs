using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Signum.Engine.Basics;
using Signum.Entities.Chart;
using Signum.Engine.Chart;
using System.ComponentModel.DataAnnotations;

namespace Signum.React.Chart
{
    public class ChartPaletteController : ControllerBase
    {
        [HttpGet("api/chart/colorPalette")]
        public List<string> ColorPelettes()
        {
            return ChartColorLogic.Colors.Value.Keys.Select(t => TypeLogic.GetCleanName(t)).ToList();
        }

        [HttpGet("api/chart/colorPalette/{typeName}")]
        public ChartPaletteModel ColorPelette(string typeName)
        {
            return ChartColorLogic.GetPalette(TypeLogic.GetType(typeName));
        }

        [HttpPost("api/chart/colorPalette/{typeName}/new/{paletteName}")]
        public void NewColorPelette(string typeName, string paletteName)
        {
            ChartColorLogic.CreateNewPalette(TypeLogic.GetType(typeName), paletteName);
        }

        [HttpPost("api/chart/colorPalette/{typeName}/delete")]
        public void DeleteColorPalete(string typeName)
        {
            ChartColorLogic.DeletePalette(TypeLogic.GetType(typeName));
        }

        [HttpPost("api/chart/colorPalette/{typeName}/save")]
        public void SaveColorPalete(string typeName, [Required, FromBody]ChartPaletteModel paletteModel)
        {
            ChartColorLogic.SavePalette(paletteModel);
        }
    }
}
