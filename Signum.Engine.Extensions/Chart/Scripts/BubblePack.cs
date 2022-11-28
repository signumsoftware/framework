using Signum.Entities.Chart;

namespace Signum.Engine.Chart.Scripts;

public class BubblePackChartScript : ChartScript                
{
    public BubblePackChartScript() : base(D3ChartScript.BubblePack)
    {
        this.Icon = ChartScriptLogic.LoadIcon("bubblepack.png");
        this.Columns = new List<ChartScriptColumn>
        {
            new ChartScriptColumn("Bubble", ChartColumnType.Groupable),
            new ChartScriptColumn("Size", ChartColumnType.Magnitude) ,
            new ChartScriptColumn("Parent", ChartColumnType.Groupable) { IsOptional = true },
            new ChartScriptColumn("Color Scale", ChartColumnType.Magnitude) { IsOptional = true },
            new ChartScriptColumn("Color Category", ChartColumnType.Groupable) { IsOptional = true }
        };
        this.ParameterGroups = new List<ChartScriptParameterGroup>
        {
            new ChartScriptParameterGroup()
            {
                new ChartScriptParameter("Scale", ChartParameterType.Enum) { ColumnIndex = 1, ValueDefinition = EnumValueList.Parse("ZeroMax|MinMax|Log") },
            },
            new ChartScriptParameterGroup("Stroke")
            {
                new ChartScriptParameter("StrokeColor", ChartParameterType.String) {  ValueDefinition = new StringValue("") },
                new ChartScriptParameter("StrokeWidth", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 2m } },
            },
            new ChartScriptParameterGroup("Number")
            {
                new ChartScriptParameter("NumberOpacity", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 0.8m } },
                new ChartScriptParameter("NumberSizeLimit", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 18m } },
                new ChartScriptParameter("NumberColor", ChartParameterType.String) {  ValueDefinition = new StringValue("#fff") }
            },
            new ChartScriptParameterGroup("Opacity")
            {
                new ChartScriptParameter("FillOpacity", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 0.8m } },
            },
            new ChartScriptParameterGroup("Color Scale")
            {
                new ChartScriptParameter("ColorScale", ChartParameterType.Enum) { ColumnIndex = 3, ValueDefinition = EnumValueList.Parse("ZeroMax|MinMax|Sqrt|Log") },
                new ChartScriptParameter("ColorInterpolate", ChartParameterType.Special) {  ColumnIndex = 3, ValueDefinition = new SpecialParameter(SpecialParameterType.ColorInterpolate) },
            },
            new ChartScriptParameterGroup("Color Category")
            {
                new ChartScriptParameter("ColorCategory", ChartParameterType.Special) { ColumnIndex = 4, ValueDefinition = new SpecialParameter(SpecialParameterType.ColorCategory) },
            },
        };
    }      
}                
