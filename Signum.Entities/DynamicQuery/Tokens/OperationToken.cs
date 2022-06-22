
using Signum.Entities.Reflection;

namespace Signum.Entities.DynamicQuery;

public class OperationToken : QueryToken
{
    QueryToken parent;

    public override QueryToken? Parent => parent;


    public OperationToken(QueryToken parent, string key, Type type, OperationSymbol operationSymbol)
    {
        if (!type.CleanType().IsIEntity())
            throw new InvalidOperationException("OperationToken type can be entity or lite");

        this.parent = parent;


        this.key= key;
        this.type = type;
        this.operation= operationSymbol;

    }

    OperationSymbol operation;

    public override string ToString()
    {
        return $"{type.CleanType().Name}.{key}";
    }

    public override string NiceName()
    {
        return operation.NiceToString();
    }

    Type type;
    public override Type Type { get { return type; } }

    string key;
    public override string Key { get { return key; } }

    public static Func<Type, string, Expression, Expression>? BuildExtension;

    protected override Expression BuildExpressionInternal(BuildExpressionContext context)
    {

        throw new NotImplementedException();
    }

    public static Func<OperationSymbol, Type, string?>? OperationAllowedInUI;
    public override string? IsAllowed()
    {
        if (OperationAllowedInUI == null)
            throw new InvalidOperationException("OperationToken.OperationAllowedInUI not set");

        return OperationAllowedInUI(operation, type);
    }

    public override QueryToken Clone()
    {
        return new OperationToken(this.parent.Clone(), key, type, operation);
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
