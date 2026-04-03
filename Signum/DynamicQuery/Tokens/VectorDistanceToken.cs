using Pgvector;
using Signum.Utilities.Reflection;

namespace Signum.DynamicQuery.Tokens;

internal class VectorDistanceToken : QueryToken
{
    public VectorDistanceToken(QueryToken parent)
    {
        if (parent is not VectorColumnToken)
            throw new InvalidOperationException("invalid parent for VectorDistanceToken");

        Parent = parent;
    }

    public override string? Format => "0.####";

    public override string? Unit => null;

    public override Type Type => typeof(float?);

    public override string Key => "Distance";

    public override QueryToken? Parent { get; }

    public override QueryToken Clone() => new VectorDistanceToken(this.Parent!);

    public override Implementations? GetImplementations() => null;

    public override PropertyRoute? GetPropertyRoute() => null;

    public override string? IsAllowed() => this.Parent!.IsAllowed();

    public override string NiceName() => "Distance for " + this.Parent!.NiceName();

    public override string ToString() => "Distance";

    protected override Expression BuildExpressionInternal(BuildExpressionContext context)
    {
        // First check if we're using a table-valued function join (JoinWithVectorSearch or JoinWithFullText)
        // In that case, the distance is already in the replacements
        if (context.Replacements.TryGetValue(this, out var replacement))
            return replacement.RawExpression;

        // Otherwise, calculate distance inline using Vector_Distance function
        var vectorColumnToken = (VectorColumnToken)this.Parent!;
        var vectorIndex = vectorColumnToken.GetVectorIndex();
        
        var vectorExpression = GetVectorFromFilters(context.Filters ?? new List<Filter>(), vectorColumnToken);

        if (vectorExpression == null)
            return Expression.Constant(null, typeof(float?));

        var vectorColumn = this.Parent!.BuildExpression(context);

        if (Connector.Current is PostgreSqlConnector)
        {
            var metric = vectorIndex.Postgres.Metric;
            var miDistance = ReflectionTools.GetMethodInfo(() => PgVectorSearch.Distance(default(PGVectorDistanceMetric), null!, null!));
            return Expression.Call(miDistance, Expression.Constant(metric), vectorColumn, vectorExpression);
        }
        else if (Connector.Current is SqlServerConnector)
        {
            var metric = vectorIndex.SqlServer.Metric;
            var miDistance = ReflectionTools.GetMethodInfo(() => SqlVectorSearch.Vector_Distance(default(SqlVectorDistanceMetric), null!, null!));
            return Expression.Call(miDistance, Expression.Constant(metric), vectorColumn, vectorExpression);
        }
        else
        {
            throw new NotSupportedException("Vector distance is only supported on PostgreSQL and SQL Server");
        }
    }

    internal Expression? GetVectorFromFilters(List<Filter> filters, VectorColumnToken vectorToken)
    {
        foreach (var filter in filters)
        {
            if (filter is FilterCondition fc)
            {
                if (fc.Token != null && fc.Token.Equals(vectorToken))
                {
                    if (fc.Operation == FilterOperation.SmartSearch && fc.Value is string searchString && searchString.Length > 0)
                    {
                        if (Filter.GetEmbeddingForSmartSearch == null)
                            throw new InvalidOperationException(
                                "Filter.GetEmbeddingForSmartSearch is not configured. " +
                                "Please set this function during application startup to convert text queries to embeddings.");

                        var vector = Filter.GetEmbeddingForSmartSearch(vectorToken, searchString);
                        return Expression.Constant(vector, typeof(Vector));
                    }
                    else if (fc.Value is Vector v)
                    {
                        return Expression.Constant(v, typeof(Vector));
                    }
                }
            }
            else if (filter is FilterGroup fg)
            {
                var result = GetVectorFromFilters(fg.Filters, vectorToken);
                if (result != null)
                    return result;
            }
        }

        return null;
    }

    protected override List<QueryToken> SubTokensOverride(SubTokensOptions options) => new List<QueryToken>();
}
