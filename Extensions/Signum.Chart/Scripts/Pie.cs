namespace Signum.Chart.Scripts;

public class PieChartScript : ChartScript
{
    public PieChartScript() : base(D3ChartScript.Pie)
    {
        Icon = ChartScriptLogic.LoadIcon("doughnut.png");
        Columns = new List<ChartScriptColumn>
        {
            new ChartScriptColumn(ChartColumnMessage.Sections, ChartColumnType.AnyGroupKey),
            new ChartScriptColumn(ChartColumnMessage.Angle, ChartColumnType.AnyNumber)
        };
        ParameterGroups = new List<ChartScriptParameterGroup>
        {
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Shape)
            {
                new ChartScriptParameter(ChartParameter.InnerRadious, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 0m } },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Arrange)
            {
                new ChartScriptParameter(ChartParameter.Sort, ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("No|Ascending|Descending") },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.ColorCategory)
            {
                new ChartScriptParameter(ChartParameter.ColorCategory, ChartParameterType.Special) {  ValueDefinition = new SpecialParameter(SpecialParameterType.ColorCategory) },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.ShowValue)
            {
                new ChartScriptParameter(ChartParameter.Value, ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("No|OnLabel|OnArc") },
                new ChartScriptParameter(ChartParameter.Percent, ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("No|OnLabel|OnArc") },
                new ChartScriptParameter(ChartParameter.Total, ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("No|Yes") },
            }
        };
    }
}
