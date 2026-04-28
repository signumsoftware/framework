using Pgvector;
using Signum.DynamicQuery;
using Signum.DynamicQuery.Tokens;
using Signum.Engine.Maps;
using Xunit.Sdk;

namespace Signum.Test.DynamicQueries;

public class DynamicQueryVectorTest
{
    const SubTokensOptions cto =
           SubTokensOptions.CanToArray |
           SubTokensOptions.CanOperation |
           SubTokensOptions.CanSnippet |
           SubTokensOptions.CanManual |
           SubTokensOptions.CanAggregate |
           SubTokensOptions.CanTimeSeries |
           SubTokensOptions.CanElement |
           SubTokensOptions.CanNested;


    public DynamicQueryVectorTest()
    {
        MusicStarter.StartAndLoad();
        Connector.CurrentLogger = new DebugTextWriter();

        if (!Connector.Current.SupportsVectors)
            throw SkipException.ForSkip("Skipping tests because connector does not support vectors");
    }


    [Fact]
    public void VectorTableIndexExists()
    {
        var table = Schema.Current.Tables.TryGetC(typeof(SimplePassageEntity));
        Assert.NotNull(table);

        var vectorIndexes = table.AllIndexes().OfType<VectorTableIndex>().ToList();
        Assert.True(vectorIndexes.Count > 0, "Expected at least one VectorTableIndex on SimplePassageEntity");

        var vectorIndex = vectorIndexes.First();
        Assert.Single(vectorIndex.Columns);
        
        var column = vectorIndex.Columns.Single();
        // PostgreSQL may store column names differently
        Assert.True(column.Name.ToLower() == "embedding", $"Expected 'embedding' but got '{column.Name}'");
    }

    [Fact]
    public void VectorColumnDiscovery()
    {
        var qd = QueryLogic.Queries.QueryDescription(typeof(SimplePassageEntity));

        // Vector columns should appear on the Entity level, not as regular columns
        // Parse Entity first
        var entityToken = QueryUtils.Parse("Entity", qd, cto);
        Assert.NotNull(entityToken);

        // Get sub-tokens from Entity
        var subTokens = QueryUtils.SubTokens(entityToken, qd, cto).ToList();
        
        // Look for VectorColumnToken in the Entity's sub-tokens
        var vectorToken = subTokens.FirstOrDefault(st => st is VectorColumnToken);

        Assert.NotNull(vectorToken);
        Assert.IsType<VectorColumnToken>(vectorToken);

        // Now check if VectorDistanceToken is available as a sub-token of VectorColumnToken
        var vectorSubTokens = QueryUtils.SubTokens(vectorToken, qd, cto).ToList();
        var distanceToken = vectorSubTokens.FirstOrDefault(st => st is VectorDistanceToken);

        Assert.NotNull(distanceToken);
        Assert.Equal(typeof(float?), distanceToken.Type);
    }

    [Fact]
    public void VectorDistanceTokenParsing()
    {
        var qd = QueryLogic.Queries.QueryDescription(typeof(SimplePassageEntity));

        var distanceToken = QueryUtils.Parse("Entity.Embedding.Distance", qd, cto);

        Assert.NotNull(distanceToken);
        Assert.IsType<VectorDistanceToken>(distanceToken);
        Assert.Equal(typeof(float?), distanceToken.Type);
        
        Assert.NotNull(distanceToken.Parent);
        Assert.IsType<VectorColumnToken>(distanceToken.Parent);
        
        var vectorToken = (VectorColumnToken)distanceToken.Parent;
        Assert.Equal(typeof(Vector), vectorToken.Type);

        var sampleChunk = Database.Query<SimplePassageEntity>()
            .Where(a => a.Embedding != null)
            .Select(a => a.Chunk)
            .FirstOrDefault();

        if (sampleChunk == null)
        {
            Assert.Skip("No SimplePassageEntity with embeddings found in database");
            return;
        }

        var rt = QueryLogic.Queries.ExecuteQuery(new QueryRequest
        {
            QueryName = typeof(SimplePassageEntity),
            GroupResults = false,
            Columns = new List<Column>
            {
                new Column(QueryUtils.Parse("Entity.Chunk", qd, cto), null),
                new Column(distanceToken, null),
            },
            Filters = new List<DynamicQuery.Filter>
            {
                new FilterCondition(vectorToken, FilterOperation.SmartSearch, sampleChunk),
            },
            Orders = new List<Order>
            {
                new Order(distanceToken, OrderType.Ascending),
            },
            Pagination = new Pagination.Firsts(5),
        });

        Assert.True(rt.Columns.Length == 2);
        Assert.True(rt.Rows.Length > 0);
        
        var firstRow = rt.Rows[0];
        var chunk = (string?)firstRow[0];
        var distance = (float?)firstRow[1];
        
        Assert.Equal(sampleChunk, chunk);
        Assert.NotNull(distance);
        Assert.True(distance < 0.01f, $"Expected distance < 0.01 for exact text match, got {distance}");
    }

