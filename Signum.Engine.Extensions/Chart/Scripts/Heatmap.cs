
using Signum.Logic.Chart;
using Signum.Entities.Chart;
using Signum.Engine.Chart;
using System.Collections.Generic;

namespace Signum.Logic.Chart.Scripts 
{
    public class HeatmapChartScript : ChartScript                
    {
        public HeatmapChartScript() : base(GoogleMapsCharScript.Heatmap)
        {
            this.Icon = ChartScriptLogic.LoadIcon("heatmap.png");
            this.GroupBy = GroupByChart.Optional;
            this.Columns = new List<ChartScriptColumn>
            {
                new ChartScriptColumn("Latitude", ChartColumnType.Magnitude) { IsGroupKey = true },
                new ChartScriptColumn("Longitude", ChartColumnType.Magnitude) { IsGroupKey = true },
                new ChartScriptColumn("Weight", ChartColumnType.Magnitude) { IsOptional = true }
            };
            this.Parameters = new List<ChartScriptParameter>
            {
                new ChartScriptParameter("MapType", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("Roadmap|Satellite") },
                new ChartScriptParameter("Opacity", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("1|0.9|0.8|0.7|0.6|0.5|0.4|0.3|0.2|0.1") },
                new ChartScriptParameter("Radius(px)", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 10m } },
                new ChartScriptParameter("ColorGradient", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("Default|Blue-Red|Purple-Blue|Orange-Red|Fire|Emerald|Cobalt|Purples|Greys") },
                new ChartScriptParameter("MapStyle", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("Standard|Silver|Retro|Dark|Night|Aubergine") }
            };
        }      
    }                
}
