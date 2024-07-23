using Signum.Utilities.Reflection;

namespace Signum.DynamicQuery.Tokens;

public class TimeSerieToken : QueryToken
{
    public override QueryToken? Parent => null;

    internal TimeSerieToken()
    {
        Priority = 8;
    }

    public override Type Type
    {
        get { return typeof(DateTime?); }
    }

    public override DateTimeKind DateTimeKind => DateTimeKind.Utc;

    public override string ToString()
    {
        return "[" + SystemTimeMode.TimeSerie.NiceToString() + "]";
    }

    public override string Key
    {
        get { return "TimeSerie"; }
    }

    static MethodInfo miSystemPeriod = ReflectionTools.GetMethodInfo((object o) => SystemTimeExtensions.SystemPeriod(null!));

    protected override Expression BuildExpressionInternal(BuildExpressionContext context)
    {
        throw new InvalidOperationException("TimeSerie token nof found in replacements");
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
        return SystemTimeMode.TimeSerie.NiceToString();
    }

    public override QueryToken Clone()
    {
        return new TimeSerieToken();
    }
}
