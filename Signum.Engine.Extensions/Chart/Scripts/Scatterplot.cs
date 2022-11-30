using Signum.Entities.Chart;

namespace Signum.Engine.Chart.Scripts;

public class ScatterplotChartScript : ChartScript                
{
    public ScatterplotChartScript() : base(D3ChartScript.Scatterplot)
    {
        this.Icon = ChartScriptLogic.LoadIcon("points.png");
        this.Columns = new List<ChartScriptColumn>
        {
            new ChartScriptColumn("Point Color", ChartColumnType.Groupable),
            new ChartScriptColumn("Horizontal Axis", ChartColumnType.Positionable) ,
            new ChartScriptColumn("Vertical Axis", ChartColumnType.Positionable), 
            new ChartScriptColumn("Horizontal Axis (2)", ChartColumnType.Positionable) { IsOptional = true } ,
            new ChartScriptColumn("Vertical Axis (2)", ChartColumnType.Positionable) { IsOptional = true } ,
        };
        this.ParameterGroups = new List<ChartScriptParameterGroup>
        {
            new ChartScriptParameterGroup()
            {
                new ChartScriptParameter("HorizontalScale", ChartParameterType.Enum) { ColumnIndex = 1,  ValueDefinition = EnumValueList.Parse("ZeroMax (M)|MinMax|Log (M)") },
                new ChartScriptParameter("VerticalScale", ChartParameterType.Enum) { ColumnIndex = 2,  ValueDefinition = EnumValueList.Parse("ZeroMax (M)|MinMax|Log (M)") },
            },
            new ChartScriptParameterGroup("Margin")
            {
                new ChartScriptParameter("UnitMargin", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 40m } },
            },
            new ChartScriptParameterGroup("Points")
            {
                new ChartScriptParameter("PointSize", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 4m } },
                new ChartScriptParameter("DrawingMode", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("Svg|Canvas") },
            },
            new ChartScriptParameterGroup("Color Scale")
            {
                new ChartScriptParameter("ColorScale", ChartParameterType.Enum) { ColumnIndex = 0,  ValueDefinition = EnumValueList.Parse("Ordinal|ZeroMax|MinMax|Sqrt|Log") },
                new ChartScriptParameter("ColorInterpolate", ChartParameterType.Special) {  ColumnIndex = 0, ValueDefinition = new SpecialParameter(SpecialParameterType.ColorInterpolate) },
            },
            new ChartScriptParameterGroup("Color Category")
            { 
                new ChartScriptParameter("ColorCategory", ChartParameterType.Special) {  ColumnIndex = 0, ValueDefinition = new SpecialParameter(SpecialParameterType.ColorCategory)  }
            },
            
        };
    }      
}                
