using NpgsqlTypes;
using Signum.Engine.Maps;
using Signum.Entities.TsVector;
using Signum.Utilities.Reflection;

namespace Signum.DynamicQuery.Tokens;

internal class PgTsVectorColumnToken : QueryToken
{
    private QueryToken parent;
    private PostgresTsVectorColumn column;

    public PgTsVectorColumnToken(QueryToken parent, PostgresTsVectorColumn column)
    {
        this.parent = parent;
        this.column = column;
    }

    public override string? Format => null;

    public override string? Unit => null;

    public override Type Type => typeof(NpgsqlTsVector);

    public override string Key => column.Name;

    public override QueryToken? Parent => parent;

    public override QueryToken Clone() => new PgTsVectorColumnToken(this.parent, this.column);

    public IReadOnlyList<PropertyRoute> GetColumnsRoutes() => column.Columns.Select(c => (c as FieldValue)?.Route).NotNull().ToList();

    public override Implementations? GetImplementations() => null;

    public override PropertyRoute? GetPropertyRoute() => null;

    public override string? IsAllowed() => this.parent.IsAllowed();

    public override string NiceName() => column.Name;

    public override string ToString() => column.Name;

    static MethodInfo miGetTsVectorColumna = ReflectionTools.GetMethodInfo(() => TsVectorExtensions.GetTsVectorColumn(null!, ""));

    public override string NiceTypeName => $"TsVector ({GetColumnsRoutes().ToString(a => a.PropertyString(), ", ")})";

    protected override Expression BuildExpressionInternal(BuildExpressionContext context)
    {
        var exp = this.parent.BuildExpression(context);

        var entity = exp.ExtractEntity(false);

        return Expression.Call(null, miGetTsVectorColumna, entity, Expression.Constant(this.column.Name));
    }

    protected override List<QueryToken> SubTokensOverride(SubTokensOptions options)
    {
        return new List<QueryToken>
        {
            new PgTsRankToken(this)
        };
    }
}
