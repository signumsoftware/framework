
using Signum.Logic.Chart;
using Signum.Entities.Chart;
using Signum.Engine.Chart;
using System.Collections.Generic;

namespace Signum.Logic.Chart.Scripts 
{
    public class StackedColumnsChartScript : ChartScript                
    {
        public StackedColumnsChartScript() : base(D3ChartScript.StackedColumns)
        {
            this.Icon = ChartScriptLogic.LoadIcon("stackedcolumns.png");
            this.GroupBy = GroupByChart.Always;
            this.Columns = new List<ChartScriptColumn>
            {
                new ChartScriptColumn("Horizontal Axis", ChartColumnType.Groupable) { IsGroupKey = true },
                new ChartScriptColumn("Split Columns", ChartColumnType.Groupable) { IsGroupKey = true, IsOptional = true },
                new ChartScriptColumn("Height", ChartColumnType.Magnitude) ,
                new ChartScriptColumn("Height 2", ChartColumnType.Magnitude) { IsOptional = true },
                new ChartScriptColumn("Height 3", ChartColumnType.Magnitude) { IsOptional = true },
                new ChartScriptColumn("Height 4", ChartColumnType.Magnitude) { IsOptional = true },
                new ChartScriptColumn("Height 5", ChartColumnType.Magnitude) { IsOptional = true }
            };
            this.Parameters = new List<ChartScriptParameter>
            {
                new ChartScriptParameter("Stack", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("zero|expand|wiggle|silhouette") },
                new ChartScriptParameter("Order", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("none|ascending|descending|insideOut|reverse") },
                new ChartScriptParameter("UnitMargin", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 20m } },
                new ChartScriptParameter("Labels", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("Margin|Inside|None") },
                new ChartScriptParameter("LabelsMargin", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 100m } },
                new ChartScriptParameter("NumberOpacity", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 0.8m } },
                new ChartScriptParameter("NumberColor", ChartParameterType.String) {  ValueDefinition = null },
                new ChartScriptParameter("ColorScheme", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("category10|accent|dark2|paired|pastel1|pastel2|set1|set2|set3|BrBG[K]|PRGn[K]|PiYG[K]|PuOr[K]|RdBu[K]|RdGy[K]|RdYlBu[K]|RdYlGn[K]|Spectral[K]|Blues[K]|Greys[K]|Oranges[K]|Purples[K]|Reds[K]|BuGn[K]|BuPu[K]|OrRd[K]|PuBuGn[K]|PuBu[K]|PuRd[K]|RdPu[K]|YlGnBu[K]|YlGn[K]|YlOrBr[K]|YlOrRd[K]") },
                new ChartScriptParameter("ColorSchemeSteps", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("3|4|5|6|7|8|9|10|11") }
            };
        }      
    }                
}
