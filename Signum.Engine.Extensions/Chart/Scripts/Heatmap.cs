using Signum.Entities.Chart;

namespace Signum.Engine.Chart.Scripts;

public class HeatmapChartScript : ChartScript                
{
    public HeatmapChartScript() : base(GoogleMapsChartScript.Heatmap)
    {
        this.Icon = ChartScriptLogic.LoadIcon("heatmap.png");
        this.Columns = new List<ChartScriptColumn>
        {
            new ChartScriptColumn("Latitude", ChartColumnType.Magnitude),
            new ChartScriptColumn("Longitude", ChartColumnType.Magnitude),
            new ChartScriptColumn("Weight", ChartColumnType.Magnitude) { IsOptional = true }
        };
        this.ParameterGroups = new List<ChartScriptParameterGroup>
        {
            new ChartScriptParameterGroup("Map")
            {
                new ChartScriptParameter("MapType", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("Roadmap|Satellite") },
                new ChartScriptParameter("MapStyle", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("Standard|Silver|Retro|Dark|Night|Aubergine") }
            },
            new ChartScriptParameterGroup("Label")
            {
                new ChartScriptParameter("Opacity", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("1|0.9|0.8|0.7|0.6|0.5|0.4|0.3|0.2|0.1") },
                new ChartScriptParameter("Radius(px)", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 10m } },
            },
            new ChartScriptParameterGroup("Color Gradient")
            {
                new ChartScriptParameter("ColorInterpolate", ChartParameterType.Special) {  ValueDefinition = new SpecialParameter(SpecialParameterType.ColorInterpolate) },
            }
        };
    }      
}                
