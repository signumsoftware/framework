
namespace Signum.Test.LinqProvider;

/// <summary>
/// Summary description for LinqProvider
/// </summary>
public class DistinctTest
{
    public DistinctTest()
    {
        MusicStarter.StartAndLoad();
        Connector.CurrentLogger = new DebugTextWriter();
    }

    [Fact]
    public void DistinctString()
    {
        var authors = Database.Query<AlbumEntity>().Select(a => a.Label.Name).Distinct().ToList();
    }

    [Fact]
    public void DistinctPair()
    {
        var authors = Database.Query<ArtistEntity>().Select(a =>new {a.Sex, a.Dead}).Distinct().ToList();
    }

    [Fact]
    public void DistinctFie()
    {
        var authors = Database.Query<AlbumEntity>().Select(a => a.Label).Distinct().ToList();
    }

    [Fact]
    public void DistinctFieExpanded()
    {
        var authors = Database.Query<AlbumEntity>().Where(a => a.Year != 0).Select(a => a.Label).Distinct().ToList();
    }

    [Fact]
    public void DistinctIb()
    {
        var authors = Database.Query<AlbumEntity>().Select(a => a.Author).Distinct().ToList();
    }

    [Fact]
    public void DistinctCount()
    {
        var count1 = Database.Query<AlbumEntity>().Select(a => a.Name).Distinct().Select(a => a).Count();
        var count2 = Database.Query<AlbumEntity>().Select(a => a.Name).Distinct().ToList().Count();
        Assert.Equal(count1, count2);
    }


    [Fact]
    public void DistinctTake()
    {
        var bla = Database.Query<BandEntity>().SelectMany(a => a.Members.SelectMany(m => m.Friends).Distinct()).Take(4).ToList();
    }

    [Fact]
    public void GroupTake()
    {
        var bla = (from b in Database.Query<BandEntity>()
                  from g in b.Members.GroupBy(a=>a.Sex).Select(gr=> new {gr.Key, Count = gr.Count() })
                  select new
                  {
                      Band = b.ToLite(),
                      g.Key,
                      g.Count
                  }).Take(2).ToList();

    }

    [Fact]
    public void DistinctWithCheapNullPropagation()
    {
        var nullableList = Database.Query<ArtistEntity>().Select(a => a == null ? (Sex?)null : a.Sex).Distinct().ToList();
        var notNullableList = Database.Query<ArtistEntity>().Select(a => a.Sex).Distinct().ToList();

        Assert.Equal(nullableList.Count, notNullableList.Count);
    
    }
}
