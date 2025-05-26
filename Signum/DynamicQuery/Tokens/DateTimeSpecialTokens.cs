using Signum.Utilities.Reflection;

namespace Signum.DynamicQuery.Tokens;

public class DatePartStartToken : QueryToken
{
    public QueryTokenDateMessage Name { get; private set; }
    public int? Step { get; private set; }
    QueryToken parent;
    public override QueryToken? Parent => parent;

    internal DatePartStartToken(QueryToken parent, QueryTokenDateMessage name, int? step = null)
    {
        this.Name = name;
        this.parent = parent ?? throw new ArgumentNullException(nameof(parent));
        if (name is
            QueryTokenDateMessage.Every0Hours or
            QueryTokenDateMessage.Every0Minutes or
            QueryTokenDateMessage.Every0Seconds or
            QueryTokenDateMessage.Every0Milliseconds)
        {
            this.Step = step!.Value;
        }

        Priority = -((int)name) * 1000 + (step ?? 0);
    }

    private static MethodInfo GetMethodInfoDateTime(QueryTokenDateMessage name)
    {
        return
            name == QueryTokenDateMessage.MonthStart ? miDTMonthStart :
            name == QueryTokenDateMessage.QuarterStart ? miDTQuarterStart :
            name == QueryTokenDateMessage.WeekStart ? miDTWeekStart :
            name == QueryTokenDateMessage.HourStart ? miDTTruncHour :
            name == QueryTokenDateMessage.Every0Hours ? miDTTruncStepHour :
            name == QueryTokenDateMessage.MinuteStart ? miDTTruncMinute :
            name == QueryTokenDateMessage.Every0Minutes ? miDTTruncStepMinute :
            name == QueryTokenDateMessage.SecondStart ? miDTTruncSecond :
            name == QueryTokenDateMessage.Every0Seconds ? miDTTruncStepSecond :
            name == QueryTokenDateMessage.Every0Milliseconds ? miDTTruncStepMillisecond :
            throw new InvalidOperationException("Unexpected name");
    }

    private static MethodInfo GetMethodInfoDateOnly(QueryTokenDateMessage name)
    {
        return
            name == QueryTokenDateMessage.MonthStart ? miDMonthStart :
            name == QueryTokenDateMessage.QuarterStart ? miDQuarterStart :
            name == QueryTokenDateMessage.WeekStart ? miDWeekStart :
            throw new InvalidOperationException("Unexpected name");
    }

    private static MethodInfo GetMethodInfoTimeSpan(QueryTokenDateMessage name)
    {
        return
           name == QueryTokenDateMessage.HourStart ? miTSTruncHour :
           name == QueryTokenDateMessage.Every0Hours ? miTSTruncStepHour :
           name == QueryTokenDateMessage.MinuteStart ? miTSTruncMinute :
           name == QueryTokenDateMessage.Every0Minutes ? miTSTruncStepMinute :
           name == QueryTokenDateMessage.SecondStart ? miTSTruncSecond :
           name == QueryTokenDateMessage.Every0Seconds ? miTSTruncStepSecond :
           name == QueryTokenDateMessage.Every0Milliseconds ? miTSTruncStepMillisecond :
           throw new InvalidOperationException("Unexpected name");
    }

    private static MethodInfo GetMethodInfoTimeOnly(QueryTokenDateMessage name)
    {
        return
           name == QueryTokenDateMessage.HourStart ? miTOTruncHour :
           name == QueryTokenDateMessage.Every0Hours ? miTOTruncStepHour :
           name == QueryTokenDateMessage.MinuteStart ? miTOTruncMinute :
           name == QueryTokenDateMessage.Every0Minutes ? miTOTruncStepMinute :
           name == QueryTokenDateMessage.SecondStart ? miTOTruncSecond :
           name == QueryTokenDateMessage.Every0Seconds ? miTOTruncStepSecond :
           name == QueryTokenDateMessage.Every0Milliseconds ? miTOTruncStepMillisecond :
           throw new InvalidOperationException("Unexpected name");
    }


