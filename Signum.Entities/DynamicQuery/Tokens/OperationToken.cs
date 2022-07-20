
using Signum.Entities.Reflection;

namespace Signum.Entities.DynamicQuery;

public class OperationToken : QueryToken
{
    QueryToken parent;
    Type entityType;
    OperationSymbol operation;

    public override QueryToken? Parent => parent;

    public OperationToken(QueryToken parent, string key, Type entityType, OperationSymbol operationSymbol)
    {
        var cleanType = entityType.CleanType();

        if (!cleanType.IsIEntity())
            throw new InvalidOperationException("OperationToken, invalid entityType (should be entity or lite)");

        this.parent = parent;
        this.key = key;
        this.entityType = cleanType;
        this.operation = operationSymbol;

    }

    public override string ToString()
    {
        return operation.Key;
    }

    public override string NiceName()
    {
        return operation.NiceToString();
    }

    public override Type Type { get { return typeof(CellOperationDTO); } }

    string key;
    public override string Key { get { return key; } }

    public static Func<Type, OperationSymbol, Expression, Expression>? BuildExtension;

    protected override Expression BuildExpressionInternal(BuildExpressionContext context)
    {
        if (BuildExtension == null)
            throw new InvalidOperationException("OperationToken.BuildExtension not set");

        var parentExpression = parent.BuildExpression(context);

        var result = BuildExtension(entityType, operation, parentExpression);

        return result;
    }

    public static Func<OperationSymbol, Type, string?>? IsAllowedExtension;
    public override string? IsAllowed()
    {
        if (IsAllowedExtension == null)
            throw new InvalidOperationException("OperationToken.IsAllowedExtension not set");

        return IsAllowedExtension(operation, entityType);
    }

    public override QueryToken Clone()
    {
        return new OperationToken(this.parent.Clone(), key, entityType, operation);
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

public class CellOperationDTO
{
    public CellOperationDTO(Lite<IEntity> lite, string operationKey, string? canExecute)
    {
        Lite = lite;
        OperationKey = operationKey;
        CanExecute = canExecute;
    }
    public Lite<IEntity> Lite { get; set; }
    public string OperationKey { get; set; }
    public string? CanExecute { get; set; }

}

