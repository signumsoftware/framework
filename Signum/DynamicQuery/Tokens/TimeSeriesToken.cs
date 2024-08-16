using Signum.Utilities.Reflection;

namespace Signum.DynamicQuery.Tokens;

public class TimeSeriesToken : QueryToken
{
    public override QueryToken? Parent => null;

    internal TimeSeriesToken(object queryName)
    {
        this.queryName = queryName;
        Priority = 8;
    }

    public override Type Type
    {
        get { return typeof(DateTime); }
    }

    public override DateTimeKind DateTimeKind => DateTimeKind.Utc;

    public override string ToString()
    {
        return "[" + SystemTimeMode.TimeSeries.NiceToString() + "]";
    }

    public const string KeyText = "TimeSeries";

    public override string Key
    {
        get { return KeyText; }
    }

    object queryName;
    public override object QueryName
    {
        get { return queryName; }
    }

    static MethodInfo miSystemPeriod = ReflectionTools.GetMethodInfo((object o) => SystemTimeExtensions.SystemPeriod(null!));

    protected override Expression BuildExpressionInternal(BuildExpressionContext context)
    {
        throw new InvalidOperationException("TimeSeries token nof found in replacements");
    }

    protected override List<QueryToken> SubTokensOverride(SubTokensOptions options)
    {
        return SubTokensBase(typeof(DateTime), options, GetImplementations());
    }

    public override Implementations? GetImplementations()
    {
        return null;
    }

    public override string? Format
    {
        get { return "G"; }
    }

    public override string? Unit
    {
        get { return null; }
    }

    public override string? IsAllowed()
    {
        return null;
    }

    public override PropertyRoute? GetPropertyRoute()
    {
        return null;
    }

    public override string NiceName()
    {
        return SystemTimeMode.TimeSeries.NiceToString();
    }

    public override QueryToken Clone()
    {
        return new TimeSeriesToken(this.queryName);
    }
}
