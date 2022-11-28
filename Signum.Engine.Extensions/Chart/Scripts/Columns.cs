using Signum.Entities.Chart;

namespace Signum.Engine.Chart.Scripts;

public class ColumnsChartScript : ChartScript                
{
    public ColumnsChartScript() : base(D3ChartScript.Columns)
    {
        this.Icon = ChartScriptLogic.LoadIcon("columns.png");
        this.Columns = new List<ChartScriptColumn>
        {
            new ChartScriptColumn("Columns", ChartColumnType.Groupable),
            new ChartScriptColumn("Height", ChartColumnType.Positionable) 
        };
        this.ParameterGroups = new List<ChartScriptParameterGroup>
        {
            new ChartScriptParameterGroup()
            {
                new ChartScriptParameter("CompleteValues", ChartParameterType.Enum) { ColumnIndex = 0,  ValueDefinition = EnumValueList.Parse("Auto|Yes|No|FromFilters") },
                new ChartScriptParameter("Scale", ChartParameterType.Enum) { ColumnIndex = 1,  ValueDefinition = EnumValueList.Parse("ZeroMax (M)|MinMax|Log (M)") },
            },
            new ChartScriptParameterGroup("Margins")
            {
                new ChartScriptParameter("UnitMargin", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 40m } },
                new ChartScriptParameter("Labels", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("Inside|InsideAll|Margin|MarginAll|None") },
                new ChartScriptParameter("LabelsMargin", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 100m } },
            },
            new ChartScriptParameterGroup("Number")
            {
                new ChartScriptParameter("NumberOpacity", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 0.8m } },
                new ChartScriptParameter("NumberColor", ChartParameterType.String) {  ValueDefinition = new StringValue("#fff") },
            },
            new ChartScriptParameterGroup("Color")
            {
                new ChartScriptParameter("ColorCategory", ChartParameterType.Special) {  ValueDefinition = new SpecialParameter(SpecialParameterType.ColorCategory) },
                new ChartScriptParameter("ForceColor", ChartParameterType.String) {  ValueDefinition = new StringValue("") },
            }
        };
    }      
}                
