
using Signum.Entities.Reflection;

namespace Signum.Entities.DynamicQuery;

/// <summary>
/// The abstract class for container of manual tokens
/// </summary>
public abstract class ManualContainerToken : QueryToken
{

    internal abstract string GetTokenKey();
    
    public QueryToken parent;

    public override QueryToken? Parent => parent;


    protected ManualContainerToken(QueryToken parent)
    {
        if (!parent.Type.CleanType().IsIEntity())
            throw new InvalidOperationException("ManualContainer tokens only can be child of an entity type token");
        
        this.parent = parent ?? throw new ArgumentNullException(nameof(parent));
        this.key = GetTokenKey();

    }

    public override string ToString()
    {
        return GetTokenKey();
    }

    public override string NiceName()
    {
        return GetTokenKey();
    }

    public override Type Type { get { return typeof(ManualContainerToken); } }

    string key;
    public override string Key { get { return key; } }

    protected override List<QueryToken> SubTokensOverride(SubTokensOptions options)
    {
        return new List<QueryToken>();
    }

    protected override Expression BuildExpressionInternal(BuildExpressionContext context)
    {
        return parent.BuildExpression(context);
    }

    public override string? IsAllowed()
    {
        return parent.IsAllowed();
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
