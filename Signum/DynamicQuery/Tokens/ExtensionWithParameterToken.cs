namespace Signum.DynamicQuery.Tokens;

public class ExtensionWithParameterToken<T, K, V> : QueryToken
{
    QueryToken parent;
    public override QueryToken? Parent => parent;

    public ExtensionWithParameterToken(QueryToken parent, K parameterValue,
        string prefix,
        string? unit,
        string? format,
        Implementations? implementations,
        PropertyRoute? propertyRoute,
        Expression<Func<T, V>> lambda)
    {
        ParameterValue = parameterValue;
        Prefix = prefix;
        this.unit = unit;
        this.format = format;
        this.implementations = implementations;
        this.propertyRoute = propertyRoute;
        Priority = -10;
        Lambda = lambda;
        this.parent = parent;
    }

    public override string ToString()
    {
        return Prefix + "[" + (ParameterValue is Enum e ? e.NiceToString() : ParameterValue?.ToString() ?? "null") + "]";
    }

    public override string NiceName()
    {
        return (Prefix.Length > 0 ? $"({Prefix}) " : "") + (ParameterValue is Enum e ? e.NiceToString() : ParameterValue?.ToString() ?? "null");
    }

    public override Type Type { get { return typeof(V).BuildLiteNullifyUnwrapPrimaryKey(new[] { GetPropertyRoute()! }); } }

    public string Prefix { get; set; }
    public K ParameterValue { get; }
    public override string Key => Prefix + "[" + (ParameterValue?.ToString() ?? "null") + "]";

    string? format;
    public override string? Format => format;

    string? unit;
    public override string? Unit => unit;

    protected override List<QueryToken> SubTokensOverride(SubTokensOptions options)
    {
        return SubTokensBase(typeof(V), options, implementations);
    }

    public Expression<Func<T, V>> Lambda;

    protected override Expression BuildExpressionInternal(BuildExpressionContext context)
    {
        var parentExpression = parent.BuildExpression(context).ExtractEntity(false).UnNullify();

        var result = Expression.Invoke(Lambda, parentExpression);

        return result.BuildLiteNullifyUnwrapPrimaryKey(new[] { propertyRoute! });
    }

    public PropertyRoute? propertyRoute;
    public override PropertyRoute? GetPropertyRoute() => propertyRoute;

    public Implementations? implementations;
    public override Implementations? GetImplementations() => implementations;

    public override string? IsAllowed()
    {
        string? parentAllowed = parent.IsAllowed();

        string? routeAlllowed = GetPropertyRoute()?.IsAllowed();

        if (parentAllowed.HasText() && routeAlllowed.HasText())
            return QueryTokenMessage.And.NiceToString().Combine(parentAllowed!, routeAlllowed!);

        return parentAllowed ?? routeAlllowed;
    }

    public override QueryToken Clone()
    {
        return new ExtensionWithParameterToken<T, K, V>(parent.Clone(), ParameterValue, Prefix, unit, format, implementations, propertyRoute, Lambda);
    }
}
