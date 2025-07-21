namespace Signum.Chart.Scripts;

public class CalendarStreamChartScript : ChartScript
{
    public CalendarStreamChartScript() : base(D3ChartScript.CalendarStream)
    {
        Icon = ChartScriptLogic.LoadIcon("calendar.png");
        Columns = new List<ChartScriptColumn>
        {
            new ChartScriptColumn(ChartColumnMessage.Date, ChartColumnType.Date),
            new ChartScriptColumn(ChartColumnMessage.ColorScale, ChartColumnType.AnyNumber)
        };
        ParameterGroups = new List<ChartScriptParameterGroup>
        {
            new ChartScriptParameterGroup()
            {
                new ChartScriptParameter(ChartParameter.StartDate, ChartParameterType.Enum) { ColumnIndex = 0, ValueDefinition = EnumValueList.Parse("Monday|Sunday") },
                new ChartScriptParameter(ChartParameter.ColorScale, ChartParameterType.Scala) { ColumnIndex = 1, ValueDefinition = new Scala() },
                new ChartScriptParameter(ChartParameter.ColorInterpolate, ChartParameterType.Special) { ColumnIndex = 1, ValueDefinition = new SpecialParameter(SpecialParameterType.ColorInterpolate) },
            }
        };
    }
}
