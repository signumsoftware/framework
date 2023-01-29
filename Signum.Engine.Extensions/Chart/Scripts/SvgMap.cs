using Signum.Entities.Chart;

namespace Signum.Engine.Chart.Scripts;

public class SvgMapScript : ChartScript                
{
    public SvgMapScript(string[] svgMaps) : base(SvgMapsChartScript.SvgMap)
    {
        this.Icon = ChartScriptLogic.LoadIcon("svgmap.png");
        this.Columns = new List<ChartScriptColumn>
        {
            new ChartScriptColumn("LocationCode", ChartColumnType.String),
            new ChartScriptColumn("Location", ChartColumnType.Groupable) { IsOptional = true },
            new ChartScriptColumn("Color Scale", ChartColumnType.Magnitude) { IsOptional = true },
            new ChartScriptColumn("Color Category", ChartColumnType.Groupable) { IsOptional = true },
            new ChartScriptColumn("Opacity", ChartColumnType.Magnitude) { IsOptional = true },
        };
        this.ParameterGroups = new List<ChartScriptParameterGroup>
        {
            new ChartScriptParameterGroup("Url")
            {
                new ChartScriptParameter("SvgUrl", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse(svgMaps.ToString("|")) },
            },

            new ChartScriptParameterGroup("Location")
            {
                new ChartScriptParameter("LocationSelector", ChartParameterType.String) { ColumnIndex = 0,  ValueDefinition = new StringValue("path[id]") },
                new ChartScriptParameter("LocationAttribute", ChartParameterType.String) { ColumnIndex = 0,  ValueDefinition = new StringValue("id") },
                new ChartScriptParameter("LocationMatch", ChartParameterType.Enum) {  ColumnIndex = 0, ValueDefinition = EnumValueList.Parse("Exact|Prefix") },
            },

            new ChartScriptParameterGroup("Stroke")
            {
                new ChartScriptParameter("StrokeColor", ChartParameterType.String) {  ValueDefinition = new StringValue("") },
                new ChartScriptParameter("StrokeWidth", ChartParameterType.String) {  ValueDefinition = new StringValue("") },
            },

            new ChartScriptParameterGroup("Fill")
            {
                new ChartScriptParameter("NoDataColor", ChartParameterType.String) {  ValueDefinition = new StringValue("#aaa") },
            },

            new ChartScriptParameterGroup("Color Scale")
            {
                new ChartScriptParameter("ColorScale", ChartParameterType.Enum) { ColumnIndex = 2,  ValueDefinition = EnumValueList.Parse("ZeroMax|MinMax|Sqrt|Log") },
                new ChartScriptParameter("ColorInterpolate", ChartParameterType.Special) {  ColumnIndex = 2, ValueDefinition = new SpecialParameter(SpecialParameterType.ColorInterpolate) },
                new ChartScriptParameter("ColorScaleMaxValue", ChartParameterType.Number) { ColumnIndex = 2, ValueDefinition = new NumberInterval{  DefaultValue = null}  },
            },
            new ChartScriptParameterGroup("Color Category")
            {
                new ChartScriptParameter("ColorCategory", ChartParameterType.Special) { ColumnIndex = 3, ValueDefinition = new SpecialParameter(SpecialParameterType.ColorCategory) },
            }
        };
    }      
}                
