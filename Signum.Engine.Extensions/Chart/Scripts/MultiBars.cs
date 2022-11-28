using Signum.Entities.Chart;

namespace Signum.Engine.Chart.Scripts;

public class MultiBarsChartScript : ChartScript                
{
    public MultiBarsChartScript() : base(D3ChartScript.MultiBars)
    {
        this.Icon = ChartScriptLogic.LoadIcon("multibars.png");
        this.Columns = new List<ChartScriptColumn>
        {
            new ChartScriptColumn("Vertical Axis", ChartColumnType.Groupable),
            new ChartScriptColumn("Split Bars", ChartColumnType.Groupable) { IsOptional = true },
            new ChartScriptColumn("Width", ChartColumnType.Positionable) ,
            new ChartScriptColumn("Width 2", ChartColumnType.Positionable) { IsOptional = true },
            new ChartScriptColumn("Width 3", ChartColumnType.Positionable) { IsOptional = true },
            new ChartScriptColumn("Width 4", ChartColumnType.Positionable) { IsOptional = true },
            new ChartScriptColumn("Width 5", ChartColumnType.Positionable) { IsOptional = true }
        };
        this.ParameterGroups = new List<ChartScriptParameterGroup>
        {
            new ChartScriptParameterGroup()
            {
                new ChartScriptParameter("CompleteValues", ChartParameterType.Enum) { ColumnIndex = 0,  ValueDefinition = EnumValueList.Parse("Auto|Yes|No|FromFilters") },
                new ChartScriptParameter("Scale", ChartParameterType.Enum) { ColumnIndex = 2,  ValueDefinition = EnumValueList.Parse("ZeroMax (M)|MinMax|Log (M)") },
            },
            new ChartScriptParameterGroup("Margin")
            {
                new ChartScriptParameter("LabelMargin", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 140m } },
            },
            new ChartScriptParameterGroup("Number")
            {
                new ChartScriptParameter("NumberOpacity", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 0.8m } },
                new ChartScriptParameter("NumberColor", ChartParameterType.String) {  ValueDefinition = new StringValue("#fff") },
            },
            new ChartScriptParameterGroup("Color")
            {
                new ChartScriptParameter("ColorCategory", ChartParameterType.Special) {  ValueDefinition = new SpecialParameter(SpecialParameterType.ColorCategory) },
            }
        };
    }      
}                
