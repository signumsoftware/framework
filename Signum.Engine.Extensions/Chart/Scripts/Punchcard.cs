
using Signum.Engine.Chart;
using Signum.Entities.Chart;
using System.Collections.Generic;

namespace Signum.Engine.Chart.Scripts 
{
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
                new ChartScriptParameterGroup("Scale")
                {
                    new ChartScriptParameter("CompleteHorizontalValues", ChartParameterType.Enum) { ColumnIndex = 1,  ValueDefinition = EnumValueList.Parse("Auto|Yes|No") },
                    new ChartScriptParameter("CompleteVerticalValues", ChartParameterType.Enum) { ColumnIndex = 1,  ValueDefinition = EnumValueList.Parse("Auto|Yes|No") },
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
                new ChartScriptParameterGroup("Arrange")
                {
                    new ChartScriptParameter("XSort", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("Ascending|AscendingKey|AscendingToStr|AscendingSumOrder|Descending|DescendingKey|DescendingToStr|DescendingSumOrder|None") },
                    new ChartScriptParameter("YSort", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("Ascending|AscendingKey|AscendingToStr|AscendingSumOrder|Descending|DescendingKey|DescendingToStr|DescendingSumOrder|None") },
                },
                new ChartScriptParameterGroup("Opacity")
                {
                    new ChartScriptParameter("FillOpacity", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 0.4m } },
                },
                new ChartScriptParameterGroup("Fill Color")
                {
                    new ChartScriptParameter("ColorScale", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("ZeroMax|MinMax|Sqrt|Log") },
                    new ChartScriptParameter("ColorInterpolate", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("YlGn|YlGnBu|GnBu|BuGn|PuBuGn|PuBu|BuPu|RdPu|PuRd|OrRd|YlOrRd|YlOrBr|Purples|Blues|Greens|Oranges|Reds|Greys|PuOr|BrBG|PRGn|PiYG|RdBu|RdGy|RdYlBu|Spectral|RdYlGn") },
                    new ChartScriptParameter("FillColor", ChartParameterType.String) {  ValueDefinition = new StringValue("gray") },
                },
                new ChartScriptParameterGroup("Stroke")
                {
                    new ChartScriptParameter("StrokeColor", ChartParameterType.String) {  ValueDefinition = new StringValue("gray") },
                    new ChartScriptParameter("StrokeWidth", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 2m } },
                },
                new ChartScriptParameterGroup("Opacity")
                {
                    new ChartScriptParameter("OpacityScale", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("ZeroMax|MinMax|Log|Sqrt") },
                },
                new ChartScriptParameterGroup("Inner Size")
                { 
                    new ChartScriptParameter("InnerSizeType", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("Absolute|Relative|Independent") },
                    new ChartScriptParameter("InnerFillColor", ChartParameterType.String) {  ValueDefinition = new StringValue("red") },
                }
            };
        }      
    }                
}
