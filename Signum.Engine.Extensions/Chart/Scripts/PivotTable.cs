
using Signum.Engine.Chart;
using Signum.Entities.Chart;
using System.Collections.Generic;

namespace Signum.Engine.Chart.Scripts 
{
    public class PivotTableScript : ChartScript                
    {
        public PivotTableScript() : base(HtmlChartScript.PivotTable)
        {
            this.Icon = ChartScriptLogic.LoadIcon("pivottable.png");
            this.Columns = new List<ChartScriptColumn>
            {
                new ChartScriptColumn("Horizontal", ChartColumnType.Groupable) { IsOptional = true },
                new ChartScriptColumn("Horizontal Axis (2)", ChartColumnType.Groupable) { IsOptional = true },
                new ChartScriptColumn("Horizontal Axis (3)", ChartColumnType.Groupable) { IsOptional = true },
                new ChartScriptColumn("Vertical Axis", ChartColumnType.Groupable){ IsOptional = true },
                new ChartScriptColumn("Vertical Axis (2)", ChartColumnType.Groupable){ IsOptional = true },
                new ChartScriptColumn("Vertical Axis (3)", ChartColumnType.Groupable){ IsOptional = true },
                new ChartScriptColumn("Value", ChartColumnType.Magnitude),
            };
            this.ParameterGroups = new List<ChartScriptParameterGroup>
            {
                CreateBlock("Complete Axis", "Complete ", ChartParameterType.Enum, EnumValueList.Parse("No|Yes|Consisten|FromFilters"), includeValues: false),
                CreateBlock("Order Axis", "Order ", ChartParameterType.Enum, EnumValueList.Parse("None|Ascending|AscendingKey|AscendingToStr|AscendingSumValues|Descending|DescendingKey|DescendingToStr|DescendingSumValues"), includeValues: false),
                CreateBlock("Color Gradiends", "Gradient ", ChartParameterType.Enum, EnumValueList.Parse("None|YlGn|YlGnBu|GnBu|BuGn|PuBuGn|PuBu|BuPu|RdPu|PuRd|OrRd|YlOrRd|YlOrBr|Purples|Blues|Greens|Oranges|Reds|Greys|PuOr|BrBG|PRGn|PiYG|RdBu|RdGy|RdYlBu|Spectral|RdYlGn"), includeValues: true),
                CreateBlock("Color Scale", "Scale ", ChartParameterType.Enum, EnumValueList.Parse("ZeroMax|MinMax|Sqrt|Log"), includeValues: true),
                CreateBlock("Text Alignment", "text-align ", ChartParameterType.Enum, EnumValueList.Parse("center|start|end"), includeValues: true),
                CreateBlock("Vertical Alignment", "vert-align ", ChartParameterType.Enum, EnumValueList.Parse("top|middle|bottom"), includeValues: true),
            };
        }

        private static ChartScriptParameterGroup CreateBlock(string groupName, string prefix, ChartParameterType type, IChartParameterValueDefinition valueDefinition, bool includeValues)
        {
            var result = new ChartScriptParameterGroup(groupName)
            {
                new ChartScriptParameter(prefix + "Horizontal Axis", ChartParameterType.Enum) { ValueDefinition = valueDefinition},
                new ChartScriptParameter(prefix + "Horizontal Axis (2)", ChartParameterType.Enum) { ValueDefinition = valueDefinition},
                new ChartScriptParameter(prefix + "Horizontal Axis (3)", ChartParameterType.Enum) { ValueDefinition = valueDefinition},
                new ChartScriptParameter(prefix + "Vertical Axis", ChartParameterType.Enum) { ValueDefinition = valueDefinition},
                new ChartScriptParameter(prefix + "Vertical Axis (2)", ChartParameterType.Enum) { ValueDefinition = valueDefinition},
                new ChartScriptParameter(prefix + "Vertical Axis (3)", ChartParameterType.Enum) { ValueDefinition = valueDefinition},
            };

            if (includeValues)
            {
                result.Add(new ChartScriptParameter(prefix + "Values", ChartParameterType.Enum) { ValueDefinition = valueDefinition });
            }
            return result;
        }
    }                
}
