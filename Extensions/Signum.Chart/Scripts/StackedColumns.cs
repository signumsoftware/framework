namespace Signum.Chart.Scripts;

public class StackedColumnsChartScript : ChartScript
{
    public StackedColumnsChartScript() : base(D3ChartScript.StackedColumns)
    {
        Icon = ChartScriptLogic.LoadIcon("stackedcolumns.png");
        Columns = new List<ChartScriptColumn>
        {
            new ChartScriptColumn(ChartColumnMessage.HorizontalAxis, ChartColumnType.AnyGroupKey),
            new ChartScriptColumn(ChartColumnMessage.SplitColumns, ChartColumnType.AnyGroupKey) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.Height, ChartColumnType.AnyNumber) ,
            new ChartScriptColumn(ChartColumnMessage.Height2, ChartColumnType.AnyNumber) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.Height3, ChartColumnType.AnyNumber) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.Height4, ChartColumnType.AnyNumber) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.Height5, ChartColumnType.AnyNumber) { IsOptional = true }
        };
        ParameterGroups = new List<ChartScriptParameterGroup>
        {
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Scale)
            {
                new ChartScriptParameter(ChartParameter.CompleteValues, ChartParameterType.Enum) { ColumnIndex = 0,  ValueDefinition = EnumValueList.Parse("Auto|Yes|No|FromFilters") },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Margin)
            {
                new ChartScriptParameter(ChartParameter.UnitMargin, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 40m } },
                new ChartScriptParameter(ChartParameter.Labels, ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("Margin|Inside|None") },
                new ChartScriptParameter(ChartParameter.LabelsMargin, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 100m } },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Number)
            {
                new ChartScriptParameter(ChartParameter.NumberOpacity, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 0.8m } },
                new ChartScriptParameter(ChartParameter.NumberColor, ChartParameterType.String) {  ValueDefinition = new StringValue("#fff") },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.ColorCategory)
            {
                new ChartScriptParameter(ChartParameter.ColorCategory, ChartParameterType.Special) {  ValueDefinition = new SpecialParameter(SpecialParameterType.ColorCategory) },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Shape)
            {
                new ChartScriptParameter(ChartParameter.Stack, ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("zero|expand|wiggle|silhouette") },
                new ChartScriptParameter(ChartParameter.Order, ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("none|ascending|descending|insideOut|reverse") },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.ShowPercent)
            {
                new ChartScriptParameter(ChartParameter.ValueAsPercent, ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("No|Yes") },
            },
        };
    }
}
