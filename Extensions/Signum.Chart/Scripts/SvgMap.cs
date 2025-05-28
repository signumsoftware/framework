namespace Signum.Chart.Scripts;

public class SvgMapScript : ChartScript
{
    public SvgMapScript(string[] svgMaps) : base(SvgMapsChartScript.SvgMap)
    {
        Icon = ChartScriptLogic.LoadIcon("svgmap.png");
        Columns = new List<ChartScriptColumn>
        {
            new ChartScriptColumn(ChartColumnMessage.LocationCode, ChartColumnType.String),
            new ChartScriptColumn(ChartColumnMessage.Location, ChartColumnType.Groupable) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.ColorScale, ChartColumnType.Magnitude) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.ColorCategory, ChartColumnType.Groupable) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.Opacity, ChartColumnType.Magnitude) { IsOptional = true },
        };
        ParameterGroups = new List<ChartScriptParameterGroup>
        {
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Url)
            {
                new ChartScriptParameter(ChartParameterMessage.SvgUrl, ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse(svgMaps.ToString("|")) },
            },

            new ChartScriptParameterGroup(ChartParameterGroupMessage.Location)
            {
                new ChartScriptParameter(ChartParameterMessage.LocationSelector, ChartParameterType.String) { ColumnIndex = 0,  ValueDefinition = new StringValue("path[id]") },
                new ChartScriptParameter(ChartParameterMessage.LocationAttribute, ChartParameterType.String) { ColumnIndex = 0,  ValueDefinition = new StringValue("id") },
                new ChartScriptParameter(ChartParameterMessage.LocationMatch, ChartParameterType.Enum) {  ColumnIndex = 0, ValueDefinition = EnumValueList.Parse("Exact|Prefix") },
            },

            new ChartScriptParameterGroup(ChartParameterGroupMessage.Stroke)
            {
                new ChartScriptParameter(ChartParameterMessage.StrokeColor, ChartParameterType.String) {  ValueDefinition = new StringValue("") },
                new ChartScriptParameter(ChartParameterMessage.StrokeWidth, ChartParameterType.String) {  ValueDefinition = new StringValue("") },
            },

            new ChartScriptParameterGroup(ChartParameterGroupMessage.Fill)
            {
                new ChartScriptParameter(ChartParameterMessage.NoDataColor, ChartParameterType.String) {  ValueDefinition = new StringValue("#aaa") },
            },

            new ChartScriptParameterGroup(ChartParameterGroupMessage.ColorScale)
            {
                new ChartScriptParameter(ChartParameterMessage.ColorScale, ChartParameterType.Enum) { ColumnIndex = 2,  ValueDefinition = EnumValueList.Parse("ZeroMax|MinMax|Sqrt|Log") },
                new ChartScriptParameter(ChartParameterMessage.ColorInterpolate, ChartParameterType.Special) {  ColumnIndex = 2, ValueDefinition = new SpecialParameter(SpecialParameterType.ColorInterpolate) },
                new ChartScriptParameter(ChartParameterMessage.ColorScaleMaxValue, ChartParameterType.Number) { ColumnIndex = 2, ValueDefinition = new NumberInterval{  DefaultValue = null}  },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.ColorCategory)
            {
                new ChartScriptParameter(ChartParameterMessage.ColorCategory, ChartParameterType.Special) { ColumnIndex = 3, ValueDefinition = new SpecialParameter(SpecialParameterType.ColorCategory) },
            }
        };
    }
}
