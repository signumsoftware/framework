using Signum.Entities.Chart;

namespace Signum.Engine.Chart.Scripts;

public class StackedBarsChartScript : ChartScript                
{
    public StackedBarsChartScript() : base(D3ChartScript.StackedBars)
    {
        this.Icon = ChartScriptLogic.LoadIcon("stackedbars.png");
        this.Columns = new List<ChartScriptColumn>
        {
            new ChartScriptColumn("Vertical Axis", ChartColumnType.Groupable),
            new ChartScriptColumn("Split Bars", ChartColumnType.Groupable) { IsOptional = true },
            new ChartScriptColumn("Width", ChartColumnType.Magnitude) ,
            new ChartScriptColumn("Width 2", ChartColumnType.Magnitude) { IsOptional = true },
            new ChartScriptColumn("Width 3", ChartColumnType.Magnitude) { IsOptional = true },
            new ChartScriptColumn("Width 4", ChartColumnType.Magnitude) { IsOptional = true },
            new ChartScriptColumn("Width 5", ChartColumnType.Magnitude) { IsOptional = true }
        };
        this.ParameterGroups = new List<ChartScriptParameterGroup>
        {
            new ChartScriptParameterGroup("Scale")
            {
                new ChartScriptParameter("CompleteValues", ChartParameterType.Enum) { ColumnIndex = 0,  ValueDefinition = EnumValueList.Parse("Auto|Yes|No|FromFilters") },
            },
            new ChartScriptParameterGroup("Margin")
            {
                new ChartScriptParameter("Labels", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("Margin|Inside|None") },
                new ChartScriptParameter("LabelsMargin", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 100m } },
            },
            new ChartScriptParameterGroup("Number")
            { 
                new ChartScriptParameter("NumberOpacity", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 0.8m } },
                new ChartScriptParameter("NumberColor", ChartParameterType.String) {  ValueDefinition = new StringValue("#fff") },
            },
            new ChartScriptParameterGroup("Color Category")
            {
                new ChartScriptParameter("ColorCategory", ChartParameterType.Special) {  ValueDefinition = new SpecialParameter(SpecialParameterType.ColorCategory) },
            },
            new ChartScriptParameterGroup("Form")
            {
                new ChartScriptParameter("Stack", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("zero|expand|wiggle|silhouette") },
                new ChartScriptParameter("Order", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("none|ascending|descending|insideOut|reverse") },
            },
            new ChartScriptParameterGroup("ShowPercent")
            {
                new ChartScriptParameter("ValueAsPercent", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("No|Yes") },
            },
        };
    }      
}                