    [Fact]
    public void VectorSearchWithDirectEmbedding()
    {
        var sampleChunk = Database.Query<SimplePassageEntity>()
            .Where(a => a.Embedding != null)
            .Select(a => a.Chunk)
            .FirstOrDefault();

        if (sampleChunk == null)
        {
            Assert.Skip("No SimplePassageEntity with embeddings found in database");
            return;
        }

        var qd = QueryLogic.Queries.QueryDescription(typeof(SimplePassageEntity));

        var rt = QueryLogic.Queries.ExecuteQuery(new QueryRequest
        {
            QueryName = typeof(SimplePassageEntity),
            GroupResults = false,
            Columns = new List<Column>
            {
                new Column(QueryUtils.Parse("Entity.Chunk", qd, cto), null),
                new Column(QueryUtils.Parse("Entity.Embedding.Distance", qd, cto), null),
            },
            Filters = new List<DynamicQuery.Filter>
            {
                new FilterCondition(QueryUtils.Parse("Entity.Embedding", qd, cto), FilterOperation.SmartSearch, sampleChunk),
            },
            Orders = new List<Order>
            {
                new Order(QueryUtils.Parse("Entity.Embedding.Distance", qd, cto), OrderType.Ascending),
            },
            Pagination = new Pagination.Firsts(5),
        });

        Assert.True(rt.Columns.Length == 2);
        Assert.True(rt.Rows.Length > 0);
        
        var firstRow = rt.Rows[0];
        var distance = (float?)firstRow[1];
        Assert.NotNull(distance);
        Assert.True(distance < 0.01f, $"Expected distance < 0.01 for self-match, got {distance}");
    }

    [Fact]
    public void VectorSmartSearchWithText()
    {
        // Get the first chunk text to use as search query
        var sampleChunk = Database.Query<SimplePassageEntity>()
            .Where(a => a.Embedding != null)
            .Select(a => a.Chunk)
            .FirstOrDefault();

        if (sampleChunk == null)
        {
            Assert.Skip("No SimplePassageEntity with embeddings found in database");
            return;
        }

        var qd = QueryLogic.Queries.QueryDescription(typeof(SimplePassageEntity));

        // Execute query with SmartSearch filter using text
        var rt = QueryLogic.Queries.ExecuteQuery(new QueryRequest
        {
            QueryName = typeof(SimplePassageEntity),
            GroupResults = false,
            Columns = new List<Column>
            {
                new Column(QueryUtils.Parse("Entity.Chunk", qd, cto), null),
                new Column(QueryUtils.Parse("Entity.Embedding.Distance", qd, cto), null),
            },
            Filters = new List<DynamicQuery.Filter>
            {
                // Use SmartSearch with text - this will use Filter.GetEmbeddingForSmartSearch
                new FilterCondition(QueryUtils.Parse("Entity.Embedding", qd, cto), FilterOperation.SmartSearch, sampleChunk),
            },
            Orders = new List<Order>
            {
                new Order(QueryUtils.Parse("Entity.Embedding.Distance", qd, cto), OrderType.Ascending),
            },
            Pagination = new Pagination.Firsts(5),
        });

        Assert.True(rt.Columns.Length == 2);
        Assert.True(rt.Rows.Length > 0);
        
        // First result should be the exact match with distance ~0
        var firstRow = rt.Rows[0];
        var chunk = (string?)firstRow[0];
        var distance = (float?)firstRow[1];
        
        Assert.Equal(sampleChunk, chunk);
        Assert.NotNull(distance);
        Assert.True(distance < 0.01f, $"Expected distance < 0.01 for exact text match, got {distance}");
    }

    [Fact]
    public void VectorOrderByDistanceWithoutFilter()
    {
        var qd = QueryLogic.Queries.QueryDescription(typeof(SimplePassageEntity));

        var vectorToken = (VectorColumnToken)QueryUtils.Parse("Entity.Embedding", qd, cto);
        var distanceToken = new VectorDistanceToken(vectorToken);
        
        // Query WITHOUT any filter - distance should be NULL since there's no vector to compare against
        var rt = QueryLogic.Queries.ExecuteQuery(new QueryRequest
        {
            QueryName = typeof(SimplePassageEntity),
            GroupResults = false,
            Columns = new List<Column>
            {
                new Column(QueryUtils.Parse("Entity.Chunk", qd, cto), null),
                new Column(distanceToken, null),
            },
            Filters = new List<DynamicQuery.Filter>(), // No filters at all!
            Orders = new List<Order>
            {
                new Order(distanceToken, OrderType.Ascending),
            },
            Pagination = new Pagination.Firsts(5),
        });

        Assert.True(rt.Columns.Length == 2);
        Assert.True(rt.Rows.Length > 0);
        
        // Since there's no filter to provide a vector, all distance values should be NULL
        foreach (var row in rt.Rows)
        {
            var distance = (float?)row[1];
            Assert.Null(distance);
        }
    }


}
