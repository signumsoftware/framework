using Signum.Entities.Chart;

namespace Signum.Engine.Chart.Scripts;

public class PivotTableScript : ChartScript                
{
    public PivotTableScript() : base(HtmlChartScript.PivotTable)
    {
        this.Icon = ChartScriptLogic.LoadIcon("pivottable.png");
        this.Columns = new List<ChartScriptColumn>
        {
            new ChartScriptColumn("Horizontal Axis", ChartColumnType.Groupable) { IsOptional = true },
            new ChartScriptColumn("Horizontal Axis (2)", ChartColumnType.Groupable) { IsOptional = true },
            new ChartScriptColumn("Horizontal Axis (3)", ChartColumnType.Groupable) { IsOptional = true },
            new ChartScriptColumn("Horizontal Axis (4)", ChartColumnType.Groupable) { IsOptional = true },
            new ChartScriptColumn("Vertical Axis", ChartColumnType.Groupable){ IsOptional = true },
            new ChartScriptColumn("Vertical Axis (2)", ChartColumnType.Groupable){ IsOptional = true },
            new ChartScriptColumn("Vertical Axis (3)", ChartColumnType.Groupable){ IsOptional = true },
            new ChartScriptColumn("Vertical Axis (4)", ChartColumnType.Groupable){ IsOptional = true },
            new ChartScriptColumn("Value", ChartColumnType.Magnitude),
            new ChartScriptColumn("Value (2)", ChartColumnType.Magnitude){ IsOptional = true },
            new ChartScriptColumn("Value (3)", ChartColumnType.Magnitude){ IsOptional = true },
            new ChartScriptColumn("Value (4)", ChartColumnType.Magnitude){ IsOptional = true },
        };
        this.ParameterGroups = new List<ChartScriptParameterGroup>
        {
            CreateBlock("Complete ", ChartParameterType.Enum, EnumValueList.Parse("No|Yes|Consistent|FromFilters"), includeValues: false),
            CreateBlock("Order ", ChartParameterType.Enum, EnumValueList.Parse("None|Ascending|AscendingKey|AscendingToStr|AscendingSumValues|Descending|DescendingKey|DescendingToStr|DescendingSumValues"), includeValues: false),
            CreateBlock("Gradient ", ChartParameterType.Enum, EnumValueList.Parse("None|EntityPalette|YlGn|YlGnBu|GnBu|BuGn|PuBuGn|PuBu|BuPu|RdPu|PuRd|OrRd|YlOrRd|YlOrBr|Purples|Blues|Greens|Oranges|Reds|Greys|PuOr|BrBG|PRGn|PiYG|RdBu|RdGy|RdYlBu|Spectral|RdYlGn"), includeValues: true),
            CreateBlock("Scale ", ChartParameterType.Enum, EnumValueList.Parse("ZeroMax|MinMax|Sqrt|Log"), includeValues: true),
            CreateBlock("CSS Style ", ChartParameterType.String, new StringValue(""), includeValues: true),
            CreateBlock("CSS Style (div) ", ChartParameterType.String, new StringValue(""), includeValues: true),
            CreateBlock("Max Text Length ", ChartParameterType.Number, new NumberInterval { DefaultValue = 50 }, includeValues: false),
            CreateBlock("Show Create Button ", ChartParameterType.Enum, EnumValueList.Parse("No|Yes"), includeValues: true),
            CreateBlock("Show Aggregate Values ", ChartParameterType.Enum, EnumValueList.Parse("Yes|No"), includeValues: true),
            new ChartScriptParameterGroup()
            {
                new ChartScriptParameter("SubTotal Horizontal Axis (2)", ChartParameterType.Enum) { ColumnIndex = 1, ValueDefinition = EnumValueList.Parse("no|yes")},
                new ChartScriptParameter("SubTotal Horizontal Axis (3)", ChartParameterType.Enum) { ColumnIndex = 2, ValueDefinition = EnumValueList.Parse("no|yes")},
                new ChartScriptParameter("SubTotal Horizontal Axis (4)", ChartParameterType.Enum) { ColumnIndex = 3, ValueDefinition = EnumValueList.Parse("no|yes")},
                new ChartScriptParameter("Placeholder Vertical Axis", ChartParameterType.Enum) { ColumnIndex = 4, ValueDefinition = EnumValueList.Parse("no|empty|filled")},
                new ChartScriptParameter("Placeholder Vertical Axis (2)", ChartParameterType.Enum) { ColumnIndex = 5, ValueDefinition = EnumValueList.Parse("no|empty|filled")},
                new ChartScriptParameter("Placeholder Vertical Axis (3)", ChartParameterType.Enum) { ColumnIndex = 6, ValueDefinition = EnumValueList.Parse("no|empty|filled")},
            },
            new ChartScriptParameterGroup()
            {
                new ChartScriptParameter("Multi-Value Format", ChartParameterType.String) { ColumnIndex = 8, ValueDefinition = new StringValue("")},
            }
        };
    }

    private static ChartScriptParameterGroup CreateBlock(string prefix, ChartParameterType type, IChartParameterValueDefinition valueDefinition, bool includeValues)
    {
        var result = new ChartScriptParameterGroup()
        {
            new ChartScriptParameter(prefix + "Horizontal Axis", type) { ColumnIndex = 0, ValueDefinition = valueDefinition},
            new ChartScriptParameter(prefix + "Horizontal Axis (2)", type) { ColumnIndex = 1, ValueDefinition = valueDefinition},
            new ChartScriptParameter(prefix + "Horizontal Axis (3)", type) { ColumnIndex = 2, ValueDefinition = valueDefinition},
            new ChartScriptParameter(prefix + "Horizontal Axis (4)", type) { ColumnIndex = 3, ValueDefinition = valueDefinition},
            new ChartScriptParameter(prefix + "Vertical Axis", type) { ColumnIndex = 4, ValueDefinition = valueDefinition},
            new ChartScriptParameter(prefix + "Vertical Axis (2)", type) { ColumnIndex = 5, ValueDefinition = valueDefinition},
            new ChartScriptParameter(prefix + "Vertical Axis (3)", type) { ColumnIndex = 6, ValueDefinition = valueDefinition},
            new ChartScriptParameter(prefix + "Vertical Axis (4)", type) { ColumnIndex = 7, ValueDefinition = valueDefinition},
        };

        if (includeValues)
        {
            result.Add(new ChartScriptParameter(prefix + "Value", type) { ColumnIndex = 8, ValueDefinition = valueDefinition });
        }
        return result;
    }
}                
