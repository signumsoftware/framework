namespace Signum.Chart.Scripts;

public class SvgMapScript : ChartScript
{
    public SvgMapScript(string[] svgMaps) : base(SvgMapsChartScript.SvgMap)
    {
        Icon = ChartScriptLogic.LoadIcon("svgmap.png");
        Columns = new List<ChartScriptColumn>
        {
            new ChartScriptColumn(ChartColumnMessage.LocationCode, ChartColumnType.String),
            new ChartScriptColumn(ChartColumnMessage.Location, ChartColumnType.AnyGroupKey) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.ColorScale, ChartColumnType.AnyNumber) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.ColorCategory, ChartColumnType.AnyGroupKey) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.Opacity, ChartColumnType.AnyNumber) { IsOptional = true },
        };
        ParameterGroups = new List<ChartScriptParameterGroup>
        {
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Url)
            {
                new ChartScriptParameter(ChartParameter.SvgUrl, ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse(svgMaps.ToString("|")) },
            },

            new ChartScriptParameterGroup(ChartParameterGroupMessage.Location)
            {
                new ChartScriptParameter(ChartParameter.LocationSelector, ChartParameterType.String) { ColumnIndex = 0,  ValueDefinition = new StringValue("path[id]") },
                new ChartScriptParameter(ChartParameter.LocationAttribute, ChartParameterType.String) { ColumnIndex = 0,  ValueDefinition = new StringValue("id") },
                new ChartScriptParameter(ChartParameter.LocationMatch, ChartParameterType.Enum) {  ColumnIndex = 0, ValueDefinition = EnumValueList.Parse("Exact|Prefix") },
            },

            new ChartScriptParameterGroup(ChartParameterGroupMessage.Stroke)
            {
                new ChartScriptParameter(ChartParameter.StrokeColor, ChartParameterType.String) {  ValueDefinition = new StringValue("") },
                new ChartScriptParameter(ChartParameter.StrokeWidth, ChartParameterType.String) {  ValueDefinition = new StringValue("") },
            },

            new ChartScriptParameterGroup(ChartParameterGroupMessage.Fill)
            {
                new ChartScriptParameter(ChartParameter.NoDataColor, ChartParameterType.String) {  ValueDefinition = new StringValue("#aaa") },
            },

            new ChartScriptParameterGroup(ChartParameterGroupMessage.ColorScale)
            {
                new ChartScriptParameter(ChartParameter.ColorScale, ChartParameterType.Scala) { ColumnIndex = 2,  ValueDefinition = new Scala() },
                new ChartScriptParameter(ChartParameter.ColorInterpolate, ChartParameterType.Special) {  ColumnIndex = 2, ValueDefinition = new SpecialParameter(SpecialParameterType.ColorInterpolate) },
                new ChartScriptParameter(ChartParameter.ColorScaleMaxValue, ChartParameterType.Number) { ColumnIndex = 2, ValueDefinition = new NumberInterval{  DefaultValue = null}  },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.ColorCategory)
            {
                new ChartScriptParameter(ChartParameter.ColorCategory, ChartParameterType.Special) { ColumnIndex = 3, ValueDefinition = new SpecialParameter(SpecialParameterType.ColorCategory) },
            }
        };
    }
}
