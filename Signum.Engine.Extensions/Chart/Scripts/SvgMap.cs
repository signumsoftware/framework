
using Signum.Engine.Chart;
using Signum.Entities.Chart;
using Signum.Utilities;
using System.Collections.Generic;

namespace Signum.Engine.Chart.Scripts 
{
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
                    new ChartScriptParameter("LocationSelector", ChartParameterType.String) { ColumnIndex = 0,  ValueDefinition = new StringValue("path[data-code]") },
                    new ChartScriptParameter("LocationAttribute", ChartParameterType.String) { ColumnIndex = 0,  ValueDefinition = new StringValue("data-code") },
                    new ChartScriptParameter("LocationMatch", ChartParameterType.Enum) {  ColumnIndex = 0, ValueDefinition = EnumValueList.Parse("Exact|Prefix") },
                },

                new ChartScriptParameterGroup("Stroke")
                {
                    new ChartScriptParameter("StrokeColor", ChartParameterType.String) {  ValueDefinition = new StringValue("") },
                    new ChartScriptParameter("StrokeWidth", ChartParameterType.String) {  ValueDefinition = new StringValue("") },
                },

                new ChartScriptParameterGroup("Color Scale")
                {
                    new ChartScriptParameter("ColorScale", ChartParameterType.Enum) { ColumnIndex = 2,  ValueDefinition = EnumValueList.Parse("ZeroMax|MinMax|Sqrt|Log") },
                    new ChartScriptParameter("ColorInterpolate", ChartParameterType.Enum) {  ColumnIndex = 2, ValueDefinition = EnumValueList.Parse("YlGn|YlGnBu|GnBu|BuGn|PuBuGn|PuBu|BuPu|RdPu|PuRd|OrRd|YlOrRd|YlOrBr|Purples|Blues|Greens|Oranges|Reds|Greys|PuOr|BrBG|PRGn|PiYG|RdBu|RdGy|RdYlBu|Spectral|RdYlGn") },
                    new ChartScriptParameter("ColorScaleMaxValue", ChartParameterType.Number) { ColumnIndex = 2, ValueDefinition = new NumberInterval{  DefaultValue = null}  },
                },
                new ChartScriptParameterGroup("Color Category")
                {
                    new ChartScriptParameter("ColorCategory", ChartParameterType.Enum) { ColumnIndex = 3, ValueDefinition = EnumValueList.Parse("category10|accent|dark2|paired|pastel1|pastel2|set1|set2|set3|BrBG[K]|PRGn[K]|PiYG[K]|PuOr[K]|RdBu[K]|RdGy[K]|RdYlBu[K]|RdYlGn[K]|Spectral[K]|Blues[K]|Greys[K]|Oranges[K]|Purples[K]|Reds[K]|BuGn[K]|BuPu[K]|OrRd[K]|PuBuGn[K]|PuBu[K]|PuRd[K]|RdPu[K]|YlGnBu[K]|YlGn[K]|YlOrBr[K]|YlOrRd[K]") },
                    new ChartScriptParameter("ColorCategorySteps", ChartParameterType.Enum) { ColumnIndex = 3, ValueDefinition = EnumValueList.Parse("3|4|5|6|7|8|9|10|11") },
                }
            };
        }      
    }                
}
