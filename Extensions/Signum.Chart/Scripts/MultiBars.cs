namespace Signum.Chart.Scripts;

public class MultiBarsChartScript : ChartScript
{
    public MultiBarsChartScript() : base(D3ChartScript.MultiBars)
    {
        Icon = ChartScriptLogic.LoadIcon("multibars.png");
        Columns = new List<ChartScriptColumn>
        {
            new ChartScriptColumn(ChartColumnMessage.VerticalAxis, ChartColumnType.AnyGroupKey),
            new ChartScriptColumn(ChartColumnMessage.SplitBars, ChartColumnType.AnyGroupKey) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.Width, ChartColumnType.AnyNumberDateTime) ,
            new ChartScriptColumn(ChartColumnMessage.Width2, ChartColumnType.AnyNumberDateTime) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.Width3, ChartColumnType.AnyNumberDateTime) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.Width4, ChartColumnType.AnyNumberDateTime) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.Width5, ChartColumnType.AnyNumberDateTime) { IsOptional = true }
        };
        ParameterGroups = new List<ChartScriptParameterGroup>
        {
            new ChartScriptParameterGroup()
            {
                new ChartScriptParameter(ChartParameter.CompleteValues, ChartParameterType.Enum) { ColumnIndex = 0,  ValueDefinition = EnumValueList.Parse("Auto|Yes|No|FromFilters") },
                new ChartScriptParameter(ChartParameter.Scale, ChartParameterType.Scala) { ColumnIndex = 2,  ValueDefinition = new Scala(minZeroMax: true) },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Margin)
            {
                new ChartScriptParameter(ChartParameter.LabelMargin, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 140m } },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Number)
            {
                new ChartScriptParameter(ChartParameter.NumberOpacity, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 0.8m } },
                new ChartScriptParameter(ChartParameter.NumberColor, ChartParameterType.String) {  ValueDefinition = new StringValue("#fff") },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Color)
            {
                new ChartScriptParameter(ChartParameter.ColorCategory, ChartParameterType.Special) {  ValueDefinition = new SpecialParameter(SpecialParameterType.ColorCategory) },
            }
        };
    }
}
