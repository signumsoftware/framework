using Signum.Utilities.Reflection;

namespace Signum.DynamicQuery.Tokens;

public class DateToken : QueryToken
{
    QueryToken parent;
    public override QueryToken? Parent => parent;

    internal DateToken(QueryToken parent)
    {
        this.parent = parent ?? throw new ArgumentNullException(nameof(parent));
    }

    public override string ToString()
    {
        return QueryTokenDateMessage.Date.NiceToString();
    }

    public override string NiceName()
    {
        return QueryTokenMessage._0Of1.NiceToString(QueryTokenDateMessage.Date.NiceToString(), parent.ToString());
    }

    public override string? Format
    {
        get { return "d"; }
    }

    public override string? Unit
    {
        get { return null; }
    }

    public override Type Type
    {
        get { return typeof(DateOnly?); }
    }

    public override string Key
    {
        get { return "Date"; }
    }

    protected override List<QueryToken> SubTokensOverride(SubTokensOptions options)
    {
        return new List<QueryToken>();
    }

    static MethodInfo miDate = ReflectionTools.GetMethodInfo((DateTime? d) => d.ToDateOnly());

    protected override Expression BuildExpressionInternal(BuildExpressionContext context)
    {
        var exp = parent.BuildExpression(context);

        return Expression.Call(miDate, exp);
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
        return new DateToken(parent.Clone());
    }

    public override bool IsGroupable
    {
        get { return true; }
    }
}
