
using Signum.Engine.Chart;
using Signum.Entities.Chart;
using System.Collections.Generic;

namespace Signum.Engine.Chart.Scripts 
{
    public class PivotTableScript : ChartScript                
    {
        public PivotTableScript() : base(HtmlChartScript.PivotTable)
        {
            this.Icon = ChartScriptLogic.LoadIcon("punchcard.png");
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
                CreateBlock("Color Gradiends", "Gradient ", ChartParameterType.Enum, EnumValueList.Parse("none|YlGn|YlGnBu|GnBu|BuGn|PuBuGn|PuBu|BuPu|RdPu|PuRd|OrRd|YlOrRd|YlOrBr|Purples|Blues|Greens|Oranges|Reds|Greys|PuOr|BrBG|PRGn|PiYG|RdBu|RdGy|RdYlBu|Spectral|RdYlGn")),
                CreateBlock("Color Scale", "Scale ", ChartParameterType.Enum, EnumValueList.Parse("ZeroMax|MinMax|Sqrt|Log")),
                CreateBlock("Text Alignment", "text-align ", ChartParameterType.Enum, EnumValueList.Parse("center|start|end")),
                CreateBlock("Vertical Alignment", "vert-align ", ChartParameterType.Enum, EnumValueList.Parse("top|middle|bottom")),
            };
        }

        private static ChartScriptParameterGroup CreateBlock(string groupName, string prefix, ChartParameterType type, IChartParameterValueDefinition valueDefinition)
        {
            return new ChartScriptParameterGroup(groupName)
            {
                new ChartScriptParameter(prefix + "Horizontal Axis", ChartParameterType.Enum) { ValueDefinition = valueDefinition},
                new ChartScriptParameter(prefix + "Horizontal Axis (2)", ChartParameterType.Enum) { ValueDefinition = valueDefinition},
                new ChartScriptParameter(prefix + "Horizontal Axis (3)", ChartParameterType.Enum) { ValueDefinition = valueDefinition},
                new ChartScriptParameter(prefix + "Vertical Axis", ChartParameterType.Enum) { ValueDefinition = valueDefinition},
                new ChartScriptParameter(prefix + "Vertical Axis (2)", ChartParameterType.Enum) { ValueDefinition = valueDefinition},
                new ChartScriptParameter(prefix + "Vertical Axis (3)", ChartParameterType.Enum) { ValueDefinition = valueDefinition},
                new ChartScriptParameter(prefix + "Values", ChartParameterType.Enum) { ValueDefinition = valueDefinition},
            };
        }
    }                
}
