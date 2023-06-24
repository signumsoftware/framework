
using Signum.Entities.Reflection;

namespace Signum.Entities.DynamicQuery;

public class ManualToken : QueryToken
{
    ManualContainerToken parent;
    Type entityType;

    public override QueryToken? Parent => parent;

    public ManualToken(ManualContainerToken parent, string key, Type entityType)
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

    public override Type Type { get { return typeof(ManualCellDTO); } }

    string key;
    public override string Key { get { return key; } }

    protected override Expression BuildExpressionInternal(BuildExpressionContext context)
    {

        var parentExpression = parent.BuildExpression(context);
        var entity = parentExpression.ExtractEntity(false);
        var containerTokenKey = Expression.Constant(parent.GetTokenKey());
        var tokenKey = Expression.Constant(key);

        var dtoConstructor = typeof(ManualCellDTO).GetConstructor(new[] { typeof(Lite<IEntity>), typeof(string) , typeof(string) });

        NewExpression newExpr = Expression.New(dtoConstructor!, entity.BuildLite(), containerTokenKey, tokenKey);

        return newExpr;
    }

    public override string? IsAllowed()
    {
        return parent.IsAllowed();
    }

    public override ManualToken Clone()
    {
        return new ManualToken((ManualContainerToken)this.parent.Clone(), key, entityType);
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

public class ManualCellDTO
{
    public ManualCellDTO(Lite<IEntity> lite, string manualContainerTokenKey, string manualTokenKey)
    {
        Lite = lite;
        ManualContainerTokenKey = manualContainerTokenKey;
        ManualTokenKey = manualTokenKey;
    }
    public Lite<IEntity> Lite { get; set; }
    public string ManualContainerTokenKey { get; set; }
    public string ManualTokenKey { get; set; }

}

