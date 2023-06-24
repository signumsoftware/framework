
using Signum.Entities.Reflection;

namespace Signum.Entities.DynamicQuery;

public class QuickLinkToken : QueryToken
{
    QueryToken parent;
    Type entityType;

    public override QueryToken? Parent => parent;

    public QuickLinkToken(QueryToken parent, string key, Type entityType)
    {
        var cleanType = entityType.CleanType();

        if (!cleanType.IsIEntity())
            throw new InvalidOperationException("QuickLinkToken, invalid entityType (should be entity or lite)");

        this.parent = parent;
        this.key = key;
        this.entityType = cleanType;

    }

    public override string ToString()
    {
        return Key;
    }

    public override string NiceName()
    {
        return Key;
    }

    public override Type Type { get { return typeof(CellQuickLinkDTO); } }

    string key;
    public override string Key { get { return key; } }

    protected override Expression BuildExpressionInternal(BuildExpressionContext context)
    {

        var parentExpression = parent.BuildExpression(context);
        var entity = parentExpression.ExtractEntity(false);
        var quickLinkKey = Expression.Constant(key);

        var dtoConstructor = typeof(CellQuickLinkDTO).GetConstructor(new[] { typeof(Lite<IEntity>), typeof(string) });

        NewExpression newExpr = Expression.New(dtoConstructor!, entity.BuildLite(), quickLinkKey);

        return newExpr;
    }

    public override string? IsAllowed()
    {
        return parent.IsAllowed();
    }

    public override QueryToken Clone()
    {
        return new QuickLinkToken(this.parent.Clone(), key, entityType);
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

    protected override List<QueryToken> SubTokensOverride(SubTokensOptions options)
    {
        return new List<QueryToken>();
    }
}

public class CellQuickLinkDTO
{
    public CellQuickLinkDTO(Lite<IEntity> lite, string quickLinkKey)
    {
        Lite = lite;
        QuickLinkKey = quickLinkKey;
    }
    public Lite<IEntity> Lite { get; set; }
    public string QuickLinkKey { get; set; }

}

