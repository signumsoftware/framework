namespace Signum.Chart.Scripts;

public class TreeMapChartScript : ChartScript
{
    public TreeMapChartScript() : base(D3ChartScript.Treemap)
    {
        Icon = ChartScriptLogic.LoadIcon("treemap.png");
        Columns = new List<ChartScriptColumn>
        {
            new ChartScriptColumn(ChartColumnMessage.Square, ChartColumnType.Groupable),
            new ChartScriptColumn(ChartColumnMessage.Size, ChartColumnType.Magnitude) ,
            new ChartScriptColumn(ChartColumnMessage.Parent, ChartColumnType.Groupable) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.ColorScale, ChartColumnType.Magnitude) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.ColorCategory, ChartColumnType.Groupable) { IsOptional = true }
        };
        ParameterGroups = new List<ChartScriptParameterGroup>
        {
            new ChartScriptParameterGroup()
            {
                new ChartScriptParameter(ChartParameterMessage.Scale, ChartParameterType.Enum) { ColumnIndex = 0, ValueDefinition = EnumValueList.Parse("ZeroMax|MinMax|Log") },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Margin)
            {
                new ChartScriptParameter(ChartParameterMessage.Padding, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 4m } },
                new ChartScriptParameter(ChartParameterMessage.Opacity, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 0.5m } },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Number)
            {
                new ChartScriptParameter(ChartParameterMessage.NumberOpacity, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 0.8m } },
                new ChartScriptParameter(ChartParameterMessage.NumberColor, ChartParameterType.String) {  ValueDefinition = new StringValue("#fff") },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.ColorScale)
            {
                new ChartScriptParameter(ChartParameterMessage.ColorScale, ChartParameterType.Enum) { ColumnIndex = 3, ValueDefinition = EnumValueList.Parse("ZeroMax|MinMax|Sqrt|Log") },
                new ChartScriptParameter(ChartParameterMessage.ColorInterpolate, ChartParameterType.Special) { ColumnIndex = 3, ValueDefinition = new SpecialParameter(SpecialParameterType.ColorInterpolate) },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.ColorCategory)
            {
                new ChartScriptParameter(ChartParameterMessage.ColorCategory, ChartParameterType.Special) { ColumnIndex = 4, ValueDefinition = new SpecialParameter(SpecialParameterType.ColorCategory) },
            }
        };
    }
}
