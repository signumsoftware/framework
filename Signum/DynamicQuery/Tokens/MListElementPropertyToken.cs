namespace Signum.DynamicQuery.Tokens;

public class MListElementPropertyToken : QueryToken
{


    public PropertyInfo PropertyInfo { get; private set; }

    public PropertyRoute PropertyRoute { get; private set; }

    Func<string> nicePropertyName;

    QueryToken parent;
    public override QueryToken? Parent => parent;
   
    public static MListElementPropertyToken RowId(QueryToken parent, EntityPropertyToken ept)
    {
        var mleType = MListElementPropertyToken.MListElementType(ept);
        return new MListElementPropertyToken(parent, mleType.GetProperty("RowId")!, ept.PropertyRoute, "RowId", () => QueryTokenMessage.RowId.NiceToString()) { Priority = -5 };
    }

    public static MListElementPropertyToken RowOrder(QueryToken parent, EntityPropertyToken ept)
    {
        var mleType = MListElementPropertyToken.MListElementType(ept);
        return new MListElementPropertyToken(parent, mleType.GetProperty("RowOrder")!, ept.PropertyRoute, "RowOrder", () => QueryTokenMessage.RowOrder.NiceToString()) { Priority = -5 };
    }

    internal MListElementPropertyToken(QueryToken parent, PropertyInfo pi, PropertyRoute pr, string key, Func<string> nicePropertyName)
    {
        this.parent = parent ?? throw new ArgumentNullException(nameof(parent));
        if (parent is not (CollectionAnyAllToken or CollectionElementToken or CollectionToArrayToken))
            throw new InvalidOperationException("Unexpected parent");

        PropertyInfo = pi ?? throw new ArgumentNullException(nameof(pi));
        PropertyRoute = pr;
        this.nicePropertyName = nicePropertyName;
        this.key = key;
    }

    public override Type Type
    {
        get { return PropertyInfo.PropertyType.BuildLiteNullifyUnwrapPrimaryKey(new[] { GetPropertyRoute()! }); }
    }

    public override string ToString()
    {
        return "[" + nicePropertyName() + "]";
    }

    string key;
    public override string Key => key;

    protected override Expression BuildExpressionInternal(BuildExpressionContext context)
    {
        var baseExpression = context.Replacements.GetOrThrow(parent).RawExpression;

        return Expression.Property(baseExpression, PropertyInfo.Name).BuildLiteNullifyUnwrapPrimaryKey(new[] { PropertyRoute }); // Late binding over Lite or Identifiable
    }

    protected override List<QueryToken> SubTokensOverride(SubTokensOptions options)
    {
        return SubTokensBase(Type, options, GetImplementations());
    }

    public override Implementations? GetImplementations()
    {
        return GetPropertyRoute()!.TryGetImplementations();
    }

    public override string? Format
    {
        get { return Reflector.FormatString(Type); }
    }

    public override string? Unit
    {
        get { return null; }
    }

    public override string? IsAllowed()
    {
        return parent.IsAllowed();
    }

    public override PropertyRoute? GetPropertyRoute()
    {
        return PropertyRoute;
    }

    public override string NiceName()
    {
        return QueryTokenMessage._0Of1.NiceToString(nicePropertyName(), Parent!.Parent!.ToString());
    }

    public override QueryToken Clone()
    {
        return new MListElementPropertyToken(parent.Clone(), PropertyInfo, PropertyRoute, Key, nicePropertyName);
    }

    internal static Func<PropertyRoute, Type, bool> HasAttribute = null!;


    internal static MethodInfo miMListElementsLite = null!;


    public static Expression BuildMListElements(EntityPropertyToken ept, BuildExpressionContext ctx)
    {
        //user.Friends
        //user.MListElement<UserEntity, UserEntity>(u => u.Friends)

        var entityParent = FindEntityParent(ept)!;
        var entityParentType = entityParent.Type.CleanType();

        var parentExpr = entityParent.BuildExpression(ctx);

        var param = Expression.Parameter(entityParentType, entityParentType.Name.Substring(0, 1).ToLower());

        var ctxTemp = new BuildExpressionContext(ctx.ElementType, ctx.Parameter, new Dictionary<QueryToken, ExpressionBox>
        {
            {  entityParent, new ExpressionBox(param)}
        }, ctx.Filters);

        var lambda = Expression.Lambda(ept.BuildExpression(ctxTemp), param);

        var mi = miMListElementsLite.MakeGenericMethod(entityParentType, ept.Type.ElementType()!);

        return Expression.Call(mi, parentExpr, lambda);
    }

    public static EntityPropertyToken? AsMListEntityProperty(QueryToken token)
    {
        return token is EntityPropertyToken ept && ept.Type.IsMList() && !HasAttribute(ept.PropertyRoute, typeof(IgnoreAttribute)) /*VirtualMList*/ && FindEntityParent(ept) != null ? ept : null;
    }

    public static Type MListElementType(EntityPropertyToken eptML)
    {
        return typeof(MListElement<,>).MakeGenericType(FindEntityParent(eptML)!.Type.CleanType(), eptML.Type.ElementType()!);
    }

    public static QueryToken? FindEntityParent(EntityPropertyToken ept)
    {
        if (ept.Parent!.Type.CleanType().IsEntity())
            return ept.Parent;

        if (ept.Parent is EntityPropertyToken ept2)
            return FindEntityParent(ept2);

        return null;
    }
}
