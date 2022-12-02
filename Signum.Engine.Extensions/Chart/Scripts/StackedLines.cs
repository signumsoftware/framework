using Signum.Entities.Chart;

namespace Signum.Engine.Chart.Scripts;

public class StackedLinesChartScript : ChartScript                
{
    public StackedLinesChartScript() : base(D3ChartScript.StackedLines)
    {
        this.Icon = ChartScriptLogic.LoadIcon("stackedareas.png");
        this.Columns = new List<ChartScriptColumn>
        {
            new ChartScriptColumn("Horizontal Axis", ChartColumnType.Any),
            new ChartScriptColumn("Areas", ChartColumnType.Groupable) { IsOptional = true },
            new ChartScriptColumn("Height", ChartColumnType.Magnitude) ,
            new ChartScriptColumn("Height 2", ChartColumnType.Magnitude) { IsOptional = true },
            new ChartScriptColumn("Height 3", ChartColumnType.Magnitude) { IsOptional = true },
            new ChartScriptColumn("Height 4", ChartColumnType.Magnitude) { IsOptional = true },
            new ChartScriptColumn("Height 5", ChartColumnType.Magnitude) { IsOptional = true }
        };
        this.ParameterGroups = new List<ChartScriptParameterGroup>
        {
            new ChartScriptParameterGroup()
            {
                new ChartScriptParameter("CompleteValues", ChartParameterType.Enum) { ColumnIndex = 0,  ValueDefinition = EnumValueList.Parse("Auto|Yes|No|FromFilters") },
                new ChartScriptParameter("HorizontalScale", ChartParameterType.Enum) { ColumnIndex = 0,  ValueDefinition = EnumValueList.Parse("Bands|ZeroMax (M)|MinMax (P)|Log (M)") },
            },
            new ChartScriptParameterGroup("Margin")
            {
                new ChartScriptParameter("Horizontal Margin", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 20m } },
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
                new ChartScriptParameter("Order", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("none|ascending|descending|insideOut|reverse") },
                new ChartScriptParameter("Stack", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("zero|expand|wiggle|silhouette") },
                new ChartScriptParameter("Interpolate", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("linear|step-before|step-after|cardinal|monotone|basis") },
            },
            new ChartScriptParameterGroup("ShowPercent")
            {
                new ChartScriptParameter("ValueAsPercent", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("No|Yes") },
            },
        };
    }      
}                
