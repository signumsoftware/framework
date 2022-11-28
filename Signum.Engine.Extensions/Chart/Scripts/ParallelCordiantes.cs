using Signum.Entities.Chart;

namespace Signum.Engine.Chart.Scripts;

public class ParallelCoordiantesChartScript : ChartScript                
{
    public ParallelCoordiantesChartScript(): base(D3ChartScript.ParallelCoordinates)
    {
        this.Icon = ChartScriptLogic.LoadIcon("parallelcoordinates.png");
        this.Columns = new List<ChartScriptColumn>
        {
            new ChartScriptColumn("Line", ChartColumnType.Groupable),
            new ChartScriptColumn("Dimension1", ChartColumnType.Positionable) ,
            new ChartScriptColumn("Dimension2", ChartColumnType.Positionable) ,
            new ChartScriptColumn("Dimension3", ChartColumnType.Positionable) { IsOptional = true },
            new ChartScriptColumn("Dimension4", ChartColumnType.Positionable) { IsOptional = true },
            new ChartScriptColumn("Dimension5", ChartColumnType.Positionable) { IsOptional = true },
            new ChartScriptColumn("Dimension6", ChartColumnType.Positionable) { IsOptional = true },
            new ChartScriptColumn("Dimension7", ChartColumnType.Positionable) { IsOptional = true },
            new ChartScriptColumn("Dimension8", ChartColumnType.Positionable) { IsOptional = true }
        };
        this.ParameterGroups = new List<ChartScriptParameterGroup>
        {

            new ChartScriptParameterGroup()
            {
                new ChartScriptParameter("Scale1", ChartParameterType.Enum) { ColumnIndex = 1,  ValueDefinition = EnumValueList.Parse("ZeroMax (M)|MinMax|Log (M)|Sqrt (M)") },
                new ChartScriptParameter("Scale2", ChartParameterType.Enum) { ColumnIndex = 2,  ValueDefinition = EnumValueList.Parse("ZeroMax (M)|MinMax|Log (M)|Sqrt (M)") },
                new ChartScriptParameter("Scale3", ChartParameterType.Enum) { ColumnIndex = 3,  ValueDefinition = EnumValueList.Parse("ZeroMax (M)|MinMax|Log (M)|Sqrt (M)") },
                new ChartScriptParameter("Scale4", ChartParameterType.Enum) { ColumnIndex = 4,  ValueDefinition = EnumValueList.Parse("ZeroMax (M)|MinMax|Log (M)|Sqrt (M)") },
                new ChartScriptParameter("Scale5", ChartParameterType.Enum) { ColumnIndex = 5,  ValueDefinition = EnumValueList.Parse("ZeroMax (M)|MinMax|Log (M)|Sqrt (M)") },
                new ChartScriptParameter("Scale6", ChartParameterType.Enum) { ColumnIndex = 6,  ValueDefinition = EnumValueList.Parse("ZeroMax (M)|MinMax|Log (M)|Sqrt (M)") },
                new ChartScriptParameter("Scale7", ChartParameterType.Enum) { ColumnIndex = 7,  ValueDefinition = EnumValueList.Parse("ZeroMax (M)|MinMax|Log (M)|Sqrt (M)") },
                new ChartScriptParameter("Scale8", ChartParameterType.Enum) { ColumnIndex = 8,  ValueDefinition = EnumValueList.Parse("ZeroMax (M)|MinMax|Log (M)|Sqrt (M)") },
            },
            new ChartScriptParameterGroup("Color")
            {
                new ChartScriptParameter("ColorInterpolate", ChartParameterType.Special) {  ValueDefinition = new SpecialParameter(SpecialParameterType.ColorInterpolate) },
            },
            new ChartScriptParameterGroup("Form")
            {
                new ChartScriptParameter("Interpolate", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("linear|step-before|step-after|cardinal|monotone|basis|bundle") }
            },
        };
    }      
}                
