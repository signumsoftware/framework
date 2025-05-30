namespace Signum.Chart.Scripts;

public class StackedBarsChartScript : ChartScript
{
    public StackedBarsChartScript() : base(D3ChartScript.StackedBars)
    {
        Icon = ChartScriptLogic.LoadIcon("stackedbars.png");
        Columns = new List<ChartScriptColumn>
        {
            new ChartScriptColumn(ChartColumnMessage.VerticalAxis, ChartColumnType.Groupable),
            new ChartScriptColumn(ChartColumnMessage.SplitBars, ChartColumnType.Groupable) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.Width, ChartColumnType.Magnitude) ,
            new ChartScriptColumn(ChartColumnMessage.Width2, ChartColumnType.Magnitude) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.Width3, ChartColumnType.Magnitude) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.Width4, ChartColumnType.Magnitude) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.Width5, ChartColumnType.Magnitude) { IsOptional = true }
        };
        ParameterGroups = new List<ChartScriptParameterGroup>
        {
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Scale)
            {
                new ChartScriptParameter(ChartParameterMessage.CompleteValues, ChartParameterType.Enum) { ColumnIndex = 0,  ValueDefinition = EnumValueList.Parse("Auto|Yes|No|FromFilters") },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Margin)
            {
                new ChartScriptParameter(ChartParameterMessage.Labels, ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("Margin|Inside|None") },
                new ChartScriptParameter(ChartParameterMessage.LabelsMargin, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 100m } },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Number)
            {
                new ChartScriptParameter(ChartParameterMessage.NumberOpacity, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 0.8m } },
                new ChartScriptParameter(ChartParameterMessage.NumberColor, ChartParameterType.String) {  ValueDefinition = new StringValue("#fff") },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.ColorCategory)
            {
                new ChartScriptParameter(ChartParameterMessage.ColorCategory, ChartParameterType.Special) {  ValueDefinition = new SpecialParameter(SpecialParameterType.ColorCategory) },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Shape)
            {
                new ChartScriptParameter(ChartParameterMessage.Stack, ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("zero|expand|wiggle|silhouette") },
                new ChartScriptParameter(ChartParameterMessage.Order, ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("none|ascending|descending|insideOut|reverse") },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.ShowPercent)
            {
                new ChartScriptParameter(ChartParameterMessage.ValueAsPercent, ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("No|Yes") },
            },
        };
    }
}
