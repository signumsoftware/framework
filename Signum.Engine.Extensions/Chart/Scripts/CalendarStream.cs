using Signum.Entities.Chart;

namespace Signum.Engine.Chart.Scripts;

public class CalendarStreamChartScript : ChartScript                
{
    public CalendarStreamChartScript() : base(D3ChartScript.CalendarStream)
    {
        this.Icon = ChartScriptLogic.LoadIcon("calendar.png");
        this.Columns = new List<ChartScriptColumn>
        {
            new ChartScriptColumn("Date", ChartColumnType.DateOnly),
            new ChartScriptColumn("Color Scale", ChartColumnType.Magnitude) 
        };
        this.ParameterGroups = new List<ChartScriptParameterGroup>
        {
            new ChartScriptParameterGroup()
            {
                new ChartScriptParameter("StartDate", ChartParameterType.Enum) { ColumnIndex = 0, ValueDefinition = EnumValueList.Parse("Monday|Sunday") },
                new ChartScriptParameter("ColorScale", ChartParameterType.Enum) { ColumnIndex = 1, ValueDefinition = EnumValueList.Parse("ZeroMax|MinMax|Sqrt|Log") },
                new ChartScriptParameter("ColorInterpolate", ChartParameterType.Special) { ColumnIndex = 1, ValueDefinition = new SpecialParameter(SpecialParameterType.ColorInterpolate) },
            }
        };
    }      
}                
