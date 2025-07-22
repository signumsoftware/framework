namespace Signum.Chart.Scripts;

public class MultiLinesChartScript : ChartScript
{
    public MultiLinesChartScript() : base(D3ChartScript.MultiLines)
    {
        Icon = ChartScriptLogic.LoadIcon("multilines.png");
        Columns = new List<ChartScriptColumn>
        {
            new ChartScriptColumn(ChartColumnMessage.HorizontalAxis, ChartColumnType.AllTypes),
            new ChartScriptColumn(ChartColumnMessage.SplitLines, ChartColumnType.AnyGroupKey) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.Height, ChartColumnType.AnyNumberDateTime) ,
            new ChartScriptColumn(ChartColumnMessage.Height2, ChartColumnType.AnyNumberDateTime) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.Height3, ChartColumnType.AnyNumberDateTime) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.Height4, ChartColumnType.AnyNumberDateTime) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.Height5, ChartColumnType.AnyNumberDateTime) { IsOptional = true }
        };
        ParameterGroups = new List<ChartScriptParameterGroup>
        {
            new ChartScriptParameterGroup()
            {
                new ChartScriptParameter(ChartParameter.CompleteValues, ChartParameterType.Enum) { ColumnIndex = 0,  ValueDefinition = EnumValueList.Parse("Auto|Yes|No|FromFilters") },
                new ChartScriptParameter(ChartParameter.HorizontalScale, ChartParameterType.Scala) { ColumnIndex = 0,  ValueDefinition = new Scala(bands: true) },
                new ChartScriptParameter(ChartParameter.VerticalScale, ChartParameterType.Scala) { ColumnIndex = 2,  ValueDefinition = new Scala() },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Margin)
            {
                new ChartScriptParameter(ChartParameter.UnitMargin, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 40m } },
            },
             new ChartScriptParameterGroup(ChartParameterGroupMessage.Number)
            {
                new ChartScriptParameter(ChartParameter.NumberMinWidth, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 20 } },
                new ChartScriptParameter(ChartParameter.NumberOpacity, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 0.8m } },
            },
           new ChartScriptParameterGroup(ChartParameterGroupMessage.Circle)
            {
                new ChartScriptParameter(ChartParameter.CircleAutoReduce, ChartParameterType.Enum) { ValueDefinition = EnumValueList.Parse("Yes|No") },
                new ChartScriptParameter(ChartParameter.CircleRadius, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 5 } },
                new ChartScriptParameter(ChartParameter.CircleStroke, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 2 } },
                new ChartScriptParameter(ChartParameter.CircleRadiusHover, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 15 } },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.ColorCategory)
            {
                new ChartScriptParameter(ChartParameter.ColorCategory, ChartParameterType.Special) {  ValueDefinition = new SpecialParameter(SpecialParameterType.ColorCategory) },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Shape)
            {
                new ChartScriptParameter(ChartParameter.Interpolate, ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("linear|step-before|step-after|cardinal|monotone|basis|bundle") },
            }
        };
    }
}
