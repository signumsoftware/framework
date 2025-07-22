namespace Signum.Chart.Scripts;

public class ScatterplotChartScript : ChartScript
{
    public ScatterplotChartScript() : base(D3ChartScript.Scatterplot)
    {
        Icon = ChartScriptLogic.LoadIcon("points.png");
        Columns = new List<ChartScriptColumn>
        {
            new ChartScriptColumn(ChartColumnMessage.Point, ChartColumnType.AnyGroupKey),
            new ChartScriptColumn(ChartColumnMessage.HorizontalAxis, ChartColumnType.AnyNumberDateTime) ,
            new ChartScriptColumn(ChartColumnMessage.VerticalAxis, ChartColumnType.AnyNumberDateTime),
            new ChartScriptColumn(ChartColumnMessage.HorizontalAxis2, ChartColumnType.AnyNumberDateTime) { IsOptional = true } ,
            new ChartScriptColumn(ChartColumnMessage.VerticalAxis2, ChartColumnType.AnyNumberDateTime) { IsOptional = true } ,
            new ChartScriptColumn(ChartColumnMessage.ColorScale, ChartColumnType.AnyNumber) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.ColorCategory, ChartColumnType.AnyGroupKey) { IsOptional = true }

        };
        ParameterGroups = new List<ChartScriptParameterGroup>
        {
            new ChartScriptParameterGroup()
            {
                new ChartScriptParameter(ChartParameter.HorizontalScale, ChartParameterType.Scala) { ColumnIndex = 1,  ValueDefinition = new Scala() },
                new ChartScriptParameter(ChartParameter.VerticalScale, ChartParameterType.Scala) { ColumnIndex = 2,  ValueDefinition = new Scala() },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Margin)
            {
                new ChartScriptParameter(ChartParameter.UnitMargin, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 40m } },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Points)
            {
                new ChartScriptParameter(ChartParameter.PointSize, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 4m } },
                new ChartScriptParameter(ChartParameter.DrawingMode, ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("Svg|Canvas") },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.ColorScale)
            {
                new ChartScriptParameter(ChartParameter.ColorScale, ChartParameterType.Scala) { ColumnIndex = 5,  ValueDefinition = new Scala() },
                new ChartScriptParameter(ChartParameter.ColorInterpolate, ChartParameterType.Special) {  ColumnIndex = 5, ValueDefinition = new SpecialParameter(SpecialParameterType.ColorInterpolate) },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.ColorCategory)
            {
                new ChartScriptParameter(ChartParameter.ColorCategory, ChartParameterType.Special) {  ColumnIndex = 6, ValueDefinition = new SpecialParameter(SpecialParameterType.ColorCategory)  }
            },

        };
    }
}
