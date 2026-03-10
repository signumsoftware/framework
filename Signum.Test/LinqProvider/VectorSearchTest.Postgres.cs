using Pgvector;
using Xunit.Sdk;

namespace Signum.Test.LinqProvider;

public class VectorSearchTest_Postgres
{
    public VectorSearchTest_Postgres()
    {
        MusicStarter.StartAndLoad();

        Connector.CurrentLogger = new DebugTextWriter();

        if (!(Connector.Current is PostgreSqlConnector con))
            throw SkipException.ForSkip("Skipping tests because not Postgres.");
    }

    [Fact]
    public void Distance()
    {
        var vector1 = new Vector(new float[] { 1.0f, 0.0f, 0.0f });
        var vector2 = new Vector(new float[] { 0.0f, 1.0f, 0.0f });

        var distance1 = Database.Query<NoteWithDateEntity>().Select(n => PgVectorSearch.Distance(PGVectorDistanceMetric.Cosine, vector1, vector2).InSql()).First();
        var distance2 = Database.Query<NoteWithDateEntity>().Select(n => PgVectorSearch.Distance(PGVectorDistanceMetric.L2, vector1, vector2).InSql()).First();
        var distance3 = Database.Query<NoteWithDateEntity>().Select(n => PgVectorSearch.Distance(PGVectorDistanceMetric.InnerProduct, vector1, vector2).InSql()).First();

        Assert.True(distance1 > 0);
        Assert.True(distance2 > 0);
        Assert.True(distance3 == 0);
    }

    [Fact]
    public void Normalize()
    {
        var vector = new Pgvector.Vector(new float[] { 2.0f, 3.0f, 6.0f });

        var norm2 = Database.Query<NoteWithDateEntity>().Select(n => PgVectorSearch.Normalize(vector).InSql()).First();

        foreach (var item in new[] { norm2 })
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

        var cosine = Database.Query<SimplePassageEntity>().Select(a => new
        {
            a.Chunk,
            Distance = PgVectorSearch.Distance(PGVectorDistanceMetric.Cosine, a.Embedding!, michael.Embedding!)
        }).OrderBy(a => a.Distance).Take(5)
        .ToList();
    }
}
