using Signum.Entities.Reflection;
using Signum.Utilities.Reflection;

namespace Signum.Entities.DynamicQuery;

public class MListElementPropertyToken : QueryToken
{


    public PropertyInfo PropertyInfo { get; private set; }

    public PropertyRoute PropertyRoute { get; private set; }

    Func<string> nicePropertyName;

    QueryToken parent;
    public override QueryToken? Parent => parent;
   
   internal MListElementPropertyToken(QueryToken parent, PropertyInfo pi, PropertyRoute pr, string key, Func<string> nicePropertyName)
    {
        this.parent = parent ?? throw new ArgumentNullException(nameof(parent));
        if (parent is not (CollectionAnyAllToken or CollectionElementToken))
            throw new InvalidOperationException("Unexpected parent");

        this.PropertyInfo = pi ?? throw new ArgumentNullException(nameof(pi));
        this.PropertyRoute = pr;
        this.nicePropertyName = nicePropertyName;
        this.key = key;
    }

    public override Type Type
    {
        get { return PropertyInfo.PropertyType.BuildLiteNullifyUnwrapPrimaryKey(new[] { this.GetPropertyRoute()! }); }
    }

    public override string ToString()
    {
        return "[" + nicePropertyName() + "]";
    }

    string key;
    public override string Key => key;

    protected override Expression BuildExpressionInternal(BuildExpressionContext context)
    {
        var baseExpression = context.Replacements.GetOrThrow(this.parent).RawExpression;

        return Expression.Property(baseExpression, PropertyInfo.Name).BuildLiteNullifyUnwrapPrimaryKey(new[] { this.PropertyRoute }); // Late binding over Lite or Identifiable
    }

    protected override List<QueryToken> SubTokensOverride(SubTokensOptions options)
    {
        return SubTokensBase(this.Type, options, GetImplementations());
    }

    public override Implementations? GetImplementations()
    {
        return GetPropertyRoute()!.TryGetImplementations();
    }

    public override string? Format
    {
        get { return Reflector.FormatString(this.Type); }
    }

    public override string? Unit
    {
        get { return null; }
    }

    public override string? IsAllowed()
    {
        return this.parent.IsAllowed();
    }

    public override PropertyRoute? GetPropertyRoute()
    {
        return PropertyRoute;
    }

    public override string NiceName()
    {
        return QueryTokenMessage._0Of1.NiceToString(this.nicePropertyName(), Parent!.Parent!.ToString());
    }

    public override QueryToken Clone()
    {
        return new MListElementPropertyToken(parent.Clone(), PropertyInfo, PropertyRoute, this.Key, this.nicePropertyName);
    }

    internal static Func<PropertyRoute, Type, bool> HasAttribute = null!;


    internal static MethodInfo miMListElementsLite = null!;


    public static Expression BuildMListElements(EntityPropertyToken ept, BuildExpressionContext ctx)
    {
        //user.Friends
        //user.MListElement<UserEntity, UserEntity>(u => u.Friends)

        var parentExpr = ept.Parent!.BuildExpression(ctx);

        var param = Expression.Parameter(ept.Parent.Type.CleanType(), ept.Parent!.Type.Name.Substring(0, 1).ToLower());

        var ctxTemp = new BuildExpressionContext(ctx.TupleType, ctx.Parameter, new Dictionary<QueryToken, ExpressionBox>
        {
            {  ept.Parent, new ExpressionBox(param , null)}
        });

        var lambda = Expression.Lambda(ept.BuildExpression(ctxTemp), param);

        var mi = miMListElementsLite.MakeGenericMethod(ept.Parent.Type.CleanType()!, ept.Type.ElementType()!);

        return Expression.Call(mi, parentExpr, lambda);
    }

    public static EntityPropertyToken? AsMListEntityProperty(QueryToken token)
    {
        return token is EntityPropertyToken ept && ept.Type.IsMList() && !HasAttribute(ept.PropertyRoute, typeof(IgnoreAttribute)) /*VirtualMList*/ ? ept : null;
    }

    public static Type MListEelementType(EntityPropertyToken eptML)
    {
        return typeof(MListElement<,>).MakeGenericType(eptML.Parent!.Type.CleanType(), eptML.Type.ElementType()!);
    }
}
