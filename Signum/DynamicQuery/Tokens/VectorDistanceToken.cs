using Pgvector;
using Signum.Engine;
using Signum.Engine.Maps;
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
        var vectorColumnToken = (VectorColumnToken)this.Parent!;
        var vectorIndex = vectorColumnToken.GetVectorIndex();
        
        // Extract the vector value from filters on the parent VectorColumnToken
        // First try SmartSearch, then fall back to direct Vector filter
        var vectorExpression = GetVectorFromFilters(context.Filters ?? new List<Filter>(), vectorColumnToken);

        if (vectorExpression == null)
            return Expression.Constant(null, typeof(float?));

        var vectorColumn = this.Parent!.BuildExpression(context);

        // Determine database type and create appropriate distance call
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

    private Expression? GetVectorFromFilters(List<Filter> filters, VectorColumnToken vectorToken)
    {
        foreach (var filter in filters)
        {
            if (filter is FilterCondition fc)
            {
                if (fc.Token != null && fc.Token.Equals(vectorToken))
                {
                    // Check for SmartSearch operation (string to embeddings)
                    if (fc.Operation == FilterOperation.SmartSearch && fc.Value is string searchString && searchString.Length > 0)
                    {
                        return GetQueryVectorExpression(searchString);
                    }
                    // Direct vector filter
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

    private Expression GetQueryVectorExpression(string searchString)
    {
        // This method will be called at query build time, not execution time
        // The embedding conversion needs to happen before the query is sent to the database
        // So we need to do the embedding lookup here and create a constant expression
        
        // Try to load embedding functionality via reflection to avoid hard dependency
        var chatbotLogicType = Type.GetType("Signum.Agent.ChatbotLogic, Signum.Agent");
        if (chatbotLogicType == null)
            throw new InvalidOperationException("Signum.Agent extension is not loaded. SmartSearch requires the Agent extension with embedding support.");

        // Get DefaultEmbeddingsModel property
        var defaultModelProperty = chatbotLogicType.GetProperty("DefaultEmbeddingsModel", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        if (defaultModelProperty == null)
            throw new InvalidOperationException("ChatbotLogic.DefaultEmbeddingsModel property not found.");

        // Get the ResetLazy value
        var defaultModelLazy = defaultModelProperty.GetValue(null);
        if (defaultModelLazy == null)
            throw new InvalidOperationException("DefaultEmbeddingsModel is null.");

        // Get Value property from ResetLazy
        var valueProperty = defaultModelLazy.GetType().GetProperty("Value");
        var defaultModelLite = valueProperty?.GetValue(defaultModelLazy);
        
        if (defaultModelLite == null)
            throw new InvalidOperationException("No default embedding model configured. Please configure a default EmbeddingsLanguageModelEntity.");

        // Call RetrieveFromCache
        var retrieveMethod = chatbotLogicType.GetMethod("RetrieveFromCache", 
            new[] { defaultModelLite.GetType() });
        if (retrieveMethod == null)
            throw new InvalidOperationException("ChatbotLogic.RetrieveFromCache method not found.");

        var model = retrieveMethod.Invoke(null, new[] { defaultModelLite });
        if (model == null)
            throw new InvalidOperationException("Failed to retrieve embedding model from cache.");
        
        // Call GetEmbeddingsAsync extension method
        var getEmbeddingsMethod = chatbotLogicType.GetMethod("GetEmbeddingsAsync",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        
        if (getEmbeddingsMethod == null)
            throw new InvalidOperationException("GetEmbeddingsAsync method not found.");

        // Call the async method and wait for result
        var task = (Task<List<float[]>>)getEmbeddingsMethod.Invoke(model, 
            new object[] { model, new[] { searchString }, CancellationToken.None })!;
        
        var embeddings = task.Result;
        var embedding = embeddings[0];
        
        var vector = new Vector(embedding);

        return Expression.Constant(vector, typeof(Vector));
    }

    protected override List<QueryToken> SubTokensOverride(SubTokensOptions options) => new List<QueryToken>();
}
