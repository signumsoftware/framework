using Pgvector;
using System.Data;
using Xunit.Sdk;

namespace Signum.Test.LinqProvider;

public class VectorSearchTest_SqlServer
{
    public VectorSearchTest_SqlServer()
    {
        MusicStarter.StartAndLoad();

        Connector.CurrentLogger = new DebugTextWriter();

        if (!(Connector.Current is SqlServerConnector con))
            throw SkipException.ForSkip("Skipping tests because not SQL Server.");

        if (!con.SupportsVectors)
            throw SkipException.ForSkip("Skipping tests because Vector Search requires SQL Server 2025 or later.");
    }

    [Fact]
    public void Vector_Distance()
    {
        var vector1 = new Vector(new float[] { 1.0f, 0.0f, 0.0f });
        var vector2 = new Vector(new float[] { 0.0f, 1.0f, 0.0f });

        var distance1 = Database.Query<NoteWithDateEntity>().Select(n => SqlVectorSearch.Vector_Distance(SqlVectorDistanceMetric.Cosine, vector1, vector2).InSql()).First();
        var distance2 = Database.Query<NoteWithDateEntity>().Select(n => SqlVectorSearch.Vector_Distance(SqlVectorDistanceMetric.Euclidean, vector1, vector2).InSql()).First();
        var distance3 = Database.Query<NoteWithDateEntity>().Select(n => SqlVectorSearch.Vector_Distance(SqlVectorDistanceMetric.DotProduct, vector1, vector2).InSql()).First();
        
        Assert.True(distance1 > 0);
        Assert.True(distance2 > 0);
        Assert.True(distance3 == 0);
    }

    [Fact]
    public void Vector_Norm()
    {
        var vector = new Vector(new float[] { 2.0f, 3.0f, 6.0f });

        var norm1 = Database.Query<NoteWithDateEntity>().Select(n => SqlVectorSearch.Vector_Norm(vector, SqlVectorNormType.Norm1).InSql()).First();
        var norm2 = Database.Query<NoteWithDateEntity>().Select(n => SqlVectorSearch.Vector_Norm(vector, SqlVectorNormType.Norm2).InSql()).First();
        var norminf = Database.Query<NoteWithDateEntity>().Select(n => SqlVectorSearch.Vector_Norm(vector, SqlVectorNormType.Norminf).InSql()).First();

        Assert.Equal(11, norm1);
        Assert.Equal(7, norm2);
        Assert.Equal(6, norminf);
    }

    [Fact]
    public void Vector_Normalize()
    {
        var vector = new Pgvector.Vector(new float[] { 2.0f, 3.0f, 6.0f });

        var norm1 = Database.Query<NoteWithDateEntity>().Select(n => SqlVectorSearch.Vector_Normalize(vector, SqlVectorNormType.Norm1).InSql()).First();
        var norm2 = Database.Query<NoteWithDateEntity>().Select(n => SqlVectorSearch.Vector_Normalize(vector, SqlVectorNormType.Norm2).InSql()).First();
        var norminf = Database.Query<NoteWithDateEntity>().Select(n => SqlVectorSearch.Vector_Normalize(vector, SqlVectorNormType.Norminf).InSql()).First();

        foreach (var item in new[] { norm1, norm2, norminf})
        {
            foreach (var a in item.Memory.ToArray())
            {
                Assert.True(a > 0 && a <= 1.0f);
            }
        }
    }

    [Fact]
    public void Vector_Search()
    {
        var michael = Database.Query<SimplePassageEntity>().FirstEx(a => a.Chunk.Contains("Michael"));

        var cosine = SqlVectorSearch.Vector_Search<SimplePassageEntity>(a => a.Embedding!, michael.Embedding!, SqlVectorDistanceMetric.Cosine, 5)
            .Select(a => new { a.Distance, a.Original.Chunk }).ToList();

    }

}
