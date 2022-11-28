using Signum.Entities.Chart;

namespace Signum.Engine.Chart.Scripts;

public class MultiLinesChartScript : ChartScript                
{
    public MultiLinesChartScript(): base(D3ChartScript.MultiLines)
    {
        this.Icon = ChartScriptLogic.LoadIcon("multilines.png");
        this.Columns = new List<ChartScriptColumn>
        {
            new ChartScriptColumn("Horizontal Axis", ChartColumnType.Any),
            new ChartScriptColumn("Split Lines", ChartColumnType.Groupable) { IsOptional = true },
            new ChartScriptColumn("Height", ChartColumnType.Positionable) ,
            new ChartScriptColumn("Height 2", ChartColumnType.Positionable) { IsOptional = true },
            new ChartScriptColumn("Height 3", ChartColumnType.Positionable) { IsOptional = true },
            new ChartScriptColumn("Height 4", ChartColumnType.Positionable) { IsOptional = true },
            new ChartScriptColumn("Height 5", ChartColumnType.Positionable) { IsOptional = true }
        };
        this.ParameterGroups = new List<ChartScriptParameterGroup>
        {
            new ChartScriptParameterGroup()
            {
                new ChartScriptParameter("CompleteValues", ChartParameterType.Enum) { ColumnIndex = 0,  ValueDefinition = EnumValueList.Parse("Auto|Yes|No|FromFilters") },
                new ChartScriptParameter("HorizontalScale", ChartParameterType.Enum) { ColumnIndex = 0,  ValueDefinition = EnumValueList.Parse("Bands|ZeroMax (M)|MinMax (P)|Log (M)") },
                new ChartScriptParameter("VerticalScale", ChartParameterType.Enum) { ColumnIndex = 2,  ValueDefinition = EnumValueList.Parse("ZeroMax (M)|MinMax|Log (M)") },
            },
            new ChartScriptParameterGroup("Margin")
            {
                new ChartScriptParameter("UnitMargin", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 40m } },
            },
             new ChartScriptParameterGroup("Number")
            {
                new ChartScriptParameter("NumberMinWidth", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 20 } },
                new ChartScriptParameter("NumberOpacity", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 0.8m } },
            },
           new ChartScriptParameterGroup("Circle")
            {
                new ChartScriptParameter("CircleAutoReduce", ChartParameterType.Enum) { ValueDefinition = EnumValueList.Parse("Yes|No") },
                new ChartScriptParameter("CircleRadius", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 5 } },
                new ChartScriptParameter("CircleStroke", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 2 } },
                new ChartScriptParameter("CircleRadiusHover", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 15 } },
            },
            new ChartScriptParameterGroup("Color Category")
            {
                new ChartScriptParameter("ColorCategory", ChartParameterType.Special) {  ValueDefinition = new SpecialParameter(SpecialParameterType.ColorCategory) },
            },
            new ChartScriptParameterGroup("Form")
            {
                new ChartScriptParameter("Interpolate", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("linear|step-before|step-after|cardinal|monotone|basis|bundle") },
            }
        };
    }      
}                
