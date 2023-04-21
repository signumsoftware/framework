
using Signum.Engine.SchemaInfoTables;
using Signum.Entities.DynamicQuery;
using System.Data;
using System.Diagnostics;
using System.Threading;

namespace Signum.Test.LinqProvider;

public class FullTextSearchTest
{
    public FullTextSearchTest()
    {
        MusicStarter.StartAndLoad();

        WaitForFullTextIndex();

        Connector.CurrentLogger = new DebugTextWriter();
    }

    void WaitForFullTextIndex()
    {
        for (int i = 0; i < 10; i++)
        {
            var t = Executor.ExecuteDataTable(@"
SELECT
    FULLTEXTCATALOGPROPERTY(cat.name,'MergeStatus') AS [MergeStatus],
    FULLTEXTCATALOGPROPERTY(cat.name,'PopulateStatus') AS [PopulateStatus],
    FULLTEXTCATALOGPROPERTY(cat.name,'ImportStatus') AS [ImportStatus]
FROM sys.fulltext_catalogs AS cat
");
            var pending = t.Rows.Cast<DataRow>().Any(r => t.Columns.Cast<DataColumn>().Any(c => ((int)r[c]) != 0));

            if (!pending)
                return;

            Debug.WriteLine("Waiting for FullText Catallog...");

            Thread.Sleep(500);
        }
    }

    [Fact]
    public void Contains()
    {
        if (!Connector.Current.SupportsFullTextSearch)
            return;

        var res = (from note1 in Database.Query<NoteWithDateEntity>()
                   where FullTextSearch.Contains(new[] { note1.Text }, "american AND band")
                   select note1.Id).ToList();

        Assert.True(res.Count == 1);
    }

    [Fact]
    public void Contains_AllColumns()
    {
        if (!Connector.Current.SupportsFullTextSearch)
            return;

        var res = (from note1 in Database.Query<NoteWithDateEntity>()
                   where FullTextSearch.Contains(note1, "american AND band")
                   select note1.Id).ToList();

        Assert.True(res.Count == 1);
    }


    [Fact]
    public void Contains_TwoTables()
    {
        if (!Connector.Current.SupportsFullTextSearch)
            return;

        var res = (from note1 in Database.Query<NoteWithDateEntity>()
                   from note2 in Database.Query<NoteWithDateEntity>()
                   where FullTextSearch.Contains(new[] { note1.Text }, "american AND band")
                   select note1.Id).ToList();

        Assert.True(res.Count > 1);
    }

    [Fact]
    public void Contains_TwoTables_Wrong()
    {
        if (!Connector.Current.SupportsFullTextSearch)
            return;

        Assert.Throws<InvalidOperationException>(() =>
        {
            var res = (from note1 in Database.Query<NoteWithDateEntity>()
                       from note2 in Database.Query<NoteWithDateEntity>()
                       where FullTextSearch.Contains(new[] { note1.Text, note2.Text }, "american AND band")
                       select note1.Id).ToList();
        });
    }

    [Fact]
    public void Contains_TwoTables_Right()
    {
        if (!Connector.Current.SupportsFullTextSearch)
            return;

        var res = (from note1 in Database.Query<NoteWithDateEntity>()
                   from note2 in Database.Query<NoteWithDateEntity>()
                   where FullTextSearch.Contains(new[] { note1.Text }, "american AND band") &&
                   FullTextSearch.Contains(new[] { note2.Text }, "blue AND angel")
                   select new { Id1 = note1.Id, Id2 = note2.Id }).ToList();

        Assert.True(res.Count == 1);
    }

    [Fact]
    public void FreeText()
    {
        if (!Connector.Current.SupportsFullTextSearch)
            return;

        var res = (from note1 in Database.Query<NoteWithDateEntity>()
                   where FullTextSearch.FreeText(new[] { note1.Text }, "American band")
                   select note1.Id).ToList();

        Assert.True(res.Count == 2);
    }



    [Fact]
    public void ContainsTable()
    {
        if (!Connector.Current.SupportsFullTextSearch)
            return;

        var res = (from r in FullTextSearch.ContainsTable<NoteWithDateEntity>(null, "american AND band", 5)
                   select new { r.Key, r.Rank }).ToList();

        Assert.True(res.Count == 1);
    }

    [Fact]
    public void ContainsTable_Join()
    {
        if (!Connector.Current.SupportsFullTextSearch)
            return;

        var res = (from r in Database.Query<NoteWithDateEntity>()
                   join ft in FullTextSearch.ContainsTable<NoteWithDateEntity>(null, "american AND band", 5)
                   on r.Id equals ft.Key
                   orderby ft.Rank descending
                   select new { Lite = r.ToLite(), ft.Rank }
                   ).ToList();

        Assert.True(res.Count == 1);
    }

    [Fact]
    public void FreeTextTable()
    {
        if (!Connector.Current.SupportsFullTextSearch)
            return;

        var res = (from r in FullTextSearch.FreeTextTable<NoteWithDateEntity>(null, "american band", 5)
                   select new { r.Key, r.Rank }).ToList();

        Assert.True(res.Count > 1);
    }


    [Fact]
    public void FreeTextTable_Join()
    {
        if (!Connector.Current.SupportsFullTextSearch)
            return;

        var res = (from r in Database.Query<NoteWithDateEntity>()
                   join ft in FullTextSearch.FreeTextTable<NoteWithDateEntity>(null, "american band", 5)
                   on r.Id equals ft.Key
                   orderby ft.Rank descending
                   select new { Lite = r.ToLite(), ft.Rank }
                   ).ToList();

        Assert.True(res.Count > 1);
    }

}
