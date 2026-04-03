namespace Signum.DynamicQuery.Tokens;



/// <summary>
/// A container token for Entity operation tokens
/// </summary>
public class IndexerContainerToken : QueryToken
{
    IExtensionDictionaryInfo info;

    QueryToken parent;

    public override QueryToken? Parent => parent;

    public IndexerContainerToken(QueryToken parent, IExtensionDictionaryInfo info)
    {
        this.parent = parent ?? throw new ArgumentNullException(nameof(parent));
        this.info = info;
    }

    public override bool HideInAutoExpand => false;
    protected override bool AutoExpandInternal => this.info.AutoExpand;

    public override string ToString() => "[" + info.NiceName() + "]";

    public override string NiceName() => "[" + info.NiceName() + "]";

    public override Type Type => typeof(IndexerContainerToken);

    public override string Key => "[" + info.Prefix + "]";

    public override bool AvoidCacheSubTokens => true;
   
    protected override List<QueryToken> SubTokensOverride(SubTokensOptions options)
    {
        return info.GetAllTokens(this).ToList();
    }

    protected override Expression BuildExpressionInternal(BuildExpressionContext context)
    {
        return parent.BuildExpression(context);
    }

    public override string? IsAllowed() => info.GetAllowed(this);

    public override QueryToken Clone()
    {
        return new IndexerContainerToken(parent.Clone(), info);
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

    public override PropertyRoute? GetPropertyRoute()
    {
        return null;
    }

}

public class ExtensionWithParameterToken<T, K, V> : QueryToken
{
    QueryToken parent;
    public override QueryToken? Parent => parent;

    public ExtensionWithParameterToken(QueryToken parent, K parameterValue,
        string? unit,
        string? format,
        Implementations? implementations,
        PropertyRoute? propertyRoute,
        Expression<Func<T, V>> lambda)
    {
        ParameterValue = parameterValue;
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
        return "[" + (ParameterValue is Enum e ? e.NiceToString() : ParameterValue?.ToString() ?? "null") + "]";
    }

    public override string NiceName()
    {
        return (ParameterValue is Enum e ? e.NiceToString() : ParameterValue?.ToString() ?? "null");
    }

    public override Type Type { get { return typeof(V).BuildLiteNullifyUnwrapPrimaryKey(new[] { GetPropertyRoute()! }); } }

    public K ParameterValue { get; }
    public override string Key => "[" + (ParameterValue?.ToString() ?? "null") + "]";

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
        return new ExtensionWithParameterToken<T, K, V>(parent.Clone(), ParameterValue, unit, format, implementations, propertyRoute, Lambda);
    }
}
