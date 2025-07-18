namespace Signum.Chart.Scripts;

public class StackedLinesChartScript : ChartScript
{
    public StackedLinesChartScript() : base(D3ChartScript.StackedLines)
    {
        Icon = ChartScriptLogic.LoadIcon("stackedareas.png");
        Columns = new List<ChartScriptColumn>
        {
            new ChartScriptColumn(ChartColumnMessage.HorizontalAxis, ChartColumnType.AllTypes),
            new ChartScriptColumn(ChartColumnMessage.Areas, ChartColumnType.AnyGroupKey) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.Height, ChartColumnType.AnyNumber) ,
            new ChartScriptColumn(ChartColumnMessage.Height2, ChartColumnType.AnyNumber) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.Height3, ChartColumnType.AnyNumber) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.Height4, ChartColumnType.AnyNumber) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.Height5, ChartColumnType.AnyNumber) { IsOptional = true }
        };
        ParameterGroups = new List<ChartScriptParameterGroup>
        {
            new ChartScriptParameterGroup()
            {
                new ChartScriptParameter(ChartParameterMessage.CompleteValues, ChartParameterType.Enum) { ColumnIndex = 0,  ValueDefinition = EnumValueList.Parse("Auto|Yes|No|FromFilters") },
                new ChartScriptParameter(ChartParameterMessage.HorizontalScale, ChartParameterType.Enum) { ColumnIndex = 0,  ValueDefinition = EnumValueList.Parse("Bands|ZeroMax (M)|MinMax (P)|Log (M)") },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Margin)
            {
                new ChartScriptParameter(ChartParameterMessage.HorizontalMargin, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 20m } },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Number)
            {
                new ChartScriptParameter(ChartParameterMessage.NumberOpacity, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 0.8m } },
                new ChartScriptParameter(ChartParameterMessage.NumberColor, ChartParameterType.String) {  ValueDefinition = new StringValue("#fff") },
            },
            new ChartScriptParameterGroup(ChartParameterMessage.ColorCategory)
            {
                new ChartScriptParameter(ChartParameterMessage.ColorCategory, ChartParameterType.Special) {  ValueDefinition = new SpecialParameter(SpecialParameterType.ColorCategory) },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Shape)
            {
                new ChartScriptParameter(ChartParameterMessage.Order, ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("none|ascending|descending|insideOut|reverse") },
                new ChartScriptParameter(ChartParameterMessage.Stack, ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("zero|expand|wiggle|silhouette") },
                new ChartScriptParameter(ChartParameterMessage.Interpolate, ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("linear|step-before|step-after|cardinal|monotone|basis") },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.ShowPercent)
            {
                new ChartScriptParameter(ChartParameterMessage.ValueAsPercent, ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("No|Yes") },
            },
        };
    }
}
