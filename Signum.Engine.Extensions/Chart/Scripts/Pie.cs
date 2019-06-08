
using Signum.Engine.Chart;
using Signum.Entities.Chart;
using System.Collections.Generic;

namespace Signum.Engine.Chart.Scripts 
{
    public class PieChartScript : ChartScript                
    {
        public PieChartScript(): base(D3ChartScript.Pie)
        {
            this.Icon = ChartScriptLogic.LoadIcon("doughnut.png");
            this.Columns = new List<ChartScriptColumn>
            {
                new ChartScriptColumn("Sections", ChartColumnType.Groupable),
                new ChartScriptColumn("Angle", ChartColumnType.Magnitude) 
            };
            this.ParameterGroups = new List<ChartScriptParameterGroup>
            {
                new ChartScriptParameterGroup("Form")
                {
                    new ChartScriptParameter("InnerRadious", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 0m } },
                },
                new ChartScriptParameterGroup("Arrange")
                { 
                    new ChartScriptParameter("Sort", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("No|Ascending|Descending") },
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
