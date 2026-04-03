namespace Signum.Chart.Scripts;

public class HeatmapChartScript : ChartScript
{
    public HeatmapChartScript() : base(GoogleMapsChartScript.Heatmap)
    {
        Icon = ChartScriptLogic.LoadIcon("heatmap.png");
        Columns = new List<ChartScriptColumn>
        {
            new ChartScriptColumn(ChartColumnMessage.Latitude, ChartColumnType.AnyNumber),
            new ChartScriptColumn(ChartColumnMessage.Longitude, ChartColumnType.AnyNumber),
            new ChartScriptColumn(ChartColumnMessage.Weight, ChartColumnType.AnyNumber) { IsOptional = true }
        };
        ParameterGroups = new List<ChartScriptParameterGroup>
        {
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Map)
            {
                new ChartScriptParameter(ChartParameter.MapType, ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("Roadmap|Satellite") },
                new ChartScriptParameter(ChartParameter.MapStyle, ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("Standard|Silver|Retro|Dark|Night|Aubergine") }
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Label)
            {
                new ChartScriptParameter(ChartParameter.Opacity, ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("1|0.9|0.8|0.7|0.6|0.5|0.4|0.3|0.2|0.1") },
                new ChartScriptParameter(ChartParameter.RadiousPx, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 10m } },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.ColorGradient)
            {
                new ChartScriptParameter(ChartParameter.ColorCategory, ChartParameterType.Special) {  ValueDefinition = EnumValueList.Parse("Greys|Blue-Red|Fire|Emerald|Cobalt|Purple-Blue|Orange-Red|Purples") },
            }
        };
    }
}
