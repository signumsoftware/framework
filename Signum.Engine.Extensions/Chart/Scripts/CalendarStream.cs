
using Signum.Engine.Chart;
using Signum.Entities.Chart;
using System.Collections.Generic;

namespace Signum.Engine.Chart.Scripts 
{
    public class CalendarStreamChartScript : ChartScript                
    {
        public CalendarStreamChartScript() : base(D3ChartScript.CalendarStream)
        {
            this.Icon = ChartScriptLogic.LoadIcon("calendar.png");
            this.Columns = new List<ChartScriptColumn>
            {
                new ChartScriptColumn("Date", ChartColumnType.Date),
                new ChartScriptColumn("Color Scale", ChartColumnType.Magnitude) 
            };
            this.ParameterGroups = new List<ChartScriptParameterGroup>
            {
                new ChartScriptParameterGroup("Form")
                {
                    new ChartScriptParameter("StartDate", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("Sunday|Monday") },
                },
                new ChartScriptParameterGroup("Color Scale")
                {
                    new ChartScriptParameter("ColorScale", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("ZeroMax|MinMax|Sqrt|Log") },
                    new ChartScriptParameter("ColorInterpolate", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("YlGn|YlGnBu|GnBu|BuGn|PuBuGn|PuBu|BuPu|RdPu|PuRd|OrRd|YlOrRd|YlOrBr|Purples|Blues|Greens|Oranges|Reds|Greys|PuOr|BrBG|PRGn|PiYG|RdBu|RdGy|RdYlBu|Spectral|RdYlGn") },
                }
            };
        }      
    }                
}
