
using Signum.Entities.Reflection;

namespace Signum.Entities.DynamicQuery;

/// <summary>
/// A container token for Entity quick link tokens
/// </summary>
public class QuickLinksToken : QueryToken
{

    static readonly string _QuickLinksTokenKey = "[QuickLinks]";
    
    QueryToken parent;

    public override QueryToken? Parent => parent;


    public QuickLinksToken(QueryToken parent)
    {
        if (!parent.Type.CleanType().IsIEntity())
            throw new InvalidOperationException("QuickLinksToken only can be child of entity type tokens");
        
        this.parent = parent ?? throw new ArgumentNullException(nameof(parent));
        this.key = _QuickLinksTokenKey;

    }

    public override string ToString()
    {
        return _QuickLinksTokenKey;
    }

    public override string NiceName()
    {
        return _QuickLinksTokenKey;
    }

/*    Type type;*/
    public override Type Type { get { return typeof(QuickLinksToken); } }

    string key;
    public override string Key { get { return key; } }

    protected override List<QueryToken> SubTokensOverride(SubTokensOptions options)
    {
        return new List<QueryToken>();
    }

    public override bool IsManual => true;
    protected override QueryToken GetManualSubToken(string key)
    {
        return new QuickLinkToken(this, key, parent.Type);
    }

    protected override Expression BuildExpressionInternal(BuildExpressionContext context)
    {
        return parent.BuildExpression(context);
    }

    public override string? IsAllowed()
    {
        return parent.IsAllowed();
    }

    public override QueryToken Clone()
    {
        return new QuickLinksToken(this.parent.Clone());
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
