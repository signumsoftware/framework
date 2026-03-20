using Signum.Engine.Maps;

namespace Signum.DynamicQuery.Tokens;

public class VectorColumnToken : QueryToken
{
    private QueryToken parent;
    private FieldValue column;
    private VectorTableIndex index;

    public VectorColumnToken(QueryToken parent, IColumn column, VectorTableIndex index)
    {
        this.parent = parent;
        this.column = (FieldValue)column;
        this.index = index;
    }

    public override string? Format => null;

    public override string? Unit => null;

    public override Type Type => typeof(Pgvector.Vector);

    public override string Key => column.Route.PropertyInfo!.Name;

    public override QueryToken? Parent => parent;

    public override QueryToken Clone() => new VectorColumnToken(this.parent, this.column, this.index);

    public VectorTableIndex GetVectorIndex() => index;

    public override Implementations? GetImplementations() => null;

    public override PropertyRoute? GetPropertyRoute() => column.Route;

    public override string? IsAllowed() => this.parent.IsAllowed();

    public override string NiceName() => column.Route.PropertyInfo!.NiceName();

    public override string ToString() => column.Route.PropertyInfo!.Name;

    public override string NiceTypeName => $"Vector ({column.Route.PropertyInfo!.Name})";

    public override bool IsGroupable => false;

    protected override Expression BuildExpressionInternal(BuildExpressionContext context)
    {
        if (this.parent is CollectionElementToken cet)
        {
            var ept = MListElementPropertyToken.AsMListEntityProperty(cet.Parent!);
            var baseExpression = context.Replacements.GetOrThrow(cet).RawExpression;
            
            // Get the property from the MListElement
            return Expression.Property(baseExpression, column.Route.PropertyInfo!);
        }
        else
        {
            var exp = this.parent.BuildExpression(context);
            var entity = exp.ExtractEntity(false);
            
            // Get the property from the entity
            return Expression.Property(entity, column.Route.PropertyInfo!);
        }
    }

    protected override List<QueryToken> SubTokensOverride(SubTokensOptions options)
    {
        return new List<QueryToken>
        {
            new VectorDistanceToken(this)
        };
    }
}
