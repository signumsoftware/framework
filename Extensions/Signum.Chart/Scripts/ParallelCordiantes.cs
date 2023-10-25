namespace Signum.Chart.Scripts;

public class ParallelCoordiantesChartScript : ChartScript
{
    public ParallelCoordiantesChartScript() : base(D3ChartScript.ParallelCoordinates)
    {
        Icon = ChartScriptLogic.LoadIcon("parallelcoordinates.png");
        Columns = new List<ChartScriptColumn>
        {
            new ChartScriptColumn(ChartColumnMessage.Line, ChartColumnType.Groupable),
            new ChartScriptColumn(ChartColumnMessage.Dimension1, ChartColumnType.Positionable) ,
            new ChartScriptColumn(ChartColumnMessage.Dimension2, ChartColumnType.Positionable) ,
            new ChartScriptColumn(ChartColumnMessage.Dimension3, ChartColumnType.Positionable) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.Dimension4, ChartColumnType.Positionable) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.Dimension5, ChartColumnType.Positionable) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.Dimension6, ChartColumnType.Positionable) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.Dimension7, ChartColumnType.Positionable) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.Dimension8, ChartColumnType.Positionable) { IsOptional = true }
        };
        ParameterGroups = new List<ChartScriptParameterGroup>
        {

            new ChartScriptParameterGroup()
            {
                new ChartScriptParameter(ChartParameterMessage.Scale1, ChartParameterType.Enum) { ColumnIndex = 1,  ValueDefinition = EnumValueList.Parse("ZeroMax (M)|MinMax|Log (M)|Sqrt (M)") },
                new ChartScriptParameter(ChartParameterMessage.Scale2, ChartParameterType.Enum) { ColumnIndex = 2,  ValueDefinition = EnumValueList.Parse("ZeroMax (M)|MinMax|Log (M)|Sqrt (M)") },
                new ChartScriptParameter(ChartParameterMessage.Scale3, ChartParameterType.Enum) { ColumnIndex = 3,  ValueDefinition = EnumValueList.Parse("ZeroMax (M)|MinMax|Log (M)|Sqrt (M)") },
                new ChartScriptParameter(ChartParameterMessage.Scale4, ChartParameterType.Enum) { ColumnIndex = 4,  ValueDefinition = EnumValueList.Parse("ZeroMax (M)|MinMax|Log (M)|Sqrt (M)") },
                new ChartScriptParameter(ChartParameterMessage.Scale5, ChartParameterType.Enum) { ColumnIndex = 5,  ValueDefinition = EnumValueList.Parse("ZeroMax (M)|MinMax|Log (M)|Sqrt (M)") },
                new ChartScriptParameter(ChartParameterMessage.Scale6, ChartParameterType.Enum) { ColumnIndex = 6,  ValueDefinition = EnumValueList.Parse("ZeroMax (M)|MinMax|Log (M)|Sqrt (M)") },
                new ChartScriptParameter(ChartParameterMessage.Scale7, ChartParameterType.Enum) { ColumnIndex = 7,  ValueDefinition = EnumValueList.Parse("ZeroMax (M)|MinMax|Log (M)|Sqrt (M)") },
                new ChartScriptParameter(ChartParameterMessage.Scale8, ChartParameterType.Enum) { ColumnIndex = 8,  ValueDefinition = EnumValueList.Parse("ZeroMax (M)|MinMax|Log (M)|Sqrt (M)") },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Color)
            {
                new ChartScriptParameter(ChartParameterMessage.ColorInterpolate, ChartParameterType.Special) {  ValueDefinition = new SpecialParameter(SpecialParameterType.ColorInterpolate) },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Shape)
            {
                new ChartScriptParameter(ChartParameterMessage.Interpolate, ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("linear|step-before|step-after|cardinal|monotone|basis|bundle") }
            },
        };
    }
}
