namespace Signum.Chart.Scripts;

public class BubbleplotChartScript : ChartScript
{
    public BubbleplotChartScript() : base(D3ChartScript.Bubbleplot)
    {
        Icon = ChartScriptLogic.LoadIcon("bubbles.png");
        Columns = new List<ChartScriptColumn>
        {
            new ChartScriptColumn(ChartColumnMessage.Bubble, ChartColumnType.AnyGroupKey),
            new ChartScriptColumn(ChartColumnMessage.HorizontalAxis, ChartColumnType.AnyNumberDateTime) ,
            new ChartScriptColumn(ChartColumnMessage.VerticalAxis, ChartColumnType.AnyNumberDateTime) ,
            new ChartScriptColumn(ChartColumnMessage.Size, ChartColumnType.AnyNumber),
            new ChartScriptColumn(ChartColumnMessage.ColorScale, ChartColumnType.AnyNumber) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.ColorCategory, ChartColumnType.AnyGroupKey) { IsOptional = true }
        };
        ParameterGroups = new List<ChartScriptParameterGroup>
        {
            new ChartScriptParameterGroup()
            {
                new ChartScriptParameter(ChartParameter.HorizontalScale, ChartParameterType.Scala) { ColumnIndex = 1,  ValueDefinition = new Scala() },
                new ChartScriptParameter(ChartParameter.VerticalScale, ChartParameterType.Scala) { ColumnIndex = 2,  ValueDefinition = new Scala() },
                new ChartScriptParameter(ChartParameter.SizeScale, ChartParameterType.Scala) { ColumnIndex = 3, ValueDefinition = new Scala() },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Margin)
            {
                new ChartScriptParameter(ChartParameter.UnitMargin, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 40m } },
                new ChartScriptParameter(ChartParameter.TopMargin, ChartParameterType.String) {  ValueDefinition = new StringValue("0.15*") },
                new ChartScriptParameter(ChartParameter.RightMargin, ChartParameterType.String) {  ValueDefinition = new StringValue("0.15*") },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Label)
            {
                new ChartScriptParameter(ChartParameter.ShowLabel, ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("Yes|No") },
                new ChartScriptParameter(ChartParameter.LabelColor, ChartParameterType.String) {  ValueDefinition = new StringValue("#fff") },
                new ChartScriptParameter(ChartParameter.FillOpacity, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 0.4m } },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.ColorScale)
            {
                new ChartScriptParameter(ChartParameter.ColorScale, ChartParameterType.Scala) { ColumnIndex = 4,  ValueDefinition = new Scala() },
                new ChartScriptParameter(ChartParameter.ColorInterpolate, ChartParameterType.Special) {  ColumnIndex = 4, ValueDefinition = new SpecialParameter(SpecialParameterType.ColorInterpolate) },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.ColorCategory)
            {
                new ChartScriptParameter(ChartParameter.ColorCategory, ChartParameterType.Special) { ColumnIndex = 5, ValueDefinition = new SpecialParameter(SpecialParameterType.ColorCategory) },
            }
        };
    }
}
