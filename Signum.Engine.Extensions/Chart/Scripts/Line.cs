using Signum.Entities.Chart;

namespace Signum.Engine.Chart.Scripts;

public class LineChartScript : ChartScript                
{
    public LineChartScript() : base(D3ChartScript.Line)
    {
        this.Icon = ChartScriptLogic.LoadIcon("lines.png");
        this.Columns = new List<ChartScriptColumn>
        {
            new ChartScriptColumn("Horizontal Axis", ChartColumnType.Any),
            new ChartScriptColumn("Height", ChartColumnType.Positionable) 
        };
        this.ParameterGroups = new List<ChartScriptParameterGroup>
        {
            new ChartScriptParameterGroup()
            {
                new ChartScriptParameter("CompleteValues", ChartParameterType.Enum) { ColumnIndex = 0,  ValueDefinition = EnumValueList.Parse("Auto|Yes|No|FromFilters") },
                new ChartScriptParameter("HorizontalScale", ChartParameterType.Enum) { ColumnIndex = 0,  ValueDefinition = EnumValueList.Parse("Bands|ZeroMax (M)|MinMax (P)|Log (M)") },
                new ChartScriptParameter("VerticalScale", ChartParameterType.Enum) { ColumnIndex = 1,  ValueDefinition = EnumValueList.Parse("ZeroMax (M)|MinMax|Log (M)") },
            },
            new ChartScriptParameterGroup("Margins")
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
            new ChartScriptParameterGroup("Color")
            {
                new ChartScriptParameter("Color", ChartParameterType.String) {  ValueDefinition = new StringValue("steelblue") },
            },
            new ChartScriptParameterGroup("Form")
            {
                new ChartScriptParameter("Interpolate", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("linear|step-before|step-after|cardinal|monotone|basis|bundle|catmull-rom") },
            } 
        };
    }      
}                
