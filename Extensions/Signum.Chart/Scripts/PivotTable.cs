namespace Signum.Chart.Scripts;

public class PivotTableScript : ChartScript
{
    public PivotTableScript() : base(HtmlChartScript.PivotTable)
    {
        Icon = ChartScriptLogic.LoadIcon("pivottable.png");
        Columns = new List<ChartScriptColumn>
        {
            new ChartScriptColumn(ChartColumnMessage.HorizontalAxis, ChartColumnType.AnyGroupKey) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.HorizontalAxis2, ChartColumnType.AnyGroupKey) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.HorizontalAxis3, ChartColumnType.AnyGroupKey) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.HorizontalAxis4, ChartColumnType.AnyGroupKey) { IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.VerticalAxis, ChartColumnType.AnyGroupKey){ IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.VerticalAxis2, ChartColumnType.AnyGroupKey){ IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.VerticalAxis3, ChartColumnType.AnyGroupKey){ IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.VerticalAxis4, ChartColumnType.AnyGroupKey){ IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.Value, ChartColumnType.AnyNumber),
            new ChartScriptColumn(ChartColumnMessage.Value2, ChartColumnType.AnyNumber){ IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.Value3, ChartColumnType.AnyNumber){ IsOptional = true },
            new ChartScriptColumn(ChartColumnMessage.Value4, ChartColumnType.AnyNumber){ IsOptional = true },
        };
        ParameterGroups = new List<ChartScriptParameterGroup>
        {
            CreateBlock(ChartParameter.Complete, ChartParameterType.Enum, EnumValueList.Parse("No|Yes|Consistent|FromFilters"), includeValues: false),
            CreateBlock(ChartParameter.Order, ChartParameterType.Enum, EnumValueList.Parse("None|Ascending|AscendingKey|AscendingToStr|AscendingSumValues|Descending|DescendingKey|DescendingToStr|DescendingSumValues"), includeValues: false),
            CreateBlock(ChartParameter.Gradient, ChartParameterType.Enum, EnumValueList.Parse("None|EntityPalette|YlGn|YlGnBu|GnBu|BuGn|PuBuGn|PuBu|BuPu|RdPu|PuRd|OrRd|YlOrRd|YlOrBr|Purples|Blues|Greens|Oranges|Reds|Greys|PuOr|BrBG|PRGn|PiYG|RdBu|RdGy|RdYlBu|Spectral|RdYlGn"), includeValues: true),
            CreateBlock(ChartParameter.Scale, ChartParameterType.Scala, new Scala(), includeValues: true),
            CreateBlock(ChartParameter.CSSStyle, ChartParameterType.String, new StringValue(""), includeValues: true),
            CreateBlock(ChartParameter.CSSStyleDiv, ChartParameterType.String, new StringValue(""), includeValues: true),
            CreateBlock(ChartParameter.MaxTextLength, ChartParameterType.Number, new NumberInterval { DefaultValue = 50 }, includeValues: false),
            CreateBlock(ChartParameter.ShowCreateButton, ChartParameterType.Enum, EnumValueList.Parse("No|Yes"), includeValues: true),
            CreateBlock(ChartParameter.ShowAggregateValues, ChartParameterType.Enum, EnumValueList.Parse("Yes|No"), includeValues: true),
            new ChartScriptParameterGroup()
            {
                new ChartScriptParameter(ChartParameter.SubTotal, ChartColumnMessage.HorizontalAxis2, ChartParameterType.Enum) { ColumnIndex = 1, ValueDefinition = EnumValueList.Parse("no|yes")},
                new ChartScriptParameter(ChartParameter.SubTotal, ChartColumnMessage.HorizontalAxis3, ChartParameterType.Enum) { ColumnIndex = 2, ValueDefinition = EnumValueList.Parse("no|yes")},
                new ChartScriptParameter(ChartParameter.SubTotal, ChartColumnMessage.HorizontalAxis4, ChartParameterType.Enum) { ColumnIndex = 3, ValueDefinition = EnumValueList.Parse("no|yes")},
                new ChartScriptParameter(ChartParameter.Placeholder, ChartColumnMessage.VerticalAxis, ChartParameterType.Enum) { ColumnIndex = 4, ValueDefinition = EnumValueList.Parse("no|empty|filled")},
                new ChartScriptParameter(ChartParameter.Placeholder, ChartColumnMessage.VerticalAxis2, ChartParameterType.Enum) { ColumnIndex = 5, ValueDefinition = EnumValueList.Parse("no|empty|filled")},
                new ChartScriptParameter(ChartParameter.Placeholder, ChartColumnMessage.VerticalAxis3, ChartParameterType.Enum) { ColumnIndex = 6, ValueDefinition = EnumValueList.Parse("no|empty|filled")},
            },
            new ChartScriptParameterGroup()
            {
                new ChartScriptParameter(ChartParameter.MultiValueFormat, ChartParameterType.String) { ColumnIndex = 8, ValueDefinition = new StringValue("")},
            }
        };
    }

    private static ChartScriptParameterGroup CreateBlock(Enum prefix, ChartParameterType type, IChartParameterValueDefinition valueDefinition, bool includeValues)
    {
        var result = new ChartScriptParameterGroup()
        {
            new ChartScriptParameter(prefix, ChartColumnMessage.HorizontalAxis, type) { ColumnIndex = 0, ValueDefinition = valueDefinition},
            new ChartScriptParameter(prefix, ChartColumnMessage.HorizontalAxis2, type) { ColumnIndex = 1, ValueDefinition = valueDefinition},
            new ChartScriptParameter(prefix, ChartColumnMessage.HorizontalAxis3, type) { ColumnIndex = 2, ValueDefinition = valueDefinition},
            new ChartScriptParameter(prefix, ChartColumnMessage.HorizontalAxis4, type) { ColumnIndex = 3, ValueDefinition = valueDefinition},
            new ChartScriptParameter(prefix, ChartColumnMessage.VerticalAxis, type) { ColumnIndex = 4, ValueDefinition = valueDefinition},
            new ChartScriptParameter(prefix, ChartColumnMessage.VerticalAxis2, type) { ColumnIndex = 5, ValueDefinition = valueDefinition},
            new ChartScriptParameter(prefix, ChartColumnMessage.VerticalAxis3, type) { ColumnIndex = 6, ValueDefinition = valueDefinition},
            new ChartScriptParameter(prefix, ChartColumnMessage.VerticalAxis4, type) { ColumnIndex = 7, ValueDefinition = valueDefinition},
        };

        if (includeValues)
        {
            result.Add(new ChartScriptParameter(prefix, ChartColumnMessage.Value, type) { ColumnIndex = 8, ValueDefinition = valueDefinition });
        }
        return result;
    }
}
