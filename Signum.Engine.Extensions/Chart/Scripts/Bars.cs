using Signum.Entities.Chart;

namespace Signum.Engine.Chart.Scripts;

public class BarsChartScript : ChartScript                
{
    public BarsChartScript(): base(D3ChartScript.Bars)
    {
        this.Icon = ChartScriptLogic.LoadIcon("bars.png");
        this.Columns = new List<ChartScriptColumn>
        {
            new ChartScriptColumn("Bars", ChartColumnType.Groupable),
            new ChartScriptColumn("Width", ChartColumnType.Positionable) 
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
                new ChartScriptParameter("Labels", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("Inside|InsideAll|Margin|MarginAll|None") },
                new ChartScriptParameter("LabelsMargin", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 100m } },
            },
            new ChartScriptParameterGroup("Numbers")
            {
                new ChartScriptParameter("NumberOpacity", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 0.8m } },
                new ChartScriptParameter("NumberColor", ChartParameterType.String) {  ValueDefinition = new StringValue("#fff") },
            },
            new ChartScriptParameterGroup("Color Category")
            {
                new ChartScriptParameter("ColorCategory", ChartParameterType.Special) {  ValueDefinition = new SpecialParameter(SpecialParameterType.ColorCategory) },
            }
        };
    }      
}                
