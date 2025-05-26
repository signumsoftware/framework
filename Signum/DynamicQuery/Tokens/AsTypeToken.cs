namespace Signum.DynamicQuery.Tokens;


public class AsTypeToken : QueryToken
{
    QueryToken parent;
    public override QueryToken? Parent => parent;

    Type entityType;
    internal AsTypeToken(QueryToken parent, Type type)
    {
        this.parent = parent ?? throw new ArgumentNullException(nameof(parent));
        this.entityType = type ?? throw new ArgumentNullException(nameof(type));

        this.Priority = 8;
    }

    public override Type Type
    {
        get { return entityType.BuildLite(); }
    }

    public override string ToString()
    {
        var cleanType = EnumEntity.Extract(entityType) ?? entityType;
        return QueryTokenMessage.As0.NiceToString().FormatWith(cleanType.NiceName());
    }

    public override string Key
    {
        get { return "({0})".FormatWith(TypeLogic.GetCleanName(entityType)); }
    }

    protected override Expression BuildExpressionInternal(BuildExpressionContext context)
    {
        Expression baseExpression = parent.BuildExpression(context);

        Expression result = Expression.TypeAs(baseExpression.ExtractEntity(false), entityType);

        return result.BuildLite();
    }

    protected override List<QueryToken> SubTokensOverride(SubTokensOptions options)
    {
        return SubTokensBase(entityType, options, GetImplementations());
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
        return Implementations.By(entityType);
    }

    public override string? IsAllowed()
    {
        var parentAllowed = this.parent.IsAllowed();
        var routeAllowed = GetPropertyRoute()!.IsAllowed();

        if (parentAllowed.HasText() && routeAllowed.HasText())
            QueryTokenMessage.And.NiceToString().Combine(parentAllowed, routeAllowed);

        return parentAllowed ?? routeAllowed;
    }

    public override PropertyRoute? GetPropertyRoute()
    {
        return PropertyRoute.Root(entityType);
    }

    public override string NiceName()
    {
        var cleanType = EnumEntity.Extract(entityType) ?? entityType;
        return QueryTokenMessage._0As1.NiceToString().FormatWith(parent.ToString(), cleanType.NiceName());
    }

    public override QueryToken Clone()
    {
        return new AsTypeToken(parent.Clone(), entityType);
    }
}

