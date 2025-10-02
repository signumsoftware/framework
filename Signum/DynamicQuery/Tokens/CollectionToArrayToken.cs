using Signum.Engine.Maps;
using Signum.Utilities.Reflection;
using System.ComponentModel;

namespace Signum.DynamicQuery.Tokens;

public class CollectionToArrayToken : QueryToken
{
    public CollectionToArrayType ToArrayType { get; private set; }

    QueryToken parent;
    public override QueryToken? Parent => parent;

    readonly Type elementType;
    internal CollectionToArrayToken(QueryToken parent, CollectionToArrayType toArrayType)
    {
        elementType = parent.Type.ElementType()!;
        if (elementType == null)
            throw new InvalidOperationException("not a collection");

        this.ToArrayType = toArrayType;

        this.parent = parent ?? throw new ArgumentNullException(nameof(parent));
    }

    protected override bool AutoExpandInternal => false;
    public override bool HideInAutoExpand => true;

    public override Type Type
    {
        get { return elementType.BuildLiteNullifyUnwrapPrimaryKey(new[] { this.GetPropertyRoute()! }); }
    }

    public override string ToString()
    {
        return ToArrayType.NiceToString();
    }

    public override string Key
    {
        get { return ToArrayType.ToString(); }
    }


    public override CollectionToArrayToken? HasCollectionToArray() => this;

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


    public override CollectionToArrayToken? HasToArray() => this;

    public override PropertyRoute? GetPropertyRoute()
    {
        if (parent is ExtensionToken et && et.IsProjection)
            return et.GetElementPropertyRoute();

        PropertyRoute? pr = this.parent!.GetPropertyRoute();
        if (pr != null && pr.Type.ElementType() != null)
            return pr.Add("Item");

        return pr;
    }

    public override string NiceName()
    {
        return this.parent.NiceName();
    }

    public override QueryToken Clone()
    {
        return new CollectionToArrayToken(parent.Clone(), ToArrayType);
    }

    protected override Expression BuildExpressionInternal(BuildExpressionContext context)
    {
        throw new InvalidOperationException("ToArrayToken should have a replacement at this stage");
    }


    public static int MaxToArrayValues = 100;

    static MethodInfo miToArray = ReflectionTools.GetMethodInfo(() => Enumerable.ToArray<int>(null!)).GetGenericMethodDefinition();
    static MethodInfo miToDistict = ReflectionTools.GetMethodInfo(() => Enumerable.Distinct<int>(null!)).GetGenericMethodDefinition();
    static MethodInfo miSelect = ReflectionTools.GetMethodInfo(() => Enumerable.Select<int, long>(null!, a => a)).GetGenericMethodDefinition();
    static MethodInfo miTake = ReflectionTools.GetMethodInfo(() => Enumerable.Take<int>(null!, 0)).GetGenericMethodDefinition();
    static MethodInfo miSelectMany = ReflectionTools.GetMethodInfo(() => Enumerable.SelectMany<string, char>(null!, a => a)).GetGenericMethodDefinition();

    internal static Expression BuildToArrayExpression(QueryToken token, CollectionToArrayToken cta, BuildExpressionContext context)
    {
        var ept = MListElementPropertyToken.AsMListEntityProperty(cta.Parent!);

        BuildExpressionContext subCtx;
        Expression query;
        if (ept != null)
        {
            var collection = MListElementPropertyToken.BuildMListElements(ept, context);
            query = collection;
            Type mleType = collection.Type.ElementType()!;
            var param = Expression.Parameter(mleType, mleType.Name.Substring(0, 1).ToLower());
            subCtx = new BuildExpressionContext(mleType, param, new Dictionary<QueryToken, ExpressionBox>
            {
                [cta] = new ExpressionBox(param, mlistElementRoute: cta.GetPropertyRoute())
            }, 
             context.Filters,
             context.Orders, 
             context.Pagination);
        }
        else
        {
            var collection = cta.Parent!.BuildExpression(context);
            query = collection;
            Type elemeType = collection.Type.ElementType()!;
            var param = Expression.Parameter(elemeType, elemeType.Name.Substring(0, 1).ToLower());
            subCtx = new BuildExpressionContext(elemeType, param, new Dictionary<QueryToken, ExpressionBox>()
            {
                [cta] = new ExpressionBox(param.BuildLiteNullifyUnwrapPrimaryKey(new[] { cta.GetPropertyRoute()! }))
            },
            context.Filters, 
            context.Orders, 
            context.Pagination);
        }

        var cets = token.Follow(a => a.Parent).TakeWhile(a => a != cta).OfType<CollectionElementToken>().Reverse().ToList();
        foreach (var ce in cets)
        {
            var ept2 = MListElementPropertyToken.AsMListEntityProperty(ce.Parent!);
            if (ept2 != null)
            {
                var collection = MListElementPropertyToken.BuildMListElements(ept2, subCtx);
                Type mleType = collection.Type.ElementType()!;
                query = Expression.Call(miSelectMany.MakeGenericMethod(query.Type.ElementType()!, mleType), query, Expression.Lambda(collection, subCtx.Parameter));
                var param = Expression.Parameter(mleType, mleType.Name.Substring(0, 1).ToLower());
                subCtx = new BuildExpressionContext(mleType, param, new Dictionary<QueryToken, ExpressionBox>()
                {
                    [ce] = new ExpressionBox(param, mlistElementRoute: ce.GetPropertyRoute())
                }, 
                context.Filters,
                context.Orders,
                context.Pagination);
            }
            else
            {
                var collection = ce.Parent!.BuildExpression(subCtx);
                Type elementType = collection.Type.ElementType()!;
                query = Expression.Call(miSelectMany.MakeGenericMethod(query.Type.ElementType()!, elementType), query, Expression.Lambda(collection, subCtx.Parameter));

                var param = Expression.Parameter(elementType, elementType.Name.Substring(0, 1).ToLower());
                subCtx = new BuildExpressionContext(elementType, param, new Dictionary<QueryToken, ExpressionBox>()
                {
                    [ce] = new ExpressionBox(param.BuildLiteNullifyUnwrapPrimaryKey(new[] { ce.GetPropertyRoute()! }))
                }, 
                context.Filters, 
                context.Orders,
                context.Pagination);
            }
        }

        Expression body = token.BuildExpression(subCtx);

        if (body != subCtx.Parameter)
            query = Expression.Call(miSelect.MakeGenericMethod(query.Type.ElementType()!, body.Type), query, Expression.Lambda(body, subCtx.Parameter));

        if (cta.ToArrayType == CollectionToArrayType.SeparatedByCommaDistinct ||
            cta.ToArrayType == CollectionToArrayType.SeparatedByNewLineDistinct)
            query = Expression.Call(miToDistict.MakeGenericMethod(token.Type), query);

        query = Expression.Call(miTake.MakeGenericMethod(token.Type), query, Expression.Constant(MaxToArrayValues));

        return Expression.Call(miToArray.MakeGenericMethod(token.Type), query);
    }
}

[DescriptionOptions(DescriptionOptions.Members)]
public enum CollectionToArrayType
{
    [Description("Separated by Comma")]
    SeparatedByComma,
    [Description("Separated by Comma (Distinct)")]
    SeparatedByCommaDistinct,
    [Description("Separated by New Line")]
    SeparatedByNewLine,
    [Description("Separated by New Line (Distinct)")]
    SeparatedByNewLineDistinct,
}
