
using Signum.Engine.Chart;
using Signum.Entities.Chart;
using System.Collections.Generic;

namespace Signum.Engine.Chart.Scripts 
{
    public class StackedLinesChartScript : ChartScript                
    {
        public StackedLinesChartScript() : base(D3ChartScript.StackedLines)
        {
            this.Icon = ChartScriptLogic.LoadIcon("stackedareas.png");
            this.Columns = new List<ChartScriptColumn>
            {
                new ChartScriptColumn("Horizontal Axis", ChartColumnType.Groupable),
                new ChartScriptColumn("Areas", ChartColumnType.Groupable) { IsOptional = true },
                new ChartScriptColumn("Height", ChartColumnType.Magnitude) ,
                new ChartScriptColumn("Height 2", ChartColumnType.Magnitude) { IsOptional = true },
                new ChartScriptColumn("Height 3", ChartColumnType.Magnitude) { IsOptional = true },
                new ChartScriptColumn("Height 4", ChartColumnType.Magnitude) { IsOptional = true },
                new ChartScriptColumn("Height 5", ChartColumnType.Magnitude) { IsOptional = true }
            };
            this.ParameterGroups = new List<ChartScriptParameterGroup>
            {
                new ChartScriptParameterGroup("Scale")
                {
                    new ChartScriptParameter("CompleteValues", ChartParameterType.Enum) { ColumnIndex = 1,  ValueDefinition = EnumValueList.Parse("Auto|Yes|No") },
                },
                new ChartScriptParameterGroup("Margin")
                {
                    new ChartScriptParameter("Horizontal Margin", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 20m } },
                },
                new ChartScriptParameterGroup("Number")
                { 
                    new ChartScriptParameter("NumberOpacity", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 0.8m } },
                    new ChartScriptParameter("NumberColor", ChartParameterType.String) {  ValueDefinition = new StringValue("#fff") },
                },
                new ChartScriptParameterGroup("Color Category")
                {
                    new ChartScriptParameter("ColorCategory", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("category10|accent|dark2|paired|pastel1|pastel2|set1|set2|set3|BrBG[K]|PRGn[K]|PiYG[K]|PuOr[K]|RdBu[K]|RdGy[K]|RdYlBu[K]|RdYlGn[K]|Spectral[K]|Blues[K]|Greys[K]|Oranges[K]|Purples[K]|Reds[K]|BuGn[K]|BuPu[K]|OrRd[K]|PuBuGn[K]|PuBu[K]|PuRd[K]|RdPu[K]|YlGnBu[K]|YlGn[K]|YlOrBr[K]|YlOrRd[K]") },
                    new ChartScriptParameter("ColorCategorySteps", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("3|4|5|6|7|8|9|10|11") }
                },
                new ChartScriptParameterGroup("Form")
                {
                    new ChartScriptParameter("Order", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("none|ascending|descending|insideOut|reverse") },
                    new ChartScriptParameter("Stack", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("zero|expand|wiggle|silhouette") },
                    new ChartScriptParameter("Interpolate", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("linear|step-before|step-after|cardinal|monotone|basis") },
                },
            };
        }      
    }                
}
