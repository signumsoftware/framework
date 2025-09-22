using Signum.DynamicQuery.Tokens;
using System.ComponentModel;
using System.Diagnostics;

namespace Signum.Chart;

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
            case FilterType.Lite: return ChartColumnType.Entity;
            case FilterType.Boolean:
            case FilterType.Enum: return ChartColumnType.Enum;
            case FilterType.String:
            case FilterType.Guid: return ChartColumnType.String;
            case FilterType.Integer: return ChartColumnType.Number;
            case FilterType.Decimal: return token.IsGroupable ? ChartColumnType.RoundedNumber : ChartColumnType.DecimalNumber;
            case FilterType.DateTime: return token.IsGroupable ? ChartColumnType.Date : ChartColumnType.DateTime;
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
        if ((token is DatePartStartToken dt && dt.Name is QueryTokenDateMessage.MonthStart or QueryTokenDateMessage.WeekStart or QueryTokenDateMessage.QuarterStart) ||
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
                var byName = chart.Parameters.ToDictionary(a => a.Name.RemoveChars(' ', '(', ')', '-').ToLowerInvariant());
                chart.Parameters.Clear();
                foreach (var sp in chartScriptParameters)
                {
                    var cp = byName.TryGetC(sp.Name.ToLowerInvariant());

                    if (cp != null)
                    {
                        byName.Remove(sp.Name.ToLowerInvariant());
                        cp.Name = sp.Name;
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

                if(byName.Any() && Debugger.IsAttached)
                {
                    Debugger.Break(); //Loosing parameters
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
    ChartType,
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
    QueryResultReachedMaxRows0,
    ListView,
    [Description("The selected token should be a {0}")]
    TheSelectedTokenShouldBeA0,

    [Description("The selected token should be either:")]
    TheSelectedTokenShouldBeEither
}


[InTypeScript(true), DescriptionOptions(DescriptionOptions.Members)]
public enum ChartParameter
{
    CompleteValues,
    Scale,
    Labels,
    LabelsMargin,
    NumberOpacity,
    NumberColor,
    ColorCategory,
    HorizontalScale,
    VerticalScale,
    UnitMargin,
    NumberMinWidth,
    CircleStroke,
    CircleRadius,
    CircleAutoReduce,
    CircleRadiusHover,
    Color,
    Interpolate,
    MapType,
    MapStyle,
    AnimateDrop,
    AnimateOnClick,
    InfoLinkPosition,
    ClusterMap,
    ColorScale,
    ColorInterpolation,
    LabelMargin,
    NumberSizeLimit,
    FillOpacity,
    ColorInterpolate,
    StrokeColor,
    StrokeWidth,
    [Description("Scale (1)")] Scale1,
    [Description("Scale (2)")] Scale2,
    [Description("Scale (3)")] Scale3,
    [Description("Scale (4)")] Scale4,
    [Description("Scale (5)")] Scale5,
    [Description("Scale (6)")] Scale6,
    [Description("Scale (7)")] Scale7,
    [Description("Scale (8)")] Scale8,
    InnerRadious,
    Sort,
    SvgUrl,
    LocationSelector,
    LocationAttribute,
    LocationMatch,
    ColorScaleMaxValue,
    NoDataColor,
    StartDate,
    Opacity,
    [Description("Radious(px)")]
    RadiousPx,
    SizeScale,
    TopMargin,
    RightMargin,
    ShowLabel,
    LabelColor,
    ForceColor,
    SubTotal,
    Placeholder,
    MultiValueFormat,
    Complete,
    Order,
    Gradient,
    [Description("CSS Style")]
    CSSStyle,
    [Description("CSS Style (div)")]
    CSSStyleDiv,
    MaxTextLength,
    ShowCreateButton,
    ShowAggregateValues,
    PointSize,
    DrawingMode,
    MinZoom,
    MaxZoom,
    CompleteHorizontalValues,
    CompleteVerticalValues,
    Shape,
    XMargin,
    HorizontalLineColor,
    VerticalLineColor,
    XSort,
    YSort,
    FillColor,
    OpacityScale,
    InnerSizeType,
    InnerFillColor,
    Stack,
    ValueAsPercent,
    HorizontalMargin,
    Padding,
    Zoom,
    Value,
    Percent,
    Total
}

public enum ChartParameterGroupMessage
{
    Stroke,
    Number,
    Opacity,
    ColorScale,
    ColorCategory,
    Margin,
    Circle,
    Shape,
    Color,
    Arrange,
    ShowValue,
    Url,
    Location,
    Fill,
    Map,
    Label,
    ColorGradient,
    Margins,
    Points,
    Numbers,
    Performance,
    Zoom,
    FillColor,
    Size,
    InnerSize,
    ShowPercent,
    Scale
}

public enum ChartColumnMessage
{
    SplitLines,
    Height,
    [Description("Height (2)")]Height2,
    [Description("Height (3)")] Height3,
    [Description("Height (4)")] Height4,
    [Description("Height (5)")] Height5,
    Line,
    [Description("Dimension (1)")] Dimension1,
    [Description("Dimension (2)")] Dimension2,
    [Description("Dimension (3)")] Dimension3,
    [Description("Dimension (4)")] Dimension4,
    [Description("Dimension (5)")] Dimension5,
    [Description("Dimension (6)")] Dimension6,
    [Description("Dimension (7)")] Dimension7,
    [Description("Dimension (8)")] Dimension8,
    Angle,
    Sections,
    Areas,
    ColorCategory,
    LocationCode,
    Location,
    ColorScale,
    Opacity,
    Date,
    Latitude,
    Longitude,
    Weight,
    Bubble,
    Size,
    Parent,
    Columns,
    Label,
    Icon,
    Title,
    Info,
    SplitBars,
    Width,
    Width2,
    Width3,
    Width4,
    Width5,
    SplitColumns,
    HorizontalAxis,
    [Description("Horizontal Axis (2)")] HorizontalAxis2,
    [Description("Horizontal Axis (3)")] HorizontalAxis3,
    [Description("Horizontal Axis (4)")] HorizontalAxis4,
    [Description("Vertical Axis (2)")] VerticalAxis2,
    VerticalAxis,
    [Description("Vertical Axis (3)")] VerticalAxis3,
    [Description("Vertical Axis (4)")] VerticalAxis4,
    Value,
    [Description("Value (2)")]Value2,
    [Description("Value (3)")]Value3,
    [Description("Value (4)")]Value4,
    Point,
    Bars,
    Color,
    InnerSize,
    Order,
    Square
}
