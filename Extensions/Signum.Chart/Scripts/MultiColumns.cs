namespace Signum.Chart.Scripts;

public class MultiColumnsChartScript : ChartScript
{
    public MultiColumnsChartScript() : base(D3ChartScript.MultiColumns)
    {
        Icon = ChartScriptLogic.LoadIcon("multicolumns.png");
        Columns = new List<ChartScriptColumn>
        {
            new ChartScriptColumn(ChartColumnMessage.HorizontalAxis, ChartColumnType.Groupable),
            new ChartScriptColumn(ChartColumnMessage.SplitColumns, ChartColumnType.Groupable) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.Height, ChartColumnType.Positionable) ,
            new ChartScriptColumn(ChartColumnMessage.Height2, ChartColumnType.Positionable) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.Height3, ChartColumnType.Positionable) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.Height4, ChartColumnType.Positionable) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.Height5, ChartColumnType.Positionable) { IsOptional = true }
        };
        ParameterGroups = new List<ChartScriptParameterGroup>
        {
            new ChartScriptParameterGroup()
            {
                new ChartScriptParameter(ChartParameterMessage.CompleteValues, ChartParameterType.Enum) { ColumnIndex = 0,  ValueDefinition = EnumValueList.Parse("Auto|Yes|No|FromFilters") },
                new ChartScriptParameter(ChartParameterMessage.Scale, ChartParameterType.Enum) { ColumnIndex = 2,  ValueDefinition = EnumValueList.Parse("ZeroMax (M)|MinMax|MinZeroMax|Log (M)") },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Margin)
            {
                new ChartScriptParameter(ChartParameterMessage.UnitMargin, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 40m } },
                new ChartScriptParameter(ChartParameterMessage.HorizontalMargin, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 2m } },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Number)
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
