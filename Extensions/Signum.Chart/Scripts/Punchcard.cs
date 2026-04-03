namespace Signum.Chart.Scripts;

public class PunchcardChartScript : ChartScript
{
    public PunchcardChartScript() : base(D3ChartScript.Punchcard)
    {
        Icon = ChartScriptLogic.LoadIcon("punchcard.png");
        Columns = new List<ChartScriptColumn>
        {
            new ChartScriptColumn(ChartColumnMessage.HorizontalAxis, ChartColumnType.AnyGroupKey),
            new ChartScriptColumn(ChartColumnMessage.VerticalAxis, ChartColumnType.AnyGroupKey),
            new ChartScriptColumn(ChartColumnMessage.Size, ChartColumnType.AnyNumber) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.Color, ChartColumnType.AnyNumber) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.Opacity, ChartColumnType.AnyNumber) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.InnerSize, ChartColumnType.AnyNumber) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.Order, ChartColumnType.AnyNumber) { IsOptional = true }
        };
        ParameterGroups = new List<ChartScriptParameterGroup>
        {
            new ChartScriptParameterGroup()
            {
                new ChartScriptParameter(ChartParameter.CompleteHorizontalValues, ChartParameterType.Enum) { ColumnIndex = 0,  ValueDefinition = EnumValueList.Parse("Auto|Yes|No|FromFilters") },
                new ChartScriptParameter(ChartParameter.CompleteVerticalValues, ChartParameterType.Enum) { ColumnIndex = 1,  ValueDefinition = EnumValueList.Parse("Auto|Yes|No|FromFilters") },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Size)
            {
                new ChartScriptParameter(ChartParameter.SizeScale, ChartParameterType.Scala) {  ValueDefinition = new Scala() },
                new ChartScriptParameter(ChartParameter.Shape, ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("Circle|Rectangle|ProgressBar") },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Margin)
            {
                new ChartScriptParameter(ChartParameter.XMargin, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 110m } },
                new ChartScriptParameter(ChartParameter.HorizontalLineColor, ChartParameterType.String) {  ValueDefinition = new StringValue("LightGray") },
                new ChartScriptParameter(ChartParameter.VerticalLineColor, ChartParameterType.String) {  ValueDefinition = new StringValue("LightGray") }
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Number)
            {
                new ChartScriptParameter(ChartParameter.NumberOpacity, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 0.8m } },
                new ChartScriptParameter(ChartParameter.NumberColor, ChartParameterType.String) {  ValueDefinition = new StringValue("white") },
            },
            new ChartScriptParameterGroup()
            {
                new ChartScriptParameter(ChartParameter.XSort, ChartParameterType.Enum) { ColumnIndex = 0,  ValueDefinition = EnumValueList.Parse("Ascending|AscendingKey|AscendingToStr|AscendingSumOrder|Descending|DescendingKey|DescendingToStr|DescendingSumOrder|None") },
                new ChartScriptParameter(ChartParameter.YSort, ChartParameterType.Enum) { ColumnIndex = 1,  ValueDefinition = EnumValueList.Parse("Ascending|AscendingKey|AscendingToStr|AscendingSumOrder|Descending|DescendingKey|DescendingToStr|DescendingSumOrder|None") },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Opacity)
            {
                new ChartScriptParameter(ChartParameter.FillOpacity, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 0.4m } },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.FillColor)
            {
                new ChartScriptParameter(ChartParameter.ColorScale, ChartParameterType.Scala) { ColumnIndex = 3, ValueDefinition = new Scala() },
                new ChartScriptParameter(ChartParameter.ColorInterpolate, ChartParameterType.Special) { ColumnIndex = 3, ValueDefinition = new SpecialParameter(SpecialParameterType.ColorInterpolate) },
                new ChartScriptParameter(ChartParameter.FillColor, ChartParameterType.String) {  ValueDefinition = new StringValue("gray") },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Stroke)
            {
                new ChartScriptParameter(ChartParameter.StrokeColor, ChartParameterType.String) {  ValueDefinition = new StringValue("gray") },
                new ChartScriptParameter(ChartParameter.StrokeWidth, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 2m } },
            },
            new ChartScriptParameterGroup()
            {
                new ChartScriptParameter(ChartParameter.OpacityScale, ChartParameterType.Scala) { ColumnIndex = 4,  ValueDefinition = new Scala() },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.InnerSize)
            {
                new ChartScriptParameter(ChartParameter.InnerSizeType, ChartParameterType.Enum) { ColumnIndex = 5, ValueDefinition = EnumValueList.Parse("Absolute|Relative|Independent") },
                new ChartScriptParameter(ChartParameter.InnerFillColor, ChartParameterType.String) { ColumnIndex = 5, ValueDefinition = new StringValue("red") },
            }
        };
    }
}
