using Signum.Entities.Chart;

namespace Signum.Engine.Chart.Scripts;

public class MarkermapChartScript : ChartScript                
{
    public MarkermapChartScript(): base(GoogleMapsChartScript.Markermap)
    {
        this.Icon = ChartScriptLogic.LoadIcon("markermap.png");
        this.Columns = new List<ChartScriptColumn>
        {
            new ChartScriptColumn("Latitude", ChartColumnType.Magnitude) ,
            new ChartScriptColumn("Longitude", ChartColumnType.Magnitude) ,
            new ChartScriptColumn("Label", ChartColumnType.String) { IsOptional = true },
            new ChartScriptColumn("Icon", ChartColumnType.String) { IsOptional = true },
            new ChartScriptColumn("Title", ChartColumnType.String) { IsOptional = true },
            new ChartScriptColumn("Info", ChartColumnType.String) { IsOptional = true },
            new ChartScriptColumn("Color Scale", ChartColumnType.Real) { IsOptional = true },
            new ChartScriptColumn("Color Category", ChartColumnType.Groupable) { IsOptional = true }
        };
        this.ParameterGroups = new List<ChartScriptParameterGroup>
        {
            new ChartScriptParameterGroup("Map")
            {
                new ChartScriptParameter("MapType", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("Roadmap|Satellite") },
                new ChartScriptParameter("MapStyle", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("Standard|Silver|Retro|Dark|Night|Aubergine") },
            },
            new ChartScriptParameterGroup("Label")
            {
                new ChartScriptParameter("AnimateDrop", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("No|Yes") },
                new ChartScriptParameter("AnimateOnClick", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("No|Yes") },
                new ChartScriptParameter("InfoLinkPosition", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("Inline|Below") },
            },
            new ChartScriptParameterGroup("Performance")
            {
                new ChartScriptParameter("ClusterMap", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("Yes|No") },
            },
            new ChartScriptParameterGroup()
            {
                new ChartScriptParameter("ColorScale", ChartParameterType.Enum) { ColumnIndex = 6,  ValueDefinition = EnumValueList.Parse("ZeroMax|MinMax|Sqrt|Log") },
                new ChartScriptParameter("ColorInterpolation", ChartParameterType.Special) {  ColumnIndex = 6, ValueDefinition = new SpecialParameter(SpecialParameterType.ColorInterpolate) },
                new ChartScriptParameter("ColorCategory", ChartParameterType.Special) {  ColumnIndex = 7, ValueDefinition = new SpecialParameter(SpecialParameterType.ColorCategory)}
            },
            new ChartScriptParameterGroup("Zoom")
            {
                new ChartScriptParameter("Zoom", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 2m } },
                new ChartScriptParameter("MinZoom", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = null } },
                new ChartScriptParameter("MaxZoom", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = null } },
            },
        };
    }      
}                
