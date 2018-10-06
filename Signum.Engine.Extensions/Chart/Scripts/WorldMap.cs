
using Signum.Logic.Chart;
using Signum.Entities.Chart;
using Signum.Engine.Chart;
using System.Collections.Generic;

namespace Signum.Logic.Chart.Scripts 
{
    public class WorldMapChartScript : ChartScript                
    {
        public WorldMapChartScript()
        {
            this.Name = "WorldMap";
            this.Icon = ChartScriptLogic.LoadIcon("maps.png");
            this.GroupBy = GroupByChart.Always;
            this.Columns = new List<ChartScriptColumn>
            {
                new ChartScriptColumn("Country Name", ChartColumnType.String) { IsGroupKey = true },
                new ChartScriptColumn("Color", ChartColumnType.Magnitude) 
            };
            this.Parameters = new List<ChartScriptParameter>
            {
                new ChartScriptParameter("Projection", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("orthographic|stereographic|gnomonic|equidistant|equalarea") },
                new ChartScriptParameter("ColorInterpolate", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("YlGn|YlGnBu|GnBu|BuGn|PuBuGn|PuBu|BuPu|RdPu|PuRd|OrRd|YlOrRd|YlOrBr|Purples|Blues|Greens|Oranges|Reds|Greys|PuOr|BrBG|PRGn|PiYG|RdBu|RdGy|RdYlBu|Spectral|RdYlGn") },
                new ChartScriptParameter("ColorScale", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("ZeroMax|MinMax|Sqrt|Log") }
            };
        }      
    }                
}
