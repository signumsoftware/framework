using Signum.Engine.Maps;
using System.Data;
using System.Diagnostics;
using System.Text;
using System.Threading;
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

        if (con.Version < SqlServerVersion.SqlServer2025)
            throw SkipException.ForSkip("Skipping tests because Vector Search requires SQL Server 2025 or later.");
    }

    [Fact]
    public void Vector_Distance()
    {
        var vector1 = new float[] { 1.0f, 0.0f, 0.0f };
        var vector2 = new float[] { 0.0f, 1.0f, 0.0f };

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
        var vector = new float[] { 2.0f, 3.0f, 6.0f };

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
        var vector = new float[] { 2.0f, 3.0f, 6.0f };

        var norm1 = Database.Query<NoteWithDateEntity>().Select(n => SqlVectorSearch.Vector_Normalize(vector, SqlVectorNormType.Norm1).InSql()).First();
        var norm2 = Database.Query<NoteWithDateEntity>().Select(n => SqlVectorSearch.Vector_Normalize(vector, SqlVectorNormType.Norm2).InSql()).First();
        var norminf = Database.Query<NoteWithDateEntity>().Select(n => SqlVectorSearch.Vector_Normalize(vector, SqlVectorNormType.Norminf).InSql()).First();

        foreach (var item in new[] { norm1, norm2, norminf})
        {
            foreach (var a in item)
            {
                Assert.True(a > 0 && a <= 1.0f);
            }
        }
    }


    //[Fact]
    //public void Vector_Search_Similarity()
    //{
    //    var hasVectors = Convert.ToInt32(Executor.ExecuteScalar("SELECT COUNT(*) FROM NoteWithDateEntity WHERE Vector IS NOT NULL"));
    //    if (hasVectors == 0)
    //        throw SkipException.ForSkip("No vectors in test data.");

    //    var results = Database.Query<>()
    //        .Take(5)
    //        .ToList();

    //    Assert.True(results.Count > 0);
    //}

    //[Fact]
    //public void Vector_Multiple_Distance_Metrics()
    //{
    //    var vector1 = new float[] { 1.0f, 0.0f, 0.0f };
    //    var vector2 = new float[] { 0.5f, 0.5f, 0.0f };

    //    var results = Database.Query<NoteWithDateEntity>()
    //        .Select(n => new
    //        {
    //            Cosine = VectorSearch.Vector_Distance("cosine", vector1, vector2),
    //            Euclidean = VectorSearch.Vector_Distance("euclidean", vector1, vector2),
    //            Dot = VectorSearch.Vector_Distance("dot", vector1, vector2)
    //        })
    //        .First();

    //    Assert.True(results.Cosine >= 0);
    //    Assert.True(results.Euclidean >= 0);
    //    Assert.True(results.Dot >= 0);
    //}

    //[Fact]
    //public void Vector_Distance_With_Filter()
    //{
    //    var vector = new float[] { 1.0f, 0.0f, 0.0f };
    //    var compareVector = new float[] { 0.0f, 1.0f, 0.0f };

    //    var results = (from n in Database.Query<NoteWithDateEntity>()
    //                  where n.Title != null
    //                  select new
    //                  {
    //                      Note = n,
    //                      Distance = VectorSearch.Vector_Distance("cosine", vector, compareVector)
    //                  }).Take(5).ToList();

    //    Assert.True(results.Count > 0);
    //    Assert.True(results.All(r => r.Distance >= 0));
    //}

    //[Fact]
    //public void Vector_Operations_Chain()
    //{
    //    var vector = new float[] { 2.0f, 2.0f, 1.0f };

    //    var result = Database.Query<NoteWithDateEntity>()
    //        .Select(n => new
    //        {
    //            Norm = VectorSearch.Vector_Norm(vector),
    //            Normalized = VectorSearch.Vector_Normalize(vector),
    //            NormalizedNorm = VectorSearch.Vector_Norm(VectorSearch.Vector_Normalize(vector))
    //        })
    //        .First();

    //    Assert.True(result.Norm > 0);
    //    Assert.NotNull(result.Normalized);
    //    Assert.True(Math.Abs(result.NormalizedNorm - 1.0) < 0.001);
    //}
}
