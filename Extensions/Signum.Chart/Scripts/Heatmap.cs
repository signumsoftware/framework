namespace Signum.Chart.Scripts;

public class HeatmapChartScript : ChartScript
{
    public HeatmapChartScript() : base(GoogleMapsChartScript.Heatmap)
    {
        Icon = ChartScriptLogic.LoadIcon("heatmap.png");
        Columns = new List<ChartScriptColumn>
        {
            new ChartScriptColumn(ChartColumnMessage.Latitude, ChartColumnType.Magnitude),
            new ChartScriptColumn(ChartColumnMessage.Longitude, ChartColumnType.Magnitude),
            new ChartScriptColumn(ChartColumnMessage.Weight, ChartColumnType.Magnitude) { IsOptional = true }
        };
        ParameterGroups = new List<ChartScriptParameterGroup>
        {
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Map)
            {
                new ChartScriptParameter(ChartParameterMessage.MapType, ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("Roadmap|Satellite") },
                new ChartScriptParameter(ChartParameterMessage.MapStyle, ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("Standard|Silver|Retro|Dark|Night|Aubergine") }
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Label)
            {
                new ChartScriptParameter(ChartParameterMessage.Opacity, ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("1|0.9|0.8|0.7|0.6|0.5|0.4|0.3|0.2|0.1") },
                new ChartScriptParameter(ChartParameterMessage.RadiousPx, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 10m } },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.ColorGradient)
            {
                new ChartScriptParameter(ChartParameterMessage.ColorInterpolate, ChartParameterType.Special) {  ValueDefinition = new SpecialParameter(SpecialParameterType.ColorInterpolate) },
            }
        };
    }
}
