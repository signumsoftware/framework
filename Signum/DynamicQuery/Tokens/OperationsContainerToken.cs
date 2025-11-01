namespace Signum.DynamicQuery.Tokens;

/// <summary>
/// A container token for Entity operation tokens
/// </summary>
public class OperationsContainerToken : QueryToken
{
    QueryToken parent;

    public override QueryToken? Parent => parent;

    public OperationsContainerToken(QueryToken parent)
    {
        if (!parent.Type.CleanType().IsIEntity())
            throw new InvalidOperationException("OperationsToken only can be child of entity type tokens");

        this.parent = parent ?? throw new ArgumentNullException(nameof(parent));
    }

    public override bool HideInAutoExpand => true;

    protected override bool AutoExpandInternal => false;

    public override string ToString() => "[" + QueryTokenMessage.Operations.NiceToString() + "]";

    public override string NiceName() => "[" + QueryTokenMessage.Operations.NiceToString() + "]";

    /*    Type type;*/
    public override Type Type { get { return typeof(OperationsContainerToken); } }

    public override string Key => "[Operations]";

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
        return new OperationsContainerToken(parent.Clone());
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


