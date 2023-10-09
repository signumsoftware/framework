namespace Signum.Chart.Scripts;

public class BarsChartScript : ChartScript
{
    public BarsChartScript() : base(D3ChartScript.Bars)
    {
        Icon = ChartScriptLogic.LoadIcon("bars.png");
        Columns = new List<ChartScriptColumn>
        {
            new ChartScriptColumn(ChartColumnMessage.Bars, ChartColumnType.Groupable),
            new ChartScriptColumn(ChartColumnMessage.Width, ChartColumnType.Positionable)
        };
        ParameterGroups = new List<ChartScriptParameterGroup>
        {
            new ChartScriptParameterGroup()
            {
                new ChartScriptParameter(ChartParameterMessage.CompleteValues, ChartParameterType.Enum) { ColumnIndex = 0,  ValueDefinition = EnumValueList.Parse("Auto|Yes|No|FromFilters") },
                new ChartScriptParameter(ChartParameterMessage.Scale, ChartParameterType.Enum) { ColumnIndex = 1,  ValueDefinition = EnumValueList.Parse("ZeroMax (M)|MinMax|Log (M)") },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Margins)
            {
                new ChartScriptParameter(ChartParameterMessage.Labels, ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("Inside|InsideAll|Margin|MarginAll|None") },
                new ChartScriptParameter(ChartParameterMessage.LabelsMargin, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 100m } },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Numbers)
            {
                new ChartScriptParameter(ChartParameterMessage.NumberOpacity, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 0.8m } },
                new ChartScriptParameter(ChartParameterMessage.NumberColor, ChartParameterType.String) {  ValueDefinition = new StringValue("#fff") },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.ColorCategory)
            {
                new ChartScriptParameter(ChartParameterMessage.ColorCategory, ChartParameterType.Special) {  ValueDefinition = new SpecialParameter(SpecialParameterType.ColorCategory) },
            }
        };
    }
}
