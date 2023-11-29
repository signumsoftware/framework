namespace Signum.Chart.Scripts;

public class PunchcardChartScript : ChartScript
{
    public PunchcardChartScript() : base(D3ChartScript.Punchcard)
    {
        Icon = ChartScriptLogic.LoadIcon("punchcard.png");
        Columns = new List<ChartScriptColumn>
        {
            new ChartScriptColumn(ChartColumnMessage.HorizontalAxis, ChartColumnType.Groupable),
            new ChartScriptColumn(ChartColumnMessage.VerticalAxis, ChartColumnType.Groupable),
            new ChartScriptColumn(ChartColumnMessage.Size, ChartColumnType.Magnitude) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.Color, ChartColumnType.Magnitude) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.Opacity, ChartColumnType.Magnitude) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.InnerSize, ChartColumnType.Magnitude) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.Order, ChartColumnType.Magnitude) { IsOptional = true }
        };
        ParameterGroups = new List<ChartScriptParameterGroup>
        {
            new ChartScriptParameterGroup()
            {
                new ChartScriptParameter(ChartParameterMessage.CompleteHorizontalValues, ChartParameterType.Enum) { ColumnIndex = 0,  ValueDefinition = EnumValueList.Parse("Auto|Yes|No|FromFilters") },
                new ChartScriptParameter(ChartParameterMessage.CompleteVerticalValues, ChartParameterType.Enum) { ColumnIndex = 1,  ValueDefinition = EnumValueList.Parse("Auto|Yes|No|FromFilters") },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Size)
            {
                new ChartScriptParameter(ChartParameterMessage.SizeScale, ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("ZeroMax|MinMax|Log|Sqrt") },
                new ChartScriptParameter(ChartParameterMessage.Shape, ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("Circle|Rectangle|ProgressBar") },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Margin)
            {
                new ChartScriptParameter(ChartParameterMessage.XMargin, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 110m } },
                new ChartScriptParameter(ChartParameterMessage.HorizontalLineColor, ChartParameterType.String) {  ValueDefinition = new StringValue("LightGray") },
                new ChartScriptParameter(ChartParameterMessage.VerticalLineColor, ChartParameterType.String) {  ValueDefinition = new StringValue("LightGray") }
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Number)
            {
                new ChartScriptParameter(ChartParameterMessage.NumberOpacity, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 0.8m } },
                new ChartScriptParameter(ChartParameterMessage.NumberColor, ChartParameterType.String) {  ValueDefinition = new StringValue("white") },
            },
            new ChartScriptParameterGroup()
            {
                new ChartScriptParameter(ChartParameterMessage.XSort, ChartParameterType.Enum) { ColumnIndex = 0,  ValueDefinition = EnumValueList.Parse("Ascending|AscendingKey|AscendingToStr|AscendingSumOrder|Descending|DescendingKey|DescendingToStr|DescendingSumOrder|None") },
                new ChartScriptParameter(ChartParameterMessage.YSort, ChartParameterType.Enum) { ColumnIndex = 1,  ValueDefinition = EnumValueList.Parse("Ascending|AscendingKey|AscendingToStr|AscendingSumOrder|Descending|DescendingKey|DescendingToStr|DescendingSumOrder|None") },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Opacity)
            {
                new ChartScriptParameter(ChartParameterMessage.FillOpacity, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 0.4m } },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.FillColor)
            {
                new ChartScriptParameter(ChartParameterMessage.ColorScale, ChartParameterType.Enum) { ColumnIndex = 3, ValueDefinition = EnumValueList.Parse("ZeroMax|MinMax|Sqrt|Log") },
                new ChartScriptParameter(ChartParameterMessage.ColorInterpolate, ChartParameterType.Special) { ColumnIndex = 3, ValueDefinition = new SpecialParameter(SpecialParameterType.ColorInterpolate) },
                new ChartScriptParameter(ChartParameterMessage.FillColor, ChartParameterType.String) {  ValueDefinition = new StringValue("gray") },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Stroke)
            {
                new ChartScriptParameter(ChartParameterMessage.StrokeColor, ChartParameterType.String) {  ValueDefinition = new StringValue("gray") },
                new ChartScriptParameter(ChartParameterMessage.StrokeWidth, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 2m } },
            },
            new ChartScriptParameterGroup()
            {
                new ChartScriptParameter(ChartParameterMessage.OpacityScale, ChartParameterType.Enum) { ColumnIndex = 4,  ValueDefinition = EnumValueList.Parse("ZeroMax|MinMax|Log|Sqrt") },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.InnerSize)
            {
                new ChartScriptParameter(ChartParameterMessage.InnerSizeType, ChartParameterType.Enum) { ColumnIndex = 5, ValueDefinition = EnumValueList.Parse("Absolute|Relative|Independent") },
                new ChartScriptParameter(ChartParameterMessage.InnerFillColor, ChartParameterType.String) { ColumnIndex = 5, ValueDefinition = new StringValue("red") },
            }
        };
    }
}
