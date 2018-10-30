
using Signum.Logic.Chart;
using Signum.Entities.Chart;
using Signum.Engine.Chart;
using System.Collections.Generic;

namespace Signum.Logic.Chart.Scripts 
{
    public class PieChartScript : ChartScript                
    {
        public PieChartScript(): base(D3ChartScript.Pie)
        {
            this.Icon = ChartScriptLogic.LoadIcon("doughnut.png");
            this.GroupBy = GroupByChart.Always;
            this.Columns = new List<ChartScriptColumn>
            {
                new ChartScriptColumn("Sections", ChartColumnType.Groupable) { IsGroupKey = true },
                new ChartScriptColumn("Angle", ChartColumnType.Magnitude) 
            };
            this.Parameters = new List<ChartScriptParameter>
            {
                new ChartScriptParameter("InnerRadious", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 0m } },
                new ChartScriptParameter("Sort", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("No|Ascending|Descending") },
                new ChartScriptParameter("ColorScheme", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("category10|accent|dark2|paired|pastel1|pastel2|set1|set2|set3|BrBG[K]|PRGn[K]|PiYG[K]|PuOr[K]|RdBu[K]|RdGy[K]|RdYlBu[K]|RdYlGn[K]|Spectral[K]|Blues[K]|Greys[K]|Oranges[K]|Purples[K]|Reds[K]|BuGn[K]|BuPu[K]|OrRd[K]|PuBuGn[K]|PuBu[K]|PuRd[K]|RdPu[K]|YlGnBu[K]|YlGn[K]|YlOrBr[K]|YlOrRd[K]") },
                new ChartScriptParameter("ColorSchemeSteps", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("3|4|5|6|7|8|9|10|11") }
            };
        }      
    }                
}
