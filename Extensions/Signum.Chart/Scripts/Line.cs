namespace Signum.Chart.Scripts;

public class LineChartScript : ChartScript
{
    public LineChartScript() : base(D3ChartScript.Line)
    {
        Icon = ChartScriptLogic.LoadIcon("lines.png");
        Columns = new List<ChartScriptColumn>
        {
            new ChartScriptColumn(ChartColumnMessage.HorizontalAxis, ChartColumnType.AllTypes),
            new ChartScriptColumn(ChartColumnMessage.Height, ChartColumnType.AnyNumberDateTime)
        };
        ParameterGroups = new List<ChartScriptParameterGroup>
        {
            new ChartScriptParameterGroup()
            {
                new ChartScriptParameter(ChartParameterMessage.CompleteValues, ChartParameterType.Enum) { ColumnIndex = 0,  ValueDefinition = EnumValueList.Parse("Auto|Yes|No|FromFilters") },
                new ChartScriptParameter(ChartParameterMessage.HorizontalScale, ChartParameterType.Enum) { ColumnIndex = 0,  ValueDefinition = EnumValueList.Parse("Bands|ZeroMax (M)|MinMax (P)|Log (M)") },
                new ChartScriptParameter(ChartParameterMessage.VerticalScale, ChartParameterType.Enum) { ColumnIndex = 1,  ValueDefinition = EnumValueList.Parse("ZeroMax (M)|MinMax|Log (M)") },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Margins)
            {
                new ChartScriptParameter(ChartParameterMessage.UnitMargin, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 40m } },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Number)
            {
                new ChartScriptParameter(ChartParameterMessage.NumberMinWidth, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 20 } },
                new ChartScriptParameter(ChartParameterMessage.NumberOpacity, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 0.8m } },
            },
           new ChartScriptParameterGroup(ChartParameterGroupMessage.Circle)
            {
                new ChartScriptParameter(ChartParameterMessage.CircleAutoReduce, ChartParameterType.Enum) { ValueDefinition = EnumValueList.Parse("Yes|No") },
                new ChartScriptParameter(ChartParameterMessage.CircleRadius, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 5 } },
                new ChartScriptParameter(ChartParameterMessage.CircleStroke, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 2 } },
                new ChartScriptParameter(ChartParameterMessage.CircleRadiusHover, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 15 } },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Color)
            {
                new ChartScriptParameter(ChartParameterMessage.Color, ChartParameterType.String) {  ValueDefinition = new StringValue("steelblue") },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Shape)
            {
                new ChartScriptParameter(ChartParameterMessage.Interpolate, ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("linear|step-before|step-after|cardinal|monotone|basis|bundle|catmull-rom") },
            }
        };
    }
}
