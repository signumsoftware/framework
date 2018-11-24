
using Signum.Logic.Chart;
using Signum.Entities.Chart;
using Signum.Engine.Chart;
using System.Collections.Generic;

namespace Signum.Logic.Chart.Scripts 
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
            this.Parameters = new List<ChartScriptParameter>
            {
                new ChartScriptParameter("XMargin", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 110m } },
                new ChartScriptParameter("XSort", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("AscendingKey|AscendingToStr|AscendingSumOrder|DescendingKey|DescendingToStr|DescendingSumOrder|None") },
                new ChartScriptParameter("YSort", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("AscendingKey|AscendingToStr|AscendingSumOrder|DescendingKey|DescendingToStr|DescendingSumOrder|None") },
                new ChartScriptParameter("Shape", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("Circle|Rectangle|ProgressBar") },
                new ChartScriptParameter("SizeScale", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("ZeroMax|MinMax|Log|Sqrt") },
                new ChartScriptParameter("ColorScale", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("ZeroMax|MinMax|Sqrt|Log") },
                new ChartScriptParameter("ColorInterpolate", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("YlGn|YlGnBu|GnBu|BuGn|PuBuGn|PuBu|BuPu|RdPu|PuRd|OrRd|YlOrRd|YlOrBr|Purples|Blues|Greens|Oranges|Reds|Greys|PuOr|BrBG|PRGn|PiYG|RdBu|RdGy|RdYlBu|Spectral|RdYlGn") },
                new ChartScriptParameter("StrokeColor", ChartParameterType.String) {  ValueDefinition = new StringValue("gray") },
                new ChartScriptParameter("StrokeWidth", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 2m } },
                new ChartScriptParameter("FillColor", ChartParameterType.String) {  ValueDefinition = new StringValue("gray") },
                new ChartScriptParameter("FillOpacity", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 0.4m } },
                new ChartScriptParameter("OpacityScale", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("ZeroMax|MinMax|Log|Sqrt") },
                new ChartScriptParameter("InnerSizeType", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("Absolute|Relative") },
                new ChartScriptParameter("InnerFillColor", ChartParameterType.String) {  ValueDefinition = new StringValue("red") },
                new ChartScriptParameter("NumberOpacity", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 0.8m } },
                new ChartScriptParameter("NumberColor", ChartParameterType.String) {  ValueDefinition = new StringValue("white") },
                new ChartScriptParameter("HorizontalLineColor", ChartParameterType.String) {  ValueDefinition = new StringValue("LightGray") },
                new ChartScriptParameter("VerticalLineColor", ChartParameterType.String) {  ValueDefinition = new StringValue("LightGray") }
            };
        }      
    }                
}
