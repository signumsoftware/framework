using Signum.Utilities.Reflection;

namespace Signum.DynamicQuery.Tokens;

internal static class HasValueTokenExtensions
{
    internal static List<QueryToken> AndHasValue(this List<QueryToken> list, QueryToken parent)
    {
        list.Add(new HasValueToken(parent));
        return list;
    }

    internal static List<QueryToken> AndModuloTokens(this List<QueryToken> list, QueryToken parent)
    {
        list.AddRange(new List<QueryToken>
        {
            new ModuloToken(parent, 10),
            new ModuloToken(parent, 100),
            new ModuloToken(parent, 1000),
            new ModuloToken(parent, 10000),
        });
        return list;
    }
}

public class HasValueToken : QueryToken
{
    QueryToken parent;
    public override QueryToken? Parent => parent;

    internal HasValueToken(QueryToken parent)
    {
        this.parent = parent ?? throw new ArgumentNullException(nameof(parent));

        this.Priority = -1;
    }

    public override Type Type
    {
        get { return typeof(bool?); }
    }

    public override string ToString()
    {
        return "[" + QueryTokenMessage.HasValue + "]";
    }

    public override string Key
    {
        get { return "HasValue"; }
    }


    static readonly MethodInfo miAnyE = ReflectionTools.GetMethodInfo((IEnumerable<string> col) => col.Any()).GetGenericMethodDefinition();
    static readonly MethodInfo miAnyQ = ReflectionTools.GetMethodInfo((IQueryable<string> col) => col.Any()).GetGenericMethodDefinition();

    protected override Expression BuildExpressionInternal(BuildExpressionContext context)
    {
        Expression baseExpression = parent.BuildExpression(context);

        if (IsCollection(this.Parent!.Type))
        {
            var miGen = baseExpression.Type.IsInstantiationOf(typeof(IQueryable<>)) ? miAnyQ : miAnyE;
            return Expression.Call(miGen.MakeGenericMethod(baseExpression.Type.ElementType()!), baseExpression);
        }

        var result = Expression.NotEqual(baseExpression, Expression.Constant(null, baseExpression.Type.Nullify()));

        if (baseExpression.Type == typeof(string))
            result = Expression.And(result, Expression.NotEqual(baseExpression, Expression.Constant("")));

        return result.Nullify();
    }

    protected override List<QueryToken> SubTokensOverride(SubTokensOptions options)
    {
        return new List<QueryToken>();
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

    public override string? IsAllowed()
    {
        return parent.IsAllowed();
    }

    public override PropertyRoute? GetPropertyRoute()
    {
        return null; ;
    }

    public override string NiceName()
    {
        return QueryTokenMessage._0HasValue.NiceToString(parent.ToString());
    }

    public override QueryToken Clone()
    {
        return new HasValueToken(parent.Clone());
    }
}

