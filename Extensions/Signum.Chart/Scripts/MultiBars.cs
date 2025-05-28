namespace Signum.Chart.Scripts;

public class MultiBarsChartScript : ChartScript
{
    public MultiBarsChartScript() : base(D3ChartScript.MultiBars)
    {
        Icon = ChartScriptLogic.LoadIcon("multibars.png");
        Columns = new List<ChartScriptColumn>
        {
            new ChartScriptColumn(ChartColumnMessage.VerticalAxis, ChartColumnType.Groupable),
            new ChartScriptColumn(ChartColumnMessage.SplitBars, ChartColumnType.Groupable) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.Width, ChartColumnType.Positionable) ,
            new ChartScriptColumn(ChartColumnMessage.Width2, ChartColumnType.Positionable) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.Width3, ChartColumnType.Positionable) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.Width4, ChartColumnType.Positionable) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.Width5, ChartColumnType.Positionable) { IsOptional = true }
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
                new ChartScriptParameter(ChartParameterMessage.LabelMargin, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 140m } },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Number)
            {
                new ChartScriptParameter(ChartParameterMessage.NumberOpacity, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 0.8m } },
                new ChartScriptParameter(ChartParameterMessage.NumberColor, ChartParameterType.String) {  ValueDefinition = new StringValue("#fff") },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Color)
            {
                new ChartScriptParameter(ChartParameterMessage.ColorCategory, ChartParameterType.Special) {  ValueDefinition = new SpecialParameter(SpecialParameterType.ColorCategory) },
            }
        };
    }
}
