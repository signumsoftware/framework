namespace Signum.Chart.Scripts;

public class TreeMapChartScript : ChartScript
{
    public TreeMapChartScript() : base(D3ChartScript.Treemap)
    {
        Icon = ChartScriptLogic.LoadIcon("treemap.png");
        Columns = new List<ChartScriptColumn>
        {
            new ChartScriptColumn(ChartColumnMessage.Square, ChartColumnType.AnyGroupKey),
            new ChartScriptColumn(ChartColumnMessage.Size, ChartColumnType.AnyNumber) ,
            new ChartScriptColumn(ChartColumnMessage.Parent, ChartColumnType.AnyGroupKey) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.ColorScale, ChartColumnType.AnyNumber) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.ColorCategory, ChartColumnType.AnyGroupKey) { IsOptional = true }
        };
        ParameterGroups = new List<ChartScriptParameterGroup>
        {
            new ChartScriptParameterGroup()
            {
                new ChartScriptParameter(ChartParameter.Scale, ChartParameterType.Scala) { ColumnIndex = 0, ValueDefinition = new Scala() },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Margin)
            {
                new ChartScriptParameter(ChartParameter.Padding, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 4m } },
                new ChartScriptParameter(ChartParameter.Opacity, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 0.5m } },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Number)
            {
                new ChartScriptParameter(ChartParameter.NumberOpacity, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 0.8m } },
                new ChartScriptParameter(ChartParameter.NumberColor, ChartParameterType.String) {  ValueDefinition = new StringValue("#fff") },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.ColorScale)
            {
                new ChartScriptParameter(ChartParameter.ColorScale, ChartParameterType.Scala) { ColumnIndex = 3, ValueDefinition = new Scala() },
                new ChartScriptParameter(ChartParameter.ColorInterpolate, ChartParameterType.Special) { ColumnIndex = 3, ValueDefinition = new SpecialParameter(SpecialParameterType.ColorInterpolate) },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.ColorCategory)
            {
                new ChartScriptParameter(ChartParameter.ColorCategory, ChartParameterType.Special) { ColumnIndex = 4, ValueDefinition = new SpecialParameter(SpecialParameterType.ColorCategory) },
            }
        };
    }
}
