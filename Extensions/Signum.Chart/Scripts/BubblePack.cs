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
                new ChartScriptParameter(ChartParameterMessage.Scale, ChartParameterType.Enum) { ColumnIndex = 1, ValueDefinition = EnumValueList.Parse("ZeroMax|MinMax|Log") },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Stroke)
            {
                new ChartScriptParameter(ChartParameterMessage.StrokeColor, ChartParameterType.String) {  ValueDefinition = new StringValue("") },
                new ChartScriptParameter(ChartParameterMessage.StrokeWidth, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 2m } },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Number)
            {
                new ChartScriptParameter(ChartParameterMessage.NumberOpacity, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 0.8m } },
                new ChartScriptParameter(ChartParameterMessage.NumberSizeLimit, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 18m } },
                new ChartScriptParameter(ChartParameterMessage.NumberColor, ChartParameterType.String) {  ValueDefinition = new StringValue("#fff") }
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Opacity)
            {
                new ChartScriptParameter(ChartParameterMessage.FillOpacity, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 0.8m } },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.ColorScale)
            {
                new ChartScriptParameter(ChartParameterMessage.ColorScale, ChartParameterType.Enum) { ColumnIndex = 3, ValueDefinition = EnumValueList.Parse("ZeroMax|MinMax|Sqrt|Log") },
                new ChartScriptParameter(ChartParameterMessage.ColorInterpolate, ChartParameterType.Special) {  ColumnIndex = 3, ValueDefinition = new SpecialParameter(SpecialParameterType.ColorInterpolate) },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.ColorCategory)
            {
                new ChartScriptParameter(ChartParameterMessage.ColorCategory, ChartParameterType.Special) { ColumnIndex = 4, ValueDefinition = new SpecialParameter(SpecialParameterType.ColorCategory) },
            },
        };
    }
}
