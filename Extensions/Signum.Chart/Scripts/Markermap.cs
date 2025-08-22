namespace Signum.Chart.Scripts;

public class MarkermapChartScript : ChartScript
{
    public MarkermapChartScript() : base(GoogleMapsChartScript.Markermap)
    {
        Icon = ChartScriptLogic.LoadIcon("markermap.png");
        Columns = new List<ChartScriptColumn>
        {
            new ChartScriptColumn(ChartColumnMessage.Latitude, ChartColumnType.AnyNumber) ,
            new ChartScriptColumn(ChartColumnMessage.Longitude, ChartColumnType.AnyNumber) ,
            new ChartScriptColumn(ChartColumnMessage.Label, ChartColumnType.String) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.Icon, ChartColumnType.String) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.Title, ChartColumnType.String) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.Info, ChartColumnType.String) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.ColorScale, ChartColumnType.DecimalNumber) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.ColorCategory, ChartColumnType.AnyGroupKey) { IsOptional = true }
        };
        ParameterGroups = new List<ChartScriptParameterGroup>
        {
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Map)
            {
                new ChartScriptParameter(ChartParameter.MapType, ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("Roadmap|Satellite") },
                new ChartScriptParameter(ChartParameter.MapStyle, ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("Standard|Silver|Retro|Dark|Night|Aubergine") },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Label)
            {
                new ChartScriptParameter(ChartParameter.AnimateDrop, ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("No|Yes") },
                new ChartScriptParameter(ChartParameter.AnimateOnClick, ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("No|Yes") },
                new ChartScriptParameter(ChartParameter.InfoLinkPosition, ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("Inline|Below") },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Performance)
            {
                new ChartScriptParameter(ChartParameter.ClusterMap, ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("Yes|No") },
            },
            new ChartScriptParameterGroup()
            {
                new ChartScriptParameter(ChartParameter.ColorScale, ChartParameterType.Scala) { ColumnIndex = 6,  ValueDefinition = new Scala() },
                new ChartScriptParameter(ChartParameter.ColorInterpolation, ChartParameterType.Special) {  ColumnIndex = 6, ValueDefinition = new SpecialParameter(SpecialParameterType.ColorInterpolate) },
                new ChartScriptParameter(ChartParameter.ColorCategory, ChartParameterType.Special) {  ColumnIndex = 7, ValueDefinition = new SpecialParameter(SpecialParameterType.ColorCategory)}
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Zoom)
            {
                new ChartScriptParameter(ChartParameter.Zoom, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 2m } },
                new ChartScriptParameter(ChartParameter.MinZoom, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = null } },
                new ChartScriptParameter(ChartParameter.MaxZoom, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = null } },
            },
        };
    }
}
