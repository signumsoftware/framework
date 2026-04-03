using Signum.Utilities.Reflection;

namespace Signum.DynamicQuery.Tokens;


public class EntityTypeToken : QueryToken
{
    QueryToken parent;
    public override QueryToken? Parent => parent;

    internal EntityTypeToken(QueryToken parent)
    {
        this.parent = parent ?? throw new ArgumentNullException(nameof(parent));

        Priority = 10;
    }

    public override Type Type
    {
        get { return typeof(TypeEntity).BuildLite(); }
    }

    public override string ToString()
    {
        return "[" + QueryTokenMessage.EntityType.NiceToString() + "]";
    }

    public override string Key
    {
        get { return "[EntityType]"; }
    }

    static MethodInfo miTypeEntity = ReflectionTools.GetMethodInfo(() => TypeLogic.ToTypeEntity(null!));

    protected override Expression BuildExpressionInternal(BuildExpressionContext context)
    {
        Expression baseExpression = parent.BuildExpression(context);

        Expression entityType = Expression.Property(baseExpression, "EntityType");

        Expression typeEntity = Expression.Call(miTypeEntity, entityType);

        return typeEntity.BuildLite();
    }

    protected override List<QueryToken> SubTokensOverride(SubTokensOptions options)
    {
        return SubTokensBase(typeof(TypeEntity), options, GetImplementations());
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
        return Implementations.By(typeof(TypeEntity));
    }

    public override string? IsAllowed()
    {
        var parentAllowed = parent.IsAllowed();
        var routeAllowed = GetPropertyRoute()!.IsAllowed();

        if (parentAllowed.HasText() && routeAllowed.HasText())
            QueryTokenMessage.And.NiceToString().Combine(parentAllowed, routeAllowed);

        return parentAllowed ?? routeAllowed;
    }

    public override PropertyRoute? GetPropertyRoute()
    {
        return PropertyRoute.Root(typeof(TypeEntity));
    }

    public override string NiceName()
    {
        return QueryTokenMessage._0Of1.NiceToString().FormatWith(QueryTokenMessage.EntityType.NiceToString(), parent.ToString());
    }

    public override QueryToken Clone()
    {
        return new EntityTypeToken(parent.Clone());
    }
}

