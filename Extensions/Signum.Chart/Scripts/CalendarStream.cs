namespace Signum.Chart.Scripts;

public class CalendarStreamChartScript : ChartScript
{
    public CalendarStreamChartScript() : base(D3ChartScript.CalendarStream)
    {
        Icon = ChartScriptLogic.LoadIcon("calendar.png");
        Columns = new List<ChartScriptColumn>
        {
            new ChartScriptColumn("Date", ChartColumnType.DateOnly),
            new ChartScriptColumn("Color Scale", ChartColumnType.Magnitude)
        };
        ParameterGroups = new List<ChartScriptParameterGroup>
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
