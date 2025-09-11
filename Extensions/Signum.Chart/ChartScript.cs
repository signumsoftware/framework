namespace Signum.Chart;

[EntityKind(EntityKind.SystemString, EntityData.Master, IsLowPopulation = true)]
public class ChartScriptSymbol : Symbol
{
    private ChartScriptSymbol() { }

    public ChartScriptSymbol(Type declaringType, string fieldName) :
        base(declaringType, fieldName)
    {
    }
}

[AutoInit]
public static class D3ChartScript
{
    public static readonly ChartScriptSymbol Bars;
    public static readonly ChartScriptSymbol Columns;
    public static readonly ChartScriptSymbol Line;

    public static readonly ChartScriptSymbol MultiBars;
    public static readonly ChartScriptSymbol MultiColumns;
    public static readonly ChartScriptSymbol MultiLines;

    public static readonly ChartScriptSymbol StackedBars;
    public static readonly ChartScriptSymbol StackedColumns;
    public static readonly ChartScriptSymbol StackedLines;

    public static readonly ChartScriptSymbol Pie;
    public static readonly ChartScriptSymbol BubblePack;

    public static readonly ChartScriptSymbol Scatterplot;
    public static readonly ChartScriptSymbol Bubbleplot;

    public static readonly ChartScriptSymbol ParallelCoordinates;
    public static readonly ChartScriptSymbol Punchcard;
    public static readonly ChartScriptSymbol CalendarStream;
    public static readonly ChartScriptSymbol Treemap;

}

[AutoInit]
public static class HtmlChartScript
{
    public static readonly ChartScriptSymbol PivotTable;

}

[AutoInit]
public static class SvgMapsChartScript
{
    public static readonly ChartScriptSymbol SvgMap;

}

[AutoInit]
public static class GoogleMapsChartScript
{
    public static readonly ChartScriptSymbol Heatmap;
    public static readonly ChartScriptSymbol Markermap;

}

public abstract class ChartScript
{
    public ChartScript(ChartScriptSymbol symbol)
    {
        Symbol = symbol;
    }

    //public abstract string Description { get; }

    public ChartScriptSymbol Symbol { get; set; }
    public FileContent Icon { get; set; }
    public List<ChartScriptColumn> Columns { get; set; }
    public List<ChartScriptParameterGroup> ParameterGroups { get; set; }
    public IEnumerable<ChartScriptParameter> AllParameters() => ParameterGroups.SelectMany(a => a);
}
