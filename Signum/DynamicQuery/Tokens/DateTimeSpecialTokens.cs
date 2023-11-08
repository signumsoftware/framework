using Signum.Utilities.Reflection;

namespace Signum.DynamicQuery.Tokens;

public class DatePartStartToken : QueryToken
{
    public QueryTokenMessage Name { get; private set; }
    public int? Step { get; private set; }
    QueryToken parent;
    public override QueryToken? Parent => parent;

    internal DatePartStartToken(QueryToken parent, QueryTokenMessage name, int? step = null)
    {
        this.Name = name;
        this.parent = parent ?? throw new ArgumentNullException(nameof(parent));
        if (name is QueryTokenMessage.Every0Hours or QueryTokenMessage.Every0Minutes or QueryTokenMessage.Every0Seconds)
            this.Step = step!.Value;
    }

    private static MethodInfo GetMethodInfoDateTime(QueryTokenMessage name)
    {
        return
            name == QueryTokenMessage.MonthStart ? miDTMonthStart :
            name == QueryTokenMessage.QuarterStart ? miDTQuarterStart :
            name == QueryTokenMessage.WeekStart ? miDTWeekStart :
            name == QueryTokenMessage.TruncHours ? miDTTruncHour :
            name == QueryTokenMessage.Every0Hours ? miDTTruncStepHour :
            name == QueryTokenMessage.TruncMinutes ? miDTTruncMinute :
            name == QueryTokenMessage.Every0Minutes ? miDTTruncStepMinute :
            name == QueryTokenMessage.SecondStart ? miDTTruncSecond :
            name == QueryTokenMessage.Every0Seconds ? miDTTruncStepSecond :
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
           name == QueryTokenMessage.TruncHours ? miTSTruncHour :
           name == QueryTokenMessage.Every0Hours ? miTSTruncStepHour :
           name == QueryTokenMessage.TruncMinutes ? miTSTruncMinute :
           name == QueryTokenMessage.Every0Minutes ? miTSTruncStepMinute :
           name == QueryTokenMessage.SecondStart ? miTSTruncSecond :
           name == QueryTokenMessage.Every0Seconds ? miTSTruncStepSecond :
           throw new InvalidOperationException("Unexpected name");
    }

    private static MethodInfo GetMethodInfoTimeOnly(QueryTokenMessage name)
    {
        return
           name == QueryTokenMessage.TruncHours ? miTOTruncHour :
           name == QueryTokenMessage.Every0Hours ? miTOTruncStepHour :
           name == QueryTokenMessage.TruncMinutes ? miTOTruncMinute :
           name == QueryTokenMessage.Every0Minutes ? miTOTruncStepMinute :
           name == QueryTokenMessage.SecondStart ? miTOTruncSecond :
           name == QueryTokenMessage.Every0Seconds ? miTOTruncStepSecond :
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
                Name == QueryTokenMessage.TruncHours ? "g" :
                Name == QueryTokenMessage.Every0Hours? "g" :
                Name == QueryTokenMessage.TruncMinutes ? "g" :
                Name == QueryTokenMessage.Every0Minutes ? "g" :
                Name == QueryTokenMessage.SecondStart ? "G" :
                Name == QueryTokenMessage.Every0Seconds ? "G" :
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


    public static MethodInfo miDMonthStart = ReflectionTools.GetMethodInfo(() => DateTimeExtensions.MonthStart(DateOnly.MinValue));
    public static MethodInfo miDQuarterStart = ReflectionTools.GetMethodInfo(() => DateTimeExtensions.QuarterStart(DateOnly.MinValue));
    public static MethodInfo miDWeekStart = ReflectionTools.GetMethodInfo(() => DateTimeExtensions.WeekStart(DateOnly.MinValue));


    public static MethodInfo miDTMonthStart = ReflectionTools.GetMethodInfo(() => DateTimeExtensions.MonthStart(DateTime.MinValue));
    public static MethodInfo miDTQuarterStart = ReflectionTools.GetMethodInfo(() => DateTimeExtensions.QuarterStart(DateTime.MinValue));
    public static MethodInfo miDTWeekStart = ReflectionTools.GetMethodInfo(() => DateTimeExtensions.WeekStart(DateTime.MinValue));
    public static MethodInfo miDTTruncHour = ReflectionTools.GetMethodInfo(() => DateTimeExtensions.TruncHours(DateTime.MinValue));
    public static MethodInfo miDTTruncMinute = ReflectionTools.GetMethodInfo(() => DateTimeExtensions.TruncMinutes(DateTime.MinValue));
    public static MethodInfo miDTTruncSecond = ReflectionTools.GetMethodInfo(() => DateTimeExtensions.TruncSeconds(DateTime.MinValue));
    public static MethodInfo miDTTruncStepHour = ReflectionTools.GetMethodInfo(() => DateTimeExtensions.TruncHours(DateTime.MinValue, 2));
    public static MethodInfo miDTTruncStepMinute = ReflectionTools.GetMethodInfo(() => DateTimeExtensions.TruncMinutes(DateTime.MinValue, 2));
    public static MethodInfo miDTTruncStepSecond = ReflectionTools.GetMethodInfo(() => DateTimeExtensions.TruncSeconds(DateTime.MinValue, 2));

    public static MethodInfo miTSTruncHour = ReflectionTools.GetMethodInfo(() => TimeSpanExtensions.TruncHours(TimeSpan.Zero));
    public static MethodInfo miTSTruncMinute = ReflectionTools.GetMethodInfo(() => TimeSpanExtensions.TruncMinutes(TimeSpan.Zero));
    public static MethodInfo miTSTruncSecond = ReflectionTools.GetMethodInfo(() => TimeSpanExtensions.TruncSeconds(TimeSpan.Zero));
    public static MethodInfo miTSTruncStepHour = ReflectionTools.GetMethodInfo(() => TimeSpanExtensions.TruncHours(TimeSpan.Zero, 2));
    public static MethodInfo miTSTruncStepMinute = ReflectionTools.GetMethodInfo(() => TimeSpanExtensions.TruncMinutes(TimeSpan.Zero, 2));
    public static MethodInfo miTSTruncStepSecond = ReflectionTools.GetMethodInfo(() => TimeSpanExtensions.TruncSeconds(TimeSpan.Zero, 2));

    public static MethodInfo miTOTruncHour = ReflectionTools.GetMethodInfo(() => TimeOnlyExtensions.TruncHours(TimeOnly.MinValue));
    public static MethodInfo miTOTruncMinute = ReflectionTools.GetMethodInfo(() => TimeOnlyExtensions.TruncMinutes(TimeOnly.MinValue));
    public static MethodInfo miTOTruncSecond = ReflectionTools.GetMethodInfo(() => TimeOnlyExtensions.TruncSeconds(TimeOnly.MinValue));
    public static MethodInfo miTOTruncStepHour = ReflectionTools.GetMethodInfo(() => TimeOnlyExtensions.TruncHours(TimeOnly.MinValue, 2));
    public static MethodInfo miTOTruncStepMinute = ReflectionTools.GetMethodInfo(() => TimeOnlyExtensions.TruncMinutes(TimeOnly.MinValue, 2));
    public static MethodInfo miTOTruncStepSecond = ReflectionTools.GetMethodInfo(() => TimeOnlyExtensions.TruncSeconds(TimeOnly.MinValue, 2));

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
