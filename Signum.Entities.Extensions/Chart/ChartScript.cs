using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Signum.Utilities;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Files;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using Signum.Entities.Reflection;
using Signum.Entities.Basics;

namespace Signum.Entities.Chart
{
    [Serializable]
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
    public static class GoogleMapsCharScript
    {
        public static readonly ChartScriptSymbol Heatmap;
        public static readonly ChartScriptSymbol Markermap;

    }

    public abstract class ChartScript
    {
        public ChartScript(ChartScriptSymbol symbol)
        {
            this.Symbol = symbol;
        }

        public ChartScriptSymbol Symbol { get; set; }
        public FileContent Icon { get; set; }
        public List<ChartScriptColumn> Columns { get; set; }
        public List<ChartScriptParameterGroup> ParameterGroups { get; set; }
        public IEnumerable<ChartScriptParameter> AllParameters() => ParameterGroups.SelectMany(a => a);
    }
}