    public override string ToString()
    {
        if (Step != null)
            return this.Name.NiceToString(Step);

        return this.Name.NiceToString();
    }

    public override string NiceName()
    {
        return QueryTokenMessage._0Of1.NiceToString(this.ToString(), parent.ToString());
    }

    public override string? Format
    {
        get
        {
            return
                Name == QueryTokenDateMessage.MonthStart ? "Y" :
                Name == QueryTokenDateMessage.QuarterStart ? "d" :
                Name == QueryTokenDateMessage.WeekStart ? "d" :
                Name == QueryTokenDateMessage.HourStart ? "g" :
                Name == QueryTokenDateMessage.Every0Hours? "g" :
                Name == QueryTokenDateMessage.MinuteStart ? "g" :
                Name == QueryTokenDateMessage.Every0Minutes ? "g" :
                Name == QueryTokenDateMessage.SecondStart ? "G" :
                Name == QueryTokenDateMessage.Every0Seconds ? "G" :
                Name == QueryTokenDateMessage.Every0Milliseconds ? "G" :
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
        get
        {
            if (this.Step != null)
                return this.Name.ToString().Replace("0", this.Step.ToString());

            return this.Name.ToString();
        }
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
    public static MethodInfo miDTTruncStepMillisecond = ReflectionTools.GetMethodInfo(() => DateTimeExtensions.TruncMilliseconds(DateTime.MinValue, 100));

    public static MethodInfo miTSTruncHour = ReflectionTools.GetMethodInfo(() => TimeSpanExtensions.TruncHours(TimeSpan.Zero));
    public static MethodInfo miTSTruncMinute = ReflectionTools.GetMethodInfo(() => TimeSpanExtensions.TruncMinutes(TimeSpan.Zero));
    public static MethodInfo miTSTruncSecond = ReflectionTools.GetMethodInfo(() => TimeSpanExtensions.TruncSeconds(TimeSpan.Zero));
    public static MethodInfo miTSTruncStepHour = ReflectionTools.GetMethodInfo(() => TimeSpanExtensions.TruncHours(TimeSpan.Zero, 2));
    public static MethodInfo miTSTruncStepMinute = ReflectionTools.GetMethodInfo(() => TimeSpanExtensions.TruncMinutes(TimeSpan.Zero, 2));
    public static MethodInfo miTSTruncStepSecond = ReflectionTools.GetMethodInfo(() => TimeSpanExtensions.TruncSeconds(TimeSpan.Zero, 2));
    public static MethodInfo miTSTruncStepMillisecond = ReflectionTools.GetMethodInfo(() => TimeSpanExtensions.TruncMilliseconds(TimeSpan.Zero, 100));

    public static MethodInfo miTOTruncHour = ReflectionTools.GetMethodInfo(() => TimeOnlyExtensions.TruncHours(TimeOnly.MinValue));
    public static MethodInfo miTOTruncMinute = ReflectionTools.GetMethodInfo(() => TimeOnlyExtensions.TruncMinutes(TimeOnly.MinValue));
    public static MethodInfo miTOTruncSecond = ReflectionTools.GetMethodInfo(() => TimeOnlyExtensions.TruncSeconds(TimeOnly.MinValue));
    public static MethodInfo miTOTruncStepHour = ReflectionTools.GetMethodInfo(() => TimeOnlyExtensions.TruncHours(TimeOnly.MinValue, 2));
    public static MethodInfo miTOTruncStepMinute = ReflectionTools.GetMethodInfo(() => TimeOnlyExtensions.TruncMinutes(TimeOnly.MinValue, 2));
    public static MethodInfo miTOTruncStepSecond = ReflectionTools.GetMethodInfo(() => TimeOnlyExtensions.TruncSeconds(TimeOnly.MinValue, 2));
    public static MethodInfo miTOTruncStepMillisecond = ReflectionTools.GetMethodInfo(() => TimeOnlyExtensions.TruncMilliseconds(TimeOnly.MinValue, 100));

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

        if(Step != null)
            return Expression.Call(mi, exp.UnNullify(), Expression.Constant(Step.Value)).Nullify();

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
