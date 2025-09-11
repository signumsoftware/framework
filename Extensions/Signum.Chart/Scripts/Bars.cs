namespace Signum.Chart.Scripts;

public class BarsChartScript : ChartScript
{
    
    public BarsChartScript() : base(D3ChartScript.Bars)
    {
        Icon = ChartScriptLogic.LoadIcon("bars.png");
        Columns = new List<ChartScriptColumn>
        {
            new ChartScriptColumn(ChartColumnMessage.Bars, ChartColumnType.AnyGroupKey),
            new ChartScriptColumn(ChartColumnMessage.Width, ChartColumnType.AnyNumberDateTime)
        };
        ParameterGroups = new List<ChartScriptParameterGroup>
        {
            new ChartScriptParameterGroup()
            {
                new ChartScriptParameter(ChartParameter.CompleteValues, ChartParameterType.Enum) { ColumnIndex = 0,  ValueDefinition = EnumValueList.Parse("Auto|Yes|No|FromFilters") },
                new ChartScriptParameter(ChartParameter.Scale, ChartParameterType.Scala) { ColumnIndex = 1,  ValueDefinition = new Scala(minZeroMax: true) },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Margins)
            {
                new ChartScriptParameter(ChartParameter.Labels, ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("Inside|InsideAll|Margin|MarginAll|None") },
                new ChartScriptParameter(ChartParameter.LabelsMargin, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 100m } },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Numbers)
            {
                new ChartScriptParameter(ChartParameter.NumberOpacity, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 0.8m } },
                new ChartScriptParameter(ChartParameter.NumberColor, ChartParameterType.String) {  ValueDefinition = new StringValue("#fff") },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.ColorCategory)
            {
                new ChartScriptParameter(ChartParameter.ColorCategory, ChartParameterType.Special) {  ValueDefinition = new SpecialParameter(SpecialParameterType.ColorCategory) },
            }
        };
    }
}
