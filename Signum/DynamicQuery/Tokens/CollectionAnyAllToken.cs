using Signum.Engine.Maps;
using Signum.Utilities.Reflection;

namespace Signum.DynamicQuery.Tokens;

public class CollectionAnyAllToken : QueryToken
{
    public CollectionAnyAllType CollectionAnyAllType { get; private set; }

    readonly QueryToken parent;
    public override QueryToken? Parent => parent;

    readonly Type elementType;
    internal CollectionAnyAllToken(QueryToken parent, CollectionAnyAllType type)
    {
        elementType = parent.Type.ElementType()!;
        if (elementType == null)
            throw new InvalidOperationException("not a collection");

        this.parent = parent ?? throw new ArgumentNullException(nameof(parent));
        this.CollectionAnyAllType = type;
    }

    protected override bool AutoExpandInternal => false;

    public override bool HideInAutoExpand => true;


    public override Type Type
    {
        get { return elementType.BuildLiteNullifyUnwrapPrimaryKey(new[] { this.GetPropertyRoute()! }); }
    }

    public override string ToString()
    {
        return CollectionAnyAllType.NiceToString();
    }

    public override string Key
    {
        get { return CollectionAnyAllType.ToString(); }
    }

    protected override List<QueryToken> SubTokensOverride(SubTokensOptions options)
    {
        var st = SubTokensBase(Type, options, GetImplementations());

        var ept = MListElementPropertyToken.AsMListEntityProperty(this.parent);
        if (ept != null)
        {
            st.Add(MListElementPropertyToken.RowId(this, ept));

            var fm = (FieldMList)Schema.Current.Field(ept.PropertyRoute);
            if (fm.TableMList.Order != null)
                st.Add(MListElementPropertyToken.RowOrder(this, ept));

            if (fm.TableMList.PartitionId != null)
                st.Add(MListElementPropertyToken.PartitionId(this, ept));
        }

        return st;
    }

    public override Implementations? GetImplementations()
    {
        return parent.GetElementImplementations();
    }

    public override string? Format
    {
        get
        {
            if (Parent is ExtensionToken et && et.IsProjection)
                return et.ElementFormat;

            return parent.Format;
        }
    }

    public override string? Unit
    {
        get
        {
            if (Parent is ExtensionToken et && et.IsProjection)
                return et.ElementUnit;

            return parent.Unit;
        }
    }

    public override string? IsAllowed()
    {
        return parent.IsAllowed();
    }

    public override bool HasAllOrAny() => true;

    public override PropertyRoute? GetPropertyRoute()
    {
        if (Parent is ExtensionToken et && et.IsProjection)
            return et.GetElementPropertyRoute();

        PropertyRoute parent = Parent!.GetPropertyRoute()!;
        if (parent != null && parent.Type.ElementType() != null)
            return parent.Add("Item");

        return parent;
    }

    public override string NiceName()
    {
        return null!;
    }

    public override QueryToken Clone()
    {
        return new CollectionAnyAllToken(parent.Clone(), this.CollectionAnyAllType);
    }

    protected override Expression BuildExpressionInternal(BuildExpressionContext context)
    {
        throw new InvalidOperationException("CollectionAnyAllToken should have a replacement at this stage");
    }


    internal ParameterExpression CreateParameter()
    {
        return Expression.Parameter(elementType);
    }

    internal Expression CreateExpression(ParameterExpression parameter)
    {
        return parameter.BuildLite().Nullify();
    }

    static readonly MethodInfo miAnyE = ReflectionTools.GetMethodInfo((IEnumerable<string> col) => col.Any(null!)).GetGenericMethodDefinition();
    static readonly MethodInfo miAllE = ReflectionTools.GetMethodInfo((IEnumerable<string> col) => col.All(null!)).GetGenericMethodDefinition();
    static readonly MethodInfo miAnyQ = ReflectionTools.GetMethodInfo((IQueryable<string> col) => col.Any(null!)).GetGenericMethodDefinition();
    static readonly MethodInfo miAllQ = ReflectionTools.GetMethodInfo((IQueryable<string> col) => col.All(null!)).GetGenericMethodDefinition();

    public Expression BuildAnyAll(Expression collection, ParameterExpression param, Expression body)
    {
        if (this.CollectionAnyAllType == CollectionAnyAllType.NotAll)
            body = Expression.Not(body);

        var lambda = Expression.Lambda(body, param);

        MethodInfo mi = typeof(IQueryable).IsAssignableFrom(collection.Type) ?
             (this.CollectionAnyAllType == CollectionAnyAllType.All ? miAllQ : miAnyQ) :
             (this.CollectionAnyAllType == CollectionAnyAllType.All ? miAllE : miAnyE);

        var result = Expression.Call(mi.MakeGenericMethod(param.Type), collection, lambda);

        if (this.CollectionAnyAllType == CollectionAnyAllType.NotAny)
            return Expression.Not(result);

        return result;
    }
}


[DescriptionOptions(DescriptionOptions.Members), InTypeScript(true)]
public enum CollectionAnyAllType
{
    Any,
    All,
    NotAny,
    NotAll,
}
