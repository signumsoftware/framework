
using Signum.Entities.Reflection;

namespace Signum.Entities.DynamicQuery;

/// <summary>
/// A container token for Entity operation tokens
/// </summary>
public class OperationsToken : QueryToken
{

    static readonly string _OperationsTokenKey = "[Operations]";
    
    QueryToken parent;

    public override QueryToken? Parent => parent;


    public OperationsToken(QueryToken parent)
    {
        if (!parent.Type.CleanType().IsIEntity())
            throw new InvalidOperationException("OperationsToken only can be child of entity type tokens");
        
        this.parent = parent ?? throw new ArgumentNullException(nameof(parent));
        this.key = _OperationsTokenKey;

    }

    public override string ToString()
    {
        return _OperationsTokenKey;
    }

    public override string NiceName()
    {
        return _OperationsTokenKey;
    }

/*    Type type;*/
    public override Type Type { get { return typeof(OperationsToken); } }

    string key;
    public override string Key { get { return key; } }

    public static Func<Type, IEnumerable<OperationSymbol>>? GetEligibleTypeOperations;
    protected override List<QueryToken> SubTokensOverride(SubTokensOptions options)
    {
        if (GetEligibleTypeOperations == null)
            throw new InvalidOperationException("OperationsToken.GetTypeOperations not set");


        return GetEligibleTypeOperations(parent.Type.CleanType())
            .Select(o => (QueryToken)new OperationToken(this, o.Key.Replace(".", "#"), parent.Type, o))
            .ToList();
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
        return new OperationsToken(this.parent.Clone());
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
