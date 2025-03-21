using NpgsqlTypes;
using Signum.Entities.TsVector;
using System.Data;
using System.Diagnostics;
using System.Threading;
using Xunit.Sdk;

namespace Signum.Test.LinqProvider;


public class FullTextSearchTest_Postgres
{
    public FullTextSearchTest_Postgres()
    {
        MusicStarter.StartAndLoad();

        WaitForFullTextIndex();

        Connector.CurrentLogger = new DebugTextWriter();

        if(!(Connector.Current is PostgreSqlConnector con))
            Assert.Skip("Skipping tests because not PostgreSqlConnector.");   
    }

    void WaitForFullTextIndex()
    {
//        if (Connector.Current is SqlServerConnector s && s.SupportsFullTextSearch)
//        {
//            for (int i = 0; i < 10; i++)
//            {
//                var t = Executor.ExecuteDataTable(@"
//SELECT
//    FULLTEXTCATALOGPROPERTY(cat.name,'MergeStatus') AS [MergeStatus],
//    FULLTEXTCATALOGPROPERTY(cat.name,'PopulateStatus') AS [PopulateStatus],
//    FULLTEXTCATALOGPROPERTY(cat.name,'ImportStatus') AS [ImportStatus]
//FROM sys.fulltext_catalogs AS cat
//");
//                var pending = t.Rows.Cast<DataRow>().Any(r => t.Columns.Cast<DataColumn>().Any(c => ((int)r[c]) != 0));

//                if (!pending)
//                    return;

//                Debug.WriteLine("Waiting for FullText Catallog...");

//                Thread.Sleep(500);
//            }
//        }
    }

    [Fact]
    public void ToTsQuery()
    {
        var res = (from note1 in Database.Query<NoteWithDateEntity>()
                   where note1.GetTsVectorColumn().Matches("american & band".ToTsQuery())
                   select note1.Id).ToList();

        Assert.True(res.Count == 1);
    }

    [Fact]
    public void ToTsQuery_Plain()
    {
        var res = (from note1 in Database.Query<NoteWithDateEntity>()
                   where note1.GetTsVectorColumn().Matches("american band".ToTsQuery_Plain())
                   select note1.Id).ToList();

        Assert.True(res.Count == 1);
    }

    [Fact]
    public void ToTsQuery_Phrase()
    {
        var res = (from note1 in Database.Query<NoteWithDateEntity>()
                   where note1.GetTsVectorColumn().Matches("american alternative".ToTsQuery_Phrase())
                   select note1.Id).ToList();

        Assert.True(res.Count == 1);

        res = (from note1 in Database.Query<NoteWithDateEntity>()
                   where note1.GetTsVectorColumn().Matches("alternative american".ToTsQuery_Phrase()) //wrong order
                   select note1.Id).ToList();

        Assert.True(res.Count == 0);

        res = (from note1 in Database.Query<NoteWithDateEntity>()
               where note1.GetTsVectorColumn().Matches("american band".ToTsQuery_Phrase()) //far away
               select note1.Id).ToList();

        Assert.True(res.Count == 0);
    }

    [Fact]
    public void ToTsQuery_WebSearch()
    {
        var res = (from note1 in Database.Query<NoteWithDateEntity>()
                   where note1.GetTsVectorColumn().Matches("\"american alternative\"".ToTsQuery_WebSearch())
                   select note1.Id).ToList();
        Assert.True(res.Count == 1);

        res = (from note1 in Database.Query<NoteWithDateEntity>()
                   where note1.GetTsVectorColumn().Matches("\"american alternative\" - band".ToTsQuery_WebSearch())
                   select note1.Id).ToList();
        Assert.True(res.Count == 0);

  
    }

    [Fact]
    public void ToTsQuery_Rank()
    {
        var query = "american & alternative";

        var res = (from note1 in Database.Query<NoteWithDateEntity>()
                   let tsquery = query.ToTsQuery()
                   where note1.GetTsVectorColumn().Matches(tsquery)
                   let rank = note1.GetTsVectorColumn().Rank(tsquery)
                   orderby rank descending
                   select new { note1.Id, rank }).ToList();

    }


    [Fact]
    public void ToTsQuery_RankCoverDensity()
    {
        var query = "american & alternative";

        var res = (from note1 in Database.Query<NoteWithDateEntity>()
                   let tsquery = query.ToTsQuery()
                   where note1.GetTsVectorColumn().Matches(tsquery)
                   let rank = note1.GetTsVectorColumn().RankCoverDensity(tsquery, TsRankingNormalization.DivideBy1PlusLogLength, new float[] { 0.1f, 0.2f, 0.5f, 0.8f})
                   orderby rank descending
                   select new { note1.Id, rank }).ToList();
    }


    [Fact]
    public void ToTsQuery_RankCoverDensity()
    {
        var query = "american & alternative";

        var res = (from note1 in Database.Query<NoteWithDateEntity>()
                   let tsquery = query.ToTsQuery()
                   where note1.GetTsVectorColumn().Matches(tsquery)
                   let rank = note1.GetTsVectorColumn().RankCoverDensity(tsquery, TsRankingNormalization.DivideBy1PlusLogLength, new float[] { 0.1f, 0.2f, 0.5f, 0.8f })
                   orderby rank descending
                   select new { note1.Id, rank }).ToList();
    }


}
