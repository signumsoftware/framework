using Signum.Entities.Chart;

namespace Signum.Engine.Chart.Scripts;

public class PunchcardChartScript : ChartScript                
{
    public PunchcardChartScript() : base(D3ChartScript.Punchcard)
    {
        this.Icon = ChartScriptLogic.LoadIcon("punchcard.png");
        this.Columns = new List<ChartScriptColumn>
        {
            new ChartScriptColumn("Horizontal Axis", ChartColumnType.Groupable),
            new ChartScriptColumn("Vertical Axis", ChartColumnType.Groupable),
            new ChartScriptColumn("Size", ChartColumnType.Magnitude) { IsOptional = true },
            new ChartScriptColumn("Color", ChartColumnType.Magnitude) { IsOptional = true },
            new ChartScriptColumn("Opacity", ChartColumnType.Magnitude) { IsOptional = true },
            new ChartScriptColumn("Inner Size", ChartColumnType.Magnitude) { IsOptional = true },
            new ChartScriptColumn("Ordering", ChartColumnType.Magnitude) { IsOptional = true }
        };
        this.ParameterGroups = new List<ChartScriptParameterGroup>
        {
            new ChartScriptParameterGroup()
            {
                new ChartScriptParameter("CompleteHorizontalValues", ChartParameterType.Enum) { ColumnIndex = 0,  ValueDefinition = EnumValueList.Parse("Auto|Yes|No|FromFilters") },
                new ChartScriptParameter("CompleteVerticalValues", ChartParameterType.Enum) { ColumnIndex = 1,  ValueDefinition = EnumValueList.Parse("Auto|Yes|No|FromFilters") },
            },
            new ChartScriptParameterGroup("Size")
            {
                new ChartScriptParameter("SizeScale", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("ZeroMax|MinMax|Log|Sqrt") },
                new ChartScriptParameter("Shape", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("Circle|Rectangle|ProgressBar") },
            },
            new ChartScriptParameterGroup("Margin")
            {
                new ChartScriptParameter("XMargin", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 110m } },
                new ChartScriptParameter("HorizontalLineColor", ChartParameterType.String) {  ValueDefinition = new StringValue("LightGray") },
                new ChartScriptParameter("VerticalLineColor", ChartParameterType.String) {  ValueDefinition = new StringValue("LightGray") }
            },
            new ChartScriptParameterGroup("Number")
            {
                new ChartScriptParameter("NumberOpacity", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 0.8m } },
                new ChartScriptParameter("NumberColor", ChartParameterType.String) {  ValueDefinition = new StringValue("white") },
            },
            new ChartScriptParameterGroup()
            {
                new ChartScriptParameter("XSort", ChartParameterType.Enum) { ColumnIndex = 0,  ValueDefinition = EnumValueList.Parse("Ascending|AscendingKey|AscendingToStr|AscendingSumOrder|Descending|DescendingKey|DescendingToStr|DescendingSumOrder|None") },
                new ChartScriptParameter("YSort", ChartParameterType.Enum) { ColumnIndex = 1,  ValueDefinition = EnumValueList.Parse("Ascending|AscendingKey|AscendingToStr|AscendingSumOrder|Descending|DescendingKey|DescendingToStr|DescendingSumOrder|None") },
            },
            new ChartScriptParameterGroup("Opacity")
            {
                new ChartScriptParameter("FillOpacity", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 0.4m } },
            },
            new ChartScriptParameterGroup("Fill Color")
            {
                new ChartScriptParameter("ColorScale", ChartParameterType.Enum) { ColumnIndex = 3, ValueDefinition = EnumValueList.Parse("ZeroMax|MinMax|Sqrt|Log") },
                new ChartScriptParameter("ColorInterpolate", ChartParameterType.Special) { ColumnIndex = 3, ValueDefinition = new SpecialParameter(SpecialParameterType.ColorInterpolate) },
                new ChartScriptParameter("FillColor", ChartParameterType.String) {  ValueDefinition = new StringValue("gray") },
            },
            new ChartScriptParameterGroup("Stroke")
            {
                new ChartScriptParameter("StrokeColor", ChartParameterType.String) {  ValueDefinition = new StringValue("gray") },
                new ChartScriptParameter("StrokeWidth", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 2m } },
            },
            new ChartScriptParameterGroup()
            {
                new ChartScriptParameter("OpacityScale", ChartParameterType.Enum) { ColumnIndex = 4,  ValueDefinition = EnumValueList.Parse("ZeroMax|MinMax|Log|Sqrt") },
            },
            new ChartScriptParameterGroup("Inner Size")
            { 
                new ChartScriptParameter("InnerSizeType", ChartParameterType.Enum) { ColumnIndex = 5, ValueDefinition = EnumValueList.Parse("Absolute|Relative|Independent") },
                new ChartScriptParameter("InnerFillColor", ChartParameterType.String) { ColumnIndex = 5, ValueDefinition = new StringValue("red") },
            }
        };
    }      
}                
