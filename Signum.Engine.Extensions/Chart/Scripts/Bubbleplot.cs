using Signum.Entities.Chart;

namespace Signum.Engine.Chart.Scripts;

public class BubbleplotChartScript : ChartScript                
{
    public BubbleplotChartScript() : base(D3ChartScript.Bubbleplot)
    {
        this.Icon = ChartScriptLogic.LoadIcon("bubbles.png");
        this.Columns = new List<ChartScriptColumn>
        {
            new ChartScriptColumn("Color", ChartColumnType.Groupable),
            new ChartScriptColumn("Horizontal Axis", ChartColumnType.Positionable) ,
            new ChartScriptColumn("Vertical Axis", ChartColumnType.Positionable) ,
            new ChartScriptColumn("Size", ChartColumnType.Magnitude) 
        };
        this.ParameterGroups = new List<ChartScriptParameterGroup>
        {
            new ChartScriptParameterGroup()
            {
                new ChartScriptParameter("HorizontalScale", ChartParameterType.Enum) { ColumnIndex = 1,  ValueDefinition = EnumValueList.Parse("ZeroMax (M)|MinMax|Log (M)") },
                new ChartScriptParameter("VerticalScale", ChartParameterType.Enum) { ColumnIndex = 2,  ValueDefinition = EnumValueList.Parse("ZeroMax (M)|MinMax|Log (M)") },
                new ChartScriptParameter("SizeScale", ChartParameterType.Enum) { ColumnIndex = 3, ValueDefinition = EnumValueList.Parse("ZeroMax|MinMax|Log") },
            },
            new ChartScriptParameterGroup("Margin")
            {
                new ChartScriptParameter("UnitMargin", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 40m } },
                new ChartScriptParameter("TopMargin", ChartParameterType.String) {  ValueDefinition = new StringValue("0.15*") },
                new ChartScriptParameter("RightMargin", ChartParameterType.String) {  ValueDefinition = new StringValue("0.15*") },
            },
            new ChartScriptParameterGroup("Label")
            {
                new ChartScriptParameter("ShowLabel", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("Yes|No") },
                new ChartScriptParameter("LabelColor", ChartParameterType.String) {  ValueDefinition = new StringValue("#fff") },
                new ChartScriptParameter("FillOpacity", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 0.4m } },
            },
            new ChartScriptParameterGroup("Color Scale")
            {
                new ChartScriptParameter("ColorScale", ChartParameterType.Enum) { ColumnIndex = 0,  ValueDefinition = EnumValueList.Parse("Ordinal|ZeroMax|MinMax|Sqrt|Log") },
                new ChartScriptParameter("ColorInterpolate", ChartParameterType.Special) {  ColumnIndex = 0, ValueDefinition = new SpecialParameter(SpecialParameterType.ColorInterpolate) },
            },
            new ChartScriptParameterGroup("Color Category")
            {
                new ChartScriptParameter("ColorCategory", ChartParameterType.Special) { ColumnIndex = 0, ValueDefinition = new SpecialParameter(SpecialParameterType.ColorCategory) },
            }
        };
    }      
}                
