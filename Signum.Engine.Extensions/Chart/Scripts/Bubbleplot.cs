
using Signum.Logic.Chart;
using Signum.Entities.Chart;
using Signum.Engine.Chart;
using System.Collections.Generic;

namespace Signum.Logic.Chart.Scripts 
{
    public class BubbleplotChartScript : ChartScript                
    {
        public BubbleplotChartScript() : base(D3ChartScript.Bubbleplot)
        {
            this.Icon = ChartScriptLogic.LoadIcon("bubbles.png");
            this.Columns = new List<ChartScriptColumn>
            {
                new ChartScriptColumn("Color", ChartColumnType.Groupable),
                new ChartScriptColumn("Horizontal Axis", ChartColumnType.Positionable) ,
                new ChartScriptColumn("Vertical Axis", ChartColumnType.Positionable) ,
                new ChartScriptColumn("Size", ChartColumnType.Magnitude) 
            };
            this.ParameterGroups = new List<ChartScriptParameterGroup>
            {
                new ChartScriptParameterGroup("Scale")
                {
                    new ChartScriptParameter("HorizontalScale", ChartParameterType.Enum) { ColumnIndex = 1,  ValueDefinition = EnumValueList.Parse("ZeroMax (M)|MinMax|Log (M)") },
                    new ChartScriptParameter("VerticalScale", ChartParameterType.Enum) { ColumnIndex = 2,  ValueDefinition = EnumValueList.Parse("ZeroMax (M)|MinMax|Log (M)") },
                    new ChartScriptParameter("SizeScale", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("ZeroMax|MinMax|Log") },
                },
                new ChartScriptParameterGroup("Margin")
                {
                    new ChartScriptParameter("UnitMargin", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 40m } },
                },
                new ChartScriptParameterGroup("Label")
                {
                    new ChartScriptParameter("ShowLabel", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("Yes|No") },
                    new ChartScriptParameter("LabelColor", ChartParameterType.String) {  ValueDefinition = new StringValue("#fff") },
                    new ChartScriptParameter("FillOpacity", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 0.4m } },
                },
                new ChartScriptParameterGroup("Color Scale")
                {
                    new ChartScriptParameter("ColorScale", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("Ordinal|ZeroMax|MinMax|Sqrt|Log") },
                    new ChartScriptParameter("ColorInterpolate", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("YlGn|YlGnBu|GnBu|BuGn|PuBuGn|PuBu|BuPu|RdPu|PuRd|OrRd|YlOrRd|YlOrBr|Purples|Blues|Greens|Oranges|Reds|Greys|PuOr|BrBG|PRGn|PiYG|RdBu|RdGy|RdYlBu|Spectral|RdYlGn") },
                },
                new ChartScriptParameterGroup("Color Scale")
                {
                    new ChartScriptParameter("ColorCategory", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("category10|accent|dark2|paired|pastel1|pastel2|set1|set2|set3|BrBG[K]|PRGn[K]|PiYG[K]|PuOr[K]|RdBu[K]|RdGy[K]|RdYlBu[K]|RdYlGn[K]|Spectral[K]|Blues[K]|Greys[K]|Oranges[K]|Purples[K]|Reds[K]|BuGn[K]|BuPu[K]|OrRd[K]|PuBuGn[K]|PuBu[K]|PuRd[K]|RdPu[K]|YlGnBu[K]|YlGn[K]|YlOrBr[K]|YlOrRd[K]") },
                    new ChartScriptParameter("ColorCategorySteps", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("3|4|5|6|7|8|9|10|11") },
                }
            };
        }      
    }                
}
