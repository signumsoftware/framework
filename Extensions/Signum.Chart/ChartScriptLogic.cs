using Signum.Chart.Scripts;

namespace Signum.Chart;

public static class ChartScriptLogic
{
    public static Dictionary<ChartScriptSymbol, ChartScript> Scripts = new Dictionary<ChartScriptSymbol, ChartScript>();

    internal static void Start(SchemaBuilder sb, bool googleMapsChartScripts, string[]? svgMapUrls = null)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;


        SymbolLogic<ChartScriptSymbol>.Start(sb, () => Scripts.Keys);

        ChartRequestModel.GetChartScriptFunc = s => Scripts.GetOrThrow(s);

        RegisterScript(new BarsChartScript());
        RegisterScript(new ColumnsChartScript());
        RegisterScript(new LineChartScript());

        RegisterScript(new MultiBarsChartScript());
        RegisterScript(new MultiColumnsChartScript());
        RegisterScript(new MultiLinesChartScript());

        RegisterScript(new StackedBarsChartScript());
        RegisterScript(new StackedColumnsChartScript());
        RegisterScript(new StackedLinesChartScript());

        RegisterScript(new PieChartScript());
        RegisterScript(new BubblePackChartScript());

        RegisterScript(new ScatterplotChartScript());
        RegisterScript(new BubbleplotChartScript());

        RegisterScript(new ParallelCoordiantesChartScript());
        RegisterScript(new PunchcardChartScript());
        RegisterScript(new CalendarStreamChartScript());
        RegisterScript(new TreeMapChartScript());
        RegisterScript(new PivotTableScript());

        if (googleMapsChartScripts)
        {
            RegisterScript(new HeatmapChartScript());
            RegisterScript(new MarkermapChartScript());
        }

        if (svgMapUrls != null)
        {
            RegisterScript(new SvgMapScript(svgMapUrls));
        }

    }

    private static void RegisterScript(ChartScript chartScript)
    {
        Scripts.Add(chartScript.Symbol, chartScript);
    }

    internal static FileContent LoadIcon(string fileName)
    {
        return new FileContent(fileName, typeof(ChartScriptLogic).Assembly.GetManifestResourceStream("Signum.Chart.Icons." + fileName)!.ReadAllBytes());
    }
}
