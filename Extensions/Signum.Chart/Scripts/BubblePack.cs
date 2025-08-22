namespace Signum.Chart.Scripts;

public class BubblePackChartScript : ChartScript
{
    public BubblePackChartScript() : base(D3ChartScript.BubblePack)
    {
        Icon = ChartScriptLogic.LoadIcon("bubblepack.png");
        Columns = new List<ChartScriptColumn>
        {
            new ChartScriptColumn(ChartColumnMessage.Bubble, ChartColumnType.AnyGroupKey),
            new ChartScriptColumn(ChartColumnMessage.Size, ChartColumnType.AnyNumber) ,
            new ChartScriptColumn(ChartColumnMessage.Parent, ChartColumnType.AnyGroupKey) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.ColorScale, ChartColumnType.AnyNumber) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.ColorCategory, ChartColumnType.AnyGroupKey) { IsOptional = true }
        };
        ParameterGroups = new List<ChartScriptParameterGroup>
        {
            new ChartScriptParameterGroup()
            {
                new ChartScriptParameter(ChartParameter.Scale, ChartParameterType.Scala) { ColumnIndex = 1, ValueDefinition = new Scala(zeroMax: false, minZeroMax: false, custom: false) },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Stroke)
            {
                new ChartScriptParameter(ChartParameter.StrokeColor, ChartParameterType.String) {  ValueDefinition = new StringValue("") },
                new ChartScriptParameter(ChartParameter.StrokeWidth, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 2m } },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Number)
            {
                new ChartScriptParameter(ChartParameter.NumberOpacity, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 0.8m } },
                new ChartScriptParameter(ChartParameter.NumberSizeLimit, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 18m } },
                new ChartScriptParameter(ChartParameter.NumberColor, ChartParameterType.String) {  ValueDefinition = new StringValue("#fff") }
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Opacity)
            {
                new ChartScriptParameter(ChartParameter.FillOpacity, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 0.8m } },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.ColorScale)
            {
                new ChartScriptParameter(ChartParameter.ColorScale, ChartParameterType.Scala) { ColumnIndex = 3, ValueDefinition = new Scala() },
                new ChartScriptParameter(ChartParameter.ColorInterpolate, ChartParameterType.Special) {  ColumnIndex = 3, ValueDefinition = new SpecialParameter(SpecialParameterType.ColorInterpolate) },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.ColorCategory)
            {
                new ChartScriptParameter(ChartParameter.ColorCategory, ChartParameterType.Special) { ColumnIndex = 4, ValueDefinition = new SpecialParameter(SpecialParameterType.ColorCategory) },
            },
        };
    }
}
