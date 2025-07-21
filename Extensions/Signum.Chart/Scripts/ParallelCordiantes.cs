namespace Signum.Chart.Scripts;

public class ParallelCoordiantesChartScript : ChartScript
{
    public ParallelCoordiantesChartScript() : base(D3ChartScript.ParallelCoordinates)
    {
        Icon = ChartScriptLogic.LoadIcon("parallelcoordinates.png");
        Columns = new List<ChartScriptColumn>
        {
            new ChartScriptColumn(ChartColumnMessage.Line, ChartColumnType.AnyGroupKey),
            new ChartScriptColumn(ChartColumnMessage.Dimension1, ChartColumnType.AnyNumberDateTime) ,
            new ChartScriptColumn(ChartColumnMessage.Dimension2, ChartColumnType.AnyNumberDateTime) ,
            new ChartScriptColumn(ChartColumnMessage.Dimension3, ChartColumnType.AnyNumberDateTime) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.Dimension4, ChartColumnType.AnyNumberDateTime) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.Dimension5, ChartColumnType.AnyNumberDateTime) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.Dimension6, ChartColumnType.AnyNumberDateTime) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.Dimension7, ChartColumnType.AnyNumberDateTime) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.Dimension8, ChartColumnType.AnyNumberDateTime) { IsOptional = true }
        };
        ParameterGroups = new List<ChartScriptParameterGroup>
        {

            new ChartScriptParameterGroup()
            {
                new ChartScriptParameter(ChartParameter.Scale1, ChartParameterType.Scala) { ColumnIndex = 1,  ValueDefinition = new Scala() },
                new ChartScriptParameter(ChartParameter.Scale2, ChartParameterType.Scala) { ColumnIndex = 2,  ValueDefinition = new Scala() },
                new ChartScriptParameter(ChartParameter.Scale3, ChartParameterType.Scala) { ColumnIndex = 3,  ValueDefinition = new Scala() },
                new ChartScriptParameter(ChartParameter.Scale4, ChartParameterType.Scala) { ColumnIndex = 4,  ValueDefinition = new Scala() },
                new ChartScriptParameter(ChartParameter.Scale5, ChartParameterType.Scala) { ColumnIndex = 5,  ValueDefinition = new Scala() },
                new ChartScriptParameter(ChartParameter.Scale6, ChartParameterType.Scala) { ColumnIndex = 6,  ValueDefinition = new Scala() },
                new ChartScriptParameter(ChartParameter.Scale7, ChartParameterType.Scala) { ColumnIndex = 7,  ValueDefinition = new Scala() },
                new ChartScriptParameter(ChartParameter.Scale8, ChartParameterType.Scala) { ColumnIndex = 8,  ValueDefinition = new Scala() },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Color)
            {
                new ChartScriptParameter(ChartParameter.ColorInterpolate, ChartParameterType.Special) {  ValueDefinition = new SpecialParameter(SpecialParameterType.ColorInterpolate) },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Shape)
            {
                new ChartScriptParameter(ChartParameter.Interpolate, ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("linear|step-before|step-after|cardinal|monotone|basis|bundle") }
            },
        };
    }
}
