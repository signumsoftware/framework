using Signum.Utilities.Reflection;

namespace Signum.DynamicQuery.Tokens;

public class SystemTimeToken : QueryToken
{
    QueryToken parent;
    public override QueryToken? Parent => parent;

    SystemTimeProperty property;
    internal SystemTimeToken(QueryToken parent, SystemTimeProperty property)
    {
        Priority = 8;
        this.property = property;
        this.parent = parent;
    }

    public override Type Type
    {
        get { return typeof(DateTime?); }
    }

    public override DateTimeKind DateTimeKind => DateTimeKind.Utc;

    public override string ToString()
    {
        return "[" + this.property.NiceToString() + "]";
    }

    public override string Key
    {
        get { return this.property.ToString(); }
    }
    static MethodInfo miSystemPeriod = ReflectionTools.GetMethodInfo((object o) => SystemTimeExtensions.SystemPeriod(null!));

    protected override Expression BuildExpressionInternal(BuildExpressionContext context)
    {
        var result = parent.BuildExpression(context).ExtractEntity(false);

        var period = Expression.Call(miSystemPeriod, result.UnNullify());

        return Expression.Property(period, property == SystemTimeProperty.SystemValidFrom ? "Min" : "Max");
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
        return parent.IsAllowed();
    }

    public override PropertyRoute? GetPropertyRoute()
    {
        return null;
    }

    public override string NiceName()
    {
        return this.property.NiceToString();
    }

    public override QueryToken Clone()
    {
        return new SystemTimeToken(parent.Clone(), this.property);
    }
}
