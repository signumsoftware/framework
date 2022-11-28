using Signum.Entities.Chart;

namespace Signum.Engine.Chart.Scripts;

public class StackedColumnsChartScript : ChartScript                
{
    public StackedColumnsChartScript() : base(D3ChartScript.StackedColumns)
    {
        this.Icon = ChartScriptLogic.LoadIcon("stackedcolumns.png");
        this.Columns = new List<ChartScriptColumn>
        {
            new ChartScriptColumn("Horizontal Axis", ChartColumnType.Groupable),
            new ChartScriptColumn("Split Columns", ChartColumnType.Groupable) { IsOptional = true },
            new ChartScriptColumn("Height", ChartColumnType.Magnitude) ,
            new ChartScriptColumn("Height 2", ChartColumnType.Magnitude) { IsOptional = true },
            new ChartScriptColumn("Height 3", ChartColumnType.Magnitude) { IsOptional = true },
            new ChartScriptColumn("Height 4", ChartColumnType.Magnitude) { IsOptional = true },
            new ChartScriptColumn("Height 5", ChartColumnType.Magnitude) { IsOptional = true }
        };
        this.ParameterGroups = new List<ChartScriptParameterGroup>
        {
            new ChartScriptParameterGroup("Scale")
            {
                new ChartScriptParameter("CompleteValues", ChartParameterType.Enum) { ColumnIndex = 0,  ValueDefinition = EnumValueList.Parse("Auto|Yes|No|FromFilters") },
            },
            new ChartScriptParameterGroup("Margin")
            {
                new ChartScriptParameter("UnitMargin", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 40m } },
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
