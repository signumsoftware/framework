using Signum.Engine.Maps;

namespace Signum.DynamicQuery.Tokens;

public class CollectionNestedToken : QueryToken
{
    QueryToken parent;
    public override QueryToken? Parent => parent;

    readonly Type elementType;
    internal CollectionNestedToken(QueryToken parent)
    {
        elementType = parent.Type.ElementType()!;
        if (elementType == null)
            throw new InvalidOperationException("not a collection");

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
        return QueryTokenMessage.Nested.NiceToString();
    }

    public override string Key
    {
        get { return "Nested"; }
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


    public override bool HasElement() => true;

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
        Type parentElement = elementType.CleanType();

        if (parentElement.IsModifiableEntity())
            return parentElement.NiceName();

        return QueryTokenMessage._0Of1.NiceToString(this.ToString(), Parent?.NiceName());
    }

    public override QueryToken Clone()
    {
        return new CollectionNestedToken(parent.Clone());
    }


    public override CollectionNestedToken? HasNested() => this;

    protected override Expression BuildExpressionInternal(BuildExpressionContext context)
    {
        throw new InvalidOperationException("CollectionNestedToken should have a replacement at this stage");
    }
}

