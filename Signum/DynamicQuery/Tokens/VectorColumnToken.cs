using Pgvector;
using Signum.Engine.Maps;
using Signum.Entities.VectorSearch;
using Signum.Utilities.Reflection;

namespace Signum.DynamicQuery.Tokens;

public class VectorColumnToken : QueryToken
{
    private QueryToken parent;
    private IColumn column;
    private VectorTableIndex index;

    public VectorColumnToken(QueryToken parent, IColumn column, VectorTableIndex index)
    {
        this.parent = parent;
        this.column = column;
        this.index = index;
    }

    public override string? Format => null;

    public override string? Unit => null;

    public override Type Type => typeof(Pgvector.Vector);

    public override string Key => column.Name;

    public override QueryToken? Parent => parent;

    public override QueryToken Clone() => new VectorColumnToken(this.parent, this.column, this.index);

    public VectorTableIndex GetVectorIndex() => index;

    public override Implementations? GetImplementations() => null;

    public override PropertyRoute? GetPropertyRoute()
    {
        // Try to get property route from the field
        if (column is FieldValue fv)
            return fv.Route;
        return null;
    }

    public override string? IsAllowed() => this.parent.IsAllowed();

    public override string NiceName() => column.Name;

    public override string ToString() => column.Name;

    static MethodInfo miGetVectorColumn = ReflectionTools.GetMethodInfo(() => VectorExtensions.GetVectorColumn(null!, ""));
    static MethodInfo miGetVectorColumnMList = ReflectionTools.GetMethodInfo(() => VectorExtensions.GetVectorColumn<Entity, string>(null!, "")).GetGenericMethodDefinition()!;

    public override string NiceTypeName => $"Vector ({column.Name})";

    // Vector columns can only be used in filters (for SmartSearch), not in columns or orders
    public override bool IsGroupable => false;

    protected override Expression BuildExpressionInternal(BuildExpressionContext context)
    {
        if (this.parent is CollectionElementToken cet)
        {
            var ept = MListElementPropertyToken.AsMListEntityProperty(cet.Parent!);
            var baseExpression = context.Replacements.GetOrThrow(parent).RawExpression;
            var mi = miGetVectorColumnMList.MakeGenericMethod(baseExpression.Type.GetGenericArguments());

            return Expression.Call(null, mi, baseExpression, Expression.Constant(this.column.Name));
        }
        else
        {
            var exp = this.parent.BuildExpression(context);

            var entity = exp.ExtractEntity(false);

            return Expression.Call(null, miGetVectorColumn, entity, Expression.Constant(this.column.Name));
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
