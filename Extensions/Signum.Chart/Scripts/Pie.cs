namespace Signum.Chart.Scripts;

public class PieChartScript : ChartScript
{
    public PieChartScript() : base(D3ChartScript.Pie)
    {
        Icon = ChartScriptLogic.LoadIcon("doughnut.png");
        Columns = new List<ChartScriptColumn>
        {
            new ChartScriptColumn("Sections", ChartColumnType.Groupable),
            new ChartScriptColumn("Angle", ChartColumnType.Magnitude)
        };
        ParameterGroups = new List<ChartScriptParameterGroup>
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
                new ChartScriptParameter("ColorCategory", ChartParameterType.Special) {  ValueDefinition = new SpecialParameter(SpecialParameterType.ColorCategory) },
           },
            new ChartScriptParameterGroup("ShowPercent")
            {
                new ChartScriptParameter("ValueAsPercent", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("No|Yes") },
            },

        };
    }
}
