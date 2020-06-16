
using Signum.Engine.Chart;
using Signum.Entities.Chart;
using System.Collections.Generic;

namespace Signum.Engine.Chart.Scripts 
{
    public class ColumnsChartScript : ChartScript                
    {
        public ColumnsChartScript() : base(D3ChartScript.Columns)
        {
            this.Icon = ChartScriptLogic.LoadIcon("columns.png");
            this.Columns = new List<ChartScriptColumn>
            {
                new ChartScriptColumn("Columns", ChartColumnType.Groupable),
                new ChartScriptColumn("Height", ChartColumnType.Positionable) 
            };
            this.ParameterGroups = new List<ChartScriptParameterGroup>
            {
                new ChartScriptParameterGroup("Scale")
                {
                    new ChartScriptParameter("Scale", ChartParameterType.Enum) { ColumnIndex = 1,  ValueDefinition = EnumValueList.Parse("ZeroMax (M)|MinMax|Log (M)") },
                    new ChartScriptParameter("CompleteValues", ChartParameterType.Enum) { ColumnIndex = 1,  ValueDefinition = EnumValueList.Parse("Auto|Yes|No") },
                },
                new ChartScriptParameterGroup("Margins")
                {
                    new ChartScriptParameter("UnitMargin", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 20m } },
                    new ChartScriptParameter("Labels", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("Inside|Margin|None") },
                    new ChartScriptParameter("LabelsMargin", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 100m } },
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
                }
            };
        }      
    }                
}
