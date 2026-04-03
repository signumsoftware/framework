using Signum.Utilities.Reflection;

namespace Signum.DynamicQuery.Tokens;

public class CountToken : QueryToken
{
    QueryToken parent;
    public override QueryToken? Parent => parent;

    internal CountToken(QueryToken parent)
    {
        this.parent = parent ?? throw new ArgumentNullException(nameof(parent));
    }

    public override bool HideInAutoExpand => true;

    public override Type Type
    {
        get { return typeof(int?); }
    }

    public override string ToString()
    {
        return QueryTokenMessage.Count.NiceToString();
    }

    public override string Key
    {
        get { return "Count"; }
    }

    static MethodInfo miCount = ReflectionTools.GetMethodInfo((IEnumerable<string> q) => q.Count()).GetGenericMethodDefinition();

    protected override Expression BuildExpressionInternal(BuildExpressionContext context)
    {
        var parentResult = parent.BuildExpression(context);

        var result = Expression.Call(miCount.MakeGenericMethod(parentResult.Type.ElementType()!), parentResult);

        return result.Nullify();
    }

    protected override List<QueryToken> SubTokensOverride(SubTokensOptions options)
    {
        return new List<QueryToken>();
    }

    public override string? Format
    {
        get { return null; }
    }

    public override string? Unit
    {
        get { return null; }
    }

    public override Implementations? GetImplementations()
    {
        return null;
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
        return QueryTokenMessage._0Of1.NiceToString(ToString(), parent.ToString());
    }

    public override QueryToken Clone()
    {
        return new CountToken(parent.Clone());
    }
}
