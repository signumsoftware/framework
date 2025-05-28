namespace Signum.Chart.Scripts;

public class ScatterplotChartScript : ChartScript
{
    public ScatterplotChartScript() : base(D3ChartScript.Scatterplot)
    {
        Icon = ChartScriptLogic.LoadIcon("points.png");
        Columns = new List<ChartScriptColumn>
        {
            new ChartScriptColumn(ChartColumnMessage.Point, ChartColumnType.Groupable),
            new ChartScriptColumn(ChartColumnMessage.HorizontalAxis, ChartColumnType.Positionable) ,
            new ChartScriptColumn(ChartColumnMessage.VerticalAxis, ChartColumnType.Positionable),
            new ChartScriptColumn(ChartColumnMessage.HorizontalAxis2, ChartColumnType.Positionable) { IsOptional = true } ,
            new ChartScriptColumn(ChartColumnMessage.VerticalAxis2, ChartColumnType.Positionable) { IsOptional = true } ,
            new ChartScriptColumn(ChartColumnMessage.ColorScale, ChartColumnType.Magnitude) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.ColorCategory, ChartColumnType.Groupable) { IsOptional = true }

        };
        ParameterGroups = new List<ChartScriptParameterGroup>
        {
            new ChartScriptParameterGroup()
            {
                new ChartScriptParameter(ChartParameterMessage.HorizontalScale, ChartParameterType.Enum) { ColumnIndex = 1,  ValueDefinition = EnumValueList.Parse("ZeroMax (M)|MinMax|Log (M)") },
                new ChartScriptParameter(ChartParameterMessage.VerticalScale, ChartParameterType.Enum) { ColumnIndex = 2,  ValueDefinition = EnumValueList.Parse("ZeroMax (M)|MinMax|Log (M)") },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Margin)
            {
                new ChartScriptParameter(ChartParameterMessage.UnitMargin, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 40m } },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.Points)
            {
                new ChartScriptParameter(ChartParameterMessage.PointSize, ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 4m } },
                new ChartScriptParameter(ChartParameterMessage.DrawingMode, ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("Svg|Canvas") },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.ColorScale)
            {
                new ChartScriptParameter(ChartParameterMessage.ColorScale, ChartParameterType.Enum) { ColumnIndex = 5,  ValueDefinition = EnumValueList.Parse("ZeroMax|MinMax|Sqrt|Log") },
                new ChartScriptParameter(ChartParameterMessage.ColorInterpolate, ChartParameterType.Special) {  ColumnIndex = 5, ValueDefinition = new SpecialParameter(SpecialParameterType.ColorInterpolate) },
            },
            new ChartScriptParameterGroup(ChartParameterGroupMessage.ColorCategory)
            {
                new ChartScriptParameter(ChartParameterMessage.ColorCategory, ChartParameterType.Special) {  ColumnIndex = 6, ValueDefinition = new SpecialParameter(SpecialParameterType.ColorCategory)  }
            },

        };
    }
}
