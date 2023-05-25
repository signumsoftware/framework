using Signum.Utilities.Reflection;

namespace Signum.DynamicQuery.Tokens;

public class DatePartStartToken : QueryToken
{
    public QueryTokenMessage Name { get; private set; }
    QueryToken parent;
    public override QueryToken? Parent => parent;

    internal DatePartStartToken(QueryToken parent, QueryTokenMessage name)
    {
        this.Name = name;
        this.parent = parent ?? throw new ArgumentNullException(nameof(parent));
    }

    private static MethodInfo GetMethodInfoDateTime(QueryTokenMessage name)
    {
        return
            name == QueryTokenMessage.MonthStart ? miDTMonthStart :
            name == QueryTokenMessage.QuarterStart ? miDTQuarterStart :
            name == QueryTokenMessage.WeekStart ? miDTWeekStart :
            name == QueryTokenMessage.HourStart ? miDTHourStart :
            name == QueryTokenMessage.MinuteStart ? miDTMinuteStart :
            name == QueryTokenMessage.SecondStart ? miDTSecondStart :
            throw new InvalidOperationException("Unexpected name");
    }

    private static MethodInfo GetMethodInfoDateOnly(QueryTokenMessage name)
    {
        return
            name == QueryTokenMessage.MonthStart ? miDMonthStart :
            name == QueryTokenMessage.QuarterStart ? miDQuarterStart :
            name == QueryTokenMessage.WeekStart ? miDWeekStart :
            throw new InvalidOperationException("Unexpected name");
    }

    private static MethodInfo GetMethodInfoTimeSpan(QueryTokenMessage name)
    {
        return
           name == QueryTokenMessage.HourStart ? miTSHourStart :
            name == QueryTokenMessage.MinuteStart ? miTSMinuteStart :
            name == QueryTokenMessage.SecondStart ? miTSSecondStart :
            throw new InvalidOperationException("Unexpected name");
    }

    private static MethodInfo GetMethodInfoTimeOnly(QueryTokenMessage name)
    {
        return
           name == QueryTokenMessage.HourStart ? miTOHourStart :
            name == QueryTokenMessage.MinuteStart ? miTOMinuteStart :
            name == QueryTokenMessage.SecondStart ? miTOSecondStart :
            throw new InvalidOperationException("Unexpected name");
    }


    public override string ToString()
    {
        return this.Name.NiceToString();
    }

    public override string NiceName()
    {
        return QueryTokenMessage._0Of1.NiceToString(this.Name.NiceToString(), parent.ToString());
    }

    public override string? Format
    {
        get
        {
            return
                Name == QueryTokenMessage.MonthStart ? "Y" :
                Name == QueryTokenMessage.QuarterStart ? "d" :
                Name == QueryTokenMessage.WeekStart ? "d" :
                Name == QueryTokenMessage.HourStart ? "g" :
                Name == QueryTokenMessage.MinuteStart ? "g" :
                Name == QueryTokenMessage.SecondStart ? "G" :
                throw new InvalidOperationException("Unexpected name");
        }
    }

    public override string? Unit
    {
        get { return null; }
    }

    public override Type Type
    {
        get { return Parent!.Type.Nullify(); }
    }

    public override string Key
    {
        get { return this.Name.ToString(); }
    }

    protected override List<QueryToken> SubTokensOverride(SubTokensOptions options)
    {
        return new List<QueryToken>();
    }

    public static MethodInfo miDTMonthStart = ReflectionTools.GetMethodInfo(() => DateTimeExtensions.MonthStart(DateTime.MinValue));
    public static MethodInfo miDTQuarterStart = ReflectionTools.GetMethodInfo(() => DateTimeExtensions.QuarterStart(DateTime.MinValue));
    public static MethodInfo miDTWeekStart = ReflectionTools.GetMethodInfo(() => DateTimeExtensions.WeekStart(DateTime.MinValue));
    public static MethodInfo miDTHourStart = ReflectionTools.GetMethodInfo(() => DateTimeExtensions.HourStart(DateTime.MinValue));
    public static MethodInfo miDTMinuteStart = ReflectionTools.GetMethodInfo(() => DateTimeExtensions.MinuteStart(DateTime.MinValue));
    public static MethodInfo miDTSecondStart = ReflectionTools.GetMethodInfo(() => DateTimeExtensions.SecondStart(DateTime.MinValue));

    public static MethodInfo miDMonthStart = ReflectionTools.GetMethodInfo(() => DateTimeExtensions.MonthStart(DateOnly.MinValue));
    public static MethodInfo miDQuarterStart = ReflectionTools.GetMethodInfo(() => DateTimeExtensions.QuarterStart(DateOnly.MinValue));
    public static MethodInfo miDWeekStart = ReflectionTools.GetMethodInfo(() => DateTimeExtensions.WeekStart(DateOnly.MinValue));

    public static MethodInfo miTSHourStart = ReflectionTools.GetMethodInfo(() => TimeSpanExtensions.TrimToHours(TimeSpan.Zero));
    public static MethodInfo miTSMinuteStart = ReflectionTools.GetMethodInfo(() => TimeSpanExtensions.TrimToMinutes(TimeSpan.Zero));
    public static MethodInfo miTSSecondStart = ReflectionTools.GetMethodInfo(() => TimeSpanExtensions.TrimToSeconds(TimeSpan.Zero));

    public static MethodInfo miTOHourStart = ReflectionTools.GetMethodInfo(() => TimeOnlyExtensions.TrimToHours(TimeOnly.MinValue));
    public static MethodInfo miTOMinuteStart = ReflectionTools.GetMethodInfo(() => TimeOnlyExtensions.TrimToMinutes(TimeOnly.MinValue));
    public static MethodInfo miTOSecondStart = ReflectionTools.GetMethodInfo(() => TimeOnlyExtensions.TrimToSeconds(TimeOnly.MinValue));

    protected override Expression BuildExpressionInternal(BuildExpressionContext context)
    {
        var exp = parent.BuildExpression(context);

        var ut = parent.Type.UnNullify();

        var mi =
            ut == typeof(DateTime) ? GetMethodInfoDateTime(this.Name) :
            ut == typeof(DateOnly) ? GetMethodInfoDateOnly(this.Name) :
            ut == typeof(TimeOnly) ? GetMethodInfoTimeOnly(this.Name) :
            ut == typeof(TimeSpan) ? GetMethodInfoTimeSpan(this.Name) :
             throw new UnexpectedValueException(ut);

        return Expression.Call(mi, exp.UnNullify()).Nullify();
    }

    public override PropertyRoute? GetPropertyRoute()
    {
        return parent.GetPropertyRoute();
    }

    public override Implementations? GetImplementations()
    {
        return null;
    }

    public override string? IsAllowed()
    {
        return parent.IsAllowed();
    }

    public override QueryToken Clone()
    {
        return new DatePartStartToken(parent.Clone(), this.Name);
    }

    public override bool IsGroupable
    {
        get { return true; }
    }
}
