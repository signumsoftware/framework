namespace Signum.Chart.Scripts;

public class BubbleplotChartScript : ChartScript
{
    public BubbleplotChartScript() : base(D3ChartScript.Bubbleplot)
    {
        Icon = ChartScriptLogic.LoadIcon("bubbles.png");
        Columns = new List<ChartScriptColumn>
        {
            new ChartScriptColumn(ChartColumnMessage.Bubble, ChartColumnType.Groupable),
            new ChartScriptColumn(ChartColumnMessage.HorizontalAxis, ChartColumnType.Positionable) ,
            new ChartScriptColumn(ChartColumnMessage.VerticalAxis, ChartColumnType.Positionable) ,
            new ChartScriptColumn(ChartColumnMessage.Size, ChartColumnType.Magnitude),
            new ChartScriptColumn(ChartColumnMessage.ColorScale, ChartColumnType.Magnitude) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.ColorCategory, ChartColumnType.Groupable) { IsOptional = true }
        };
        ParameterGroups = new List<ChartScriptParameterGroup>
        {
            new ChartScriptParameterGroup()
            {
                new ChartScriptParameter(ChartParameterMessage.HorizontalScale, ChartParameterType.Enum) { ColumnIndex = 1,  ValueDefinition = EnumValueList.Parse("ZeroMax (M)|MinMax|Log (M)") },
                new ChartScriptParameter(ChartParameterMessage.VerticalScale, ChartParameterType.Enum) { ColumnIndex = 2,  ValueDefinition = EnumValueList.Parse("ZeroMax (M)|MinMax|Log (M)") },
                new ChartScriptParameter(ChartParameterMessage.SizeScale, ChartParameterType.Enum) { ColumnIndex = 3, ValueDefinition = EnumValueList.Parse("ZeroMax|MinMax|Log") },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Margin)
            {
                new ChartScriptParameter(ChartParameterMessage.UnitMargin, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 40m } },
                new ChartScriptParameter(ChartParameterMessage.TopMargin, ChartParameterType.String) {  ValueDefinition = new StringValue("0.15*") },
                new ChartScriptParameter(ChartParameterMessage.RightMargin, ChartParameterType.String) {  ValueDefinition = new StringValue("0.15*") },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Label)
            {
                new ChartScriptParameter(ChartParameterMessage.ShowLabel, ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("Yes|No") },
                new ChartScriptParameter(ChartParameterMessage.LabelColor, ChartParameterType.String) {  ValueDefinition = new StringValue("#fff") },
                new ChartScriptParameter(ChartParameterMessage.FillOpacity, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 0.4m } },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.ColorScale)
            {
                new ChartScriptParameter(ChartParameterMessage.ColorScale, ChartParameterType.Enum) { ColumnIndex = 4,  ValueDefinition = EnumValueList.Parse("ZeroMax|MinMax|Sqrt|Log") },
                new ChartScriptParameter(ChartParameterMessage.ColorInterpolate, ChartParameterType.Special) {  ColumnIndex = 4, ValueDefinition = new SpecialParameter(SpecialParameterType.ColorInterpolate) },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.ColorCategory)
            {
                new ChartScriptParameter(ChartParameterMessage.ColorCategory, ChartParameterType.Special) { ColumnIndex = 5, ValueDefinition = new SpecialParameter(SpecialParameterType.ColorCategory) },
            }
        };
    }
}
