using System;
using System.Collections.Generic;
using System.Linq;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using System.Reflection;
using Signum.Entities.Chart;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Utilities;
using System.IO;
using System.Xml.Linq;
using Signum.Entities.Basics;
using Signum.Engine.Chart.Scripts;

namespace Signum.Engine.Chart
{
    public static class ChartScriptLogic
    {
        public static Dictionary<ChartScriptSymbol, ChartScript> Scripts = new Dictionary<ChartScriptSymbol, ChartScript>();

        internal static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {

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

                RegisterScript(new HeatmapChartScript());
                RegisterScript(new MarkermapChartScript());
            }
        }

        private static void RegisterScript(ChartScript chartScript)
        {
            Scripts.Add(chartScript.Symbol, chartScript);
        }

        internal static FileContent LoadIcon(string fileName)
        {
            return new FileContent(fileName, typeof(ChartScriptLogic).Assembly.GetManifestResourceStream("Signum.Engine.Chart.Icons." + fileName)!.ReadAllBytes());
        }
    }
}
