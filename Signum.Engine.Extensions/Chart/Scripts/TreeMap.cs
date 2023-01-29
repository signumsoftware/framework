using Signum.Entities.Chart;

namespace Signum.Engine.Chart.Scripts;

public class TreeMapChartScript : ChartScript                
{
    public TreeMapChartScript() : base(D3ChartScript.Treemap)
    {
        this.Icon = ChartScriptLogic.LoadIcon("treemap.png");
        this.Columns = new List<ChartScriptColumn>
        {
            new ChartScriptColumn("Square", ChartColumnType.Groupable),
            new ChartScriptColumn("Size", ChartColumnType.Magnitude) ,
            new ChartScriptColumn("Parent", ChartColumnType.Groupable) { IsOptional = true },
            new ChartScriptColumn("Color Scale", ChartColumnType.Magnitude) { IsOptional = true },
            new ChartScriptColumn("Color Category", ChartColumnType.Groupable) { IsOptional = true }
        };
        this.ParameterGroups = new List<ChartScriptParameterGroup>
        {
            new ChartScriptParameterGroup()
            {
                new ChartScriptParameter("Scale", ChartParameterType.Enum) { ColumnIndex = 0, ValueDefinition = EnumValueList.Parse("ZeroMax|MinMax|Log") },
            },
            new ChartScriptParameterGroup("Margin")
            { 
                new ChartScriptParameter("Padding", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 4m } },
                new ChartScriptParameter("Opacity", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 0.5m } },
            },
            new ChartScriptParameterGroup("Number")
            {
                new ChartScriptParameter("NumberOpacity", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 0.8m } },
                new ChartScriptParameter("NumberColor", ChartParameterType.String) {  ValueDefinition = new StringValue("#fff") },
            },
            new ChartScriptParameterGroup("Color Scale")
            {
                new ChartScriptParameter("ColorScale", ChartParameterType.Enum) { ColumnIndex = 3, ValueDefinition = EnumValueList.Parse("ZeroMax|MinMax|Sqrt|Log") },
                new ChartScriptParameter("ColorInterpolate", ChartParameterType.Special) { ColumnIndex = 3, ValueDefinition = new SpecialParameter(SpecialParameterType.ColorInterpolate) },
            },
            new ChartScriptParameterGroup("Color Category")
            {
                new ChartScriptParameter("ColorCategory", ChartParameterType.Special) { ColumnIndex = 4, ValueDefinition = new SpecialParameter(SpecialParameterType.ColorCategory) },
            }
        };
    }      
}                
