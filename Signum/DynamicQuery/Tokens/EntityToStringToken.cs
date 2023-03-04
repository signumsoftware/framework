using Signum.Utilities.Reflection;

namespace Signum.DynamicQuery.Tokens;

public class EntityToStringToken : QueryToken
{
    readonly QueryToken parent;
    public override QueryToken? Parent => parent;

    internal EntityToStringToken(QueryToken parent)
    {
        Priority = 9;
        this.parent = parent;
    }

    public override Type Type
    {
        get { return typeof(string); }
    }

    public override string ToString()
    {
        return "[" + LiteMessage.ToStr.NiceToString() + "]";
    }

    public override string Key
    {
        get { return "ToString"; }
    }

    static MethodInfo miToString = ReflectionTools.GetMethodInfo((object o) => o.ToString());
    static PropertyInfo miToStringProperty = ReflectionTools.GetPropertyInfo((Entity o) => o.ToStringProperty);

    protected override Expression BuildExpressionInternal(BuildExpressionContext context)
    {
        var baseExpression = parent.BuildExpression(context);

        return Expression.Call(baseExpression, miToString);
    }

    protected override List<QueryToken> SubTokensOverride(SubTokensOptions options)
    {
        return SubTokensBase(typeof(string), options, GetImplementations());
    }

    public override Implementations? GetImplementations()
    {
        return null;
    }

    public override string? Format
    {
        get { return null; }
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
        Type? type = Lite.Extract(parent.GetPropertyRoute()?.Type ?? parent.Type);
        if (type != null)
            return PropertyRoute.Root(type).Add(miToStringProperty);

        return null;
    }

    public override string NiceName()
    {
        return QueryTokenMessage._0Of1.NiceToString(LiteMessage.ToStr.NiceToString(), parent.ToString());
    }

    public override QueryToken Clone()
    {
        return new EntityToStringToken(parent.Clone());
    }
}
