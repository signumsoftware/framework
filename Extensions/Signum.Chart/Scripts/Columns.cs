namespace Signum.Chart.Scripts;

public class ColumnsChartScript : ChartScript
{
    public ColumnsChartScript() : base(D3ChartScript.Columns)
    {
        Icon = ChartScriptLogic.LoadIcon("columns.png");
        Columns = new List<ChartScriptColumn>
        {
            new ChartScriptColumn(ChartColumnMessage.Columns, ChartColumnType.AnyGroupKey),
            new ChartScriptColumn(ChartColumnMessage.Height, ChartColumnType.AnyNumberDateTime)
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
                new ChartScriptParameter(ChartParameter.UnitMargin, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 40m } },
                new ChartScriptParameter(ChartParameter.Labels, ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("Inside|InsideAll|Margin|MarginAll|None") },
                new ChartScriptParameter(ChartParameter.LabelsMargin, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 100m } },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Number)
            {
                new ChartScriptParameter(ChartParameter.NumberOpacity, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 0.8m } },
                new ChartScriptParameter(ChartParameter.NumberColor, ChartParameterType.String) {  ValueDefinition = new StringValue("#fff") },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Color)
            {
                new ChartScriptParameter(ChartParameter.ColorCategory, ChartParameterType.Special) {  ValueDefinition = new SpecialParameter(SpecialParameterType.ColorCategory) },
                new ChartScriptParameter(ChartParameter.ForceColor, ChartParameterType.String) {  ValueDefinition = new StringValue("") },
            }
        };
    }
}
