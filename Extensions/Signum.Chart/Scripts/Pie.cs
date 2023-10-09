namespace Signum.Chart.Scripts;

public class PieChartScript : ChartScript
{
    public PieChartScript() : base(D3ChartScript.Pie)
    {
        Icon = ChartScriptLogic.LoadIcon("doughnut.png");
        Columns = new List<ChartScriptColumn>
        {
            new ChartScriptColumn(ChartColumnMessage.Sections, ChartColumnType.Groupable),
            new ChartScriptColumn(ChartColumnMessage.Angle, ChartColumnType.Magnitude)
        };
        ParameterGroups = new List<ChartScriptParameterGroup>
        {
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Shape)
            {
                new ChartScriptParameter(ChartParameterMessage.InnerRadious, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 0m } },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Arrange)
            {
                new ChartScriptParameter(ChartParameterMessage.Sort, ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("No|Ascending|Descending") },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.ColorCategory)
            {
                new ChartScriptParameter(ChartParameterMessage.ColorCategory, ChartParameterType.Special) {  ValueDefinition = new SpecialParameter(SpecialParameterType.ColorCategory) },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.ShowValue)
            {
                new ChartScriptParameter(ChartParameterMessage.ValueAsNumberOrPercent, ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("No|Number|Percent") },
            }
        };
    }
}
