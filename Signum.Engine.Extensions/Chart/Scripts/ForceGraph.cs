
using Signum.Logic.Chart;
using Signum.Entities.Chart;
using Signum.Engine.Chart;
using System.Collections.Generic;

namespace Signum.Logic.Chart.Scripts 
{
    public class ForceGraphChartScript : ChartScript                
    {
        public ForceGraphChartScript() : base(D3ChartScript.ForceGraph)
        {
            this.Icon = ChartScriptLogic.LoadIcon("forcegraph.png");
            this.Columns = new List<ChartScriptColumn>
            {
                new ChartScriptColumn("From Node", ChartColumnType.Groupable),
                new ChartScriptColumn("To Node", ChartColumnType.Groupable),
                new ChartScriptColumn("Link width", ChartColumnType.Magnitude) { IsOptional = true }
            };
            this.Parameters = new List<ChartScriptParameter>
            {
                new ChartScriptParameter("Charge", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 150m } },
                new ChartScriptParameter("LinkDistance", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 60m } },
                new ChartScriptParameter("MaxWidth", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 4m } },
                new ChartScriptParameter("ColorScheme", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("category10|accent|dark2|paired|pastel1|pastel2|set1|set2|set3|BrBG[K]|PRGn[K]|PiYG[K]|PuOr[K]|RdBu[K]|RdGy[K]|RdYlBu[K]|RdYlGn[K]|Spectral[K]|Blues[K]|Greys[K]|Oranges[K]|Purples[K]|Reds[K]|BuGn[K]|BuPu[K]|OrRd[K]|PuBuGn[K]|PuBu[K]|PuRd[K]|RdPu[K]|YlGnBu[K]|YlGn[K]|YlOrBr[K]|YlOrRd[K]") },
                new ChartScriptParameter("ColorSchemeSteps", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("3|4|5|6|7|8|9|10|11") }
            };
        }      
    }                
}
