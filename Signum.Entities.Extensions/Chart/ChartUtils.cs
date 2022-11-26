using Signum.Entities.DynamicQuery;
using Signum.Entities.UserQueries;
using System.ComponentModel;

namespace Signum.Entities.Chart;

public static class ChartUtils
{
    public static bool IsChartColumnType(QueryToken token, ChartColumnType ct)
    {
        if (token == null)
            return false;

        var type = token.GetChartColumnType();

        if (type == null)
            return false;

        return Flag(ct, type.Value);
    }

    public static ChartColumnType? GetChartColumnType(this QueryToken token)
    {
        switch (QueryUtils.TryGetFilterType(token.Type))
        {
            case FilterType.Lite: return ChartColumnType.Lite;
            case FilterType.Boolean:
            case FilterType.Enum: return ChartColumnType.Enum;
            case FilterType.String:
            case FilterType.Guid: return ChartColumnType.String;
            case FilterType.Integer: return ChartColumnType.Integer;
            case FilterType.Decimal: return token.IsGroupable ? ChartColumnType.RealGroupable : ChartColumnType.Real;
            case FilterType.DateTime: return token.IsGroupable ? ChartColumnType.DateOnly : ChartColumnType.DateTime;
            case FilterType.Time: return ChartColumnType.Time;
        }

        return null;
    }

    public static bool Flag(ChartColumnType ct, ChartColumnType flag)
    {
        return (ct & flag) == flag;
    }

    public static bool IsDateOnly(QueryToken token)
    {
        if ((token is DatePartStartToken dt && (dt.Name == QueryTokenMessage.MonthStart || dt.Name == QueryTokenMessage.WeekStart)) ||
            token is DateToken)
            return true;

        PropertyRoute? route = token.GetPropertyRoute();

        if (route != null && route.PropertyRouteType == PropertyRouteType.FieldOrProperty)
        {
            var pp = Validator.TryGetPropertyValidator(route);
            if (pp != null)
            {
                DateTimePrecisionValidatorAttribute? datetimePrecision = pp.Validators.OfType<DateTimePrecisionValidatorAttribute>().SingleOrDefaultEx();

                if (datetimePrecision != null && datetimePrecision.Precision == DateTimePrecision.Days)
                    return true;

            }
        }

        return false;
    }
    
    public static bool SynchronizeColumns(this ChartScript chartScript, IChartBase chart, PostRetrievingContext? ctx)
    {
        bool result = false;

        for (int i = 0; i < chartScript.Columns.Count; i++)
        {
            if (chart.Columns.Count <= i)
            {
                chart.Columns.Add(new ChartColumnEmbedded());
                result = true;
            }

            chart.Columns[i].parentChart = chart;
            chart.Columns[i].ScriptColumn = chartScript.Columns[i];

            if (!result)
                result = chart.Columns[i].IntegrityCheck() != null;
        }

        if (chart.Columns.Count > chartScript.Columns.Count)
        {
            chart.Columns.RemoveRange(chartScript.Columns.Count, chart.Columns.Count - chartScript.Columns.Count);
            result = true;
        }

        if (chart.Parameters.Modified != ModifiedState.Sealed)
        {
            var chartScriptParameters = chartScript.AllParameters().ToList();

            if (chart.Parameters.Select(a => a.Name).OrderBy().SequenceEqual(chartScriptParameters.Select(a => a.Name).OrderBy()))
            {
                foreach (var cp in chart.Parameters)
                {
                    var sp = chartScriptParameters.FirstEx(a => a.Name == cp.Name);

                    cp.parentChart = chart;
                    cp.ScriptParameter = sp;
                    
                    //if (cp.PropertyCheck(() => cp.Value).HasText())
                    //    cp.Value = sp.DefaultValue(cp.GetToken());
                }
            }
            else
            {
                var byName = chart.Parameters.ToDictionary(a => a.Name);
                chart.Parameters.Clear();
                foreach (var sp in chartScriptParameters)
                {
                    var cp = byName.TryGetC(sp.Name);

                    if (cp != null)
                    {
                        cp.parentChart = chart;
                        cp.ScriptParameter = sp;
                        ctx?.ForceModifiedState.Add(cp, ModifiedState.SelfModified);
                        //if (cp.PropertyCheck(() => cp.Value).HasText())
                        //    cp.Value = sp.DefaultValue(cp.GetToken());
                    }
                    else
                    {
                        cp = new ChartParameterEmbedded
                        {
                            Name = sp.Name,
                            parentChart = chart,
                            ScriptParameter = sp,
                            Value = sp.ValueDefinition.DefaultValue(sp.GetToken(chart))
                        };
                    }

                    chart.Parameters.Add(cp);
                }
            }
        }

        return result;
    }
    
    internal static void FixParameters(IChartBase chart, ChartColumnEmbedded chartColumn)
    {
        int index = chart.Columns.IndexOf(chartColumn);

        foreach (var p in chart.Parameters.Where(p => p.ScriptParameter?.ColumnIndex == index))
        {
            if (p.PropertyCheck(() => p.Value).HasText())
                p.Value = p.ScriptParameter.DefaultValue(chartColumn.Token?.Token);
        }
    }
}

public enum ChartMessage
{
    [Description("{0} can only be created from the chart window")]
    _0CanOnlyBeCreatedFromTheChartWindow,
    [Description("{0} can only be created from the search window")]
    _0CanOnlyBeCreatedFromTheSearchWindow,
    Chart,
    [Description("Chart")]
    ChartToken,
    [Description("Chart settings")]
    ChartSettings,
    [Description("Dimension")]
    Dimension,
    DrawChart,
    [Description("Group")]
    Group,
    [Description("Query {0} is not allowed")]
    Query0IsNotAllowed,
    [Description("Toggle info")]
    ToggleInfo,
    [Description("Colors for {0}")]
    ColorsFor0,
    CreatePalette,
    [Description("My Charts")]
    MyCharts,
    CreateNew,
    Edit,
    ApplyChanges,
    ViewPalette,
    [Description("Chart for")]
    ChartFor,
    [Description("Chart of {0}")]
    ChartOf0,
    [Description("{0} is key, but {1} is an aggregate")]
    _0IsKeyBut1IsAnAggregate,
    [Description("{0} should be an aggregate")]
    _0ShouldBeAnAggregate,
    [Description("{0} should be set")]
    _0ShouldBeSet,
    [Description("{0} should be null")]
    _0ShouldBeNull,
    [Description("{0} is not {1}")]
    _0IsNot1,
    [Description("{0} is an aggregate, but the chart is not grouping")]
    _0IsAnAggregateButTheChartIsNotGrouping,
    [Description("{0} is not optional")]
    _0IsNotOptional,
    SavePalette,
    NewPalette,
    Data,
    ChooseABasePalette,
    DeletePalette,
    Preview,
    TypeNotFound,
    [Description("Type {0} is not in the database")]
    Type0NotFoundInTheDatabase,
    Reload,
    Maximize,
    Minimize,
    ShowChartSettings,
    HideChartSettings,
    [Description("Query result reached max rows ({0})")]
    QueryResultReachedMaxRows0
}

