
namespace Signum.Test.LinqProvider;

/// <summary>
/// Summary description for LinqProvider
/// </summary>
public class SelectNestedTest
{
    public SelectNestedTest()
    {
        MusicStarter.StartAndLoad();
        Connector.CurrentLogger = new DebugTextWriter();
    }

    [Fact]
    public void SelecteNested()
    {
        var neasted = (from l in Database.Query<LabelEntity>()
                       select (from a in Database.Query<AlbumEntity>()
                               where a.Label.Is(l)
                               select  a.ToLite()).ToList()).ToList();
    }

    [Fact]
    public void SelecteNestedIB()
    {
        var neasted = (from b in Database.Query<BandEntity>()
                       select (from a in Database.Query<AlbumEntity>()
                               where a.Author == b
                               select a.ToLite()).ToList()).ToList();
    }

    [Fact]
    public void SelecteNullableLookupColumns()
    {
        var neasted = (from l in Database.Query<LabelEntity>()
                       join o in Database.Query<LabelEntity>().DefaultIfEmpty() on l.Owner!.Entity equals o
                       group l.ToLite() by o.ToLite() into g
                       select new
                       {
                           Owner = g.Key,
                           List = g.ToList(),
                           Count = g.Count()
                       }).ToList();

    }

    [Fact]
    public void SelecteGroupBy()
    {
        var neasted = (from l in Database.Query<LabelEntity>()
                       group l.ToLite() by l.Owner into g
                       select new
                       {
                           Owner = g.Key,
                           List = g.ToList(),
                           Count = g.Count()
                       }).ToList();

    }

    [Fact]
    public void SelecteNestedIBPlus()
    {
        var neasted = (from l in Database.Query<LabelEntity>()
                       select (from a in Database.Query<AlbumEntity>()
                               where a.Label.Is(l)
                               select new { Label = l.ToLite(), Author = a.Author.ToLite(), Album = a.ToLite() }).ToList()).ToList();
    }

    [Fact]
    public void SelecteNestedNonKey()
    {
        var neasted = (from a in Database.Query<AlbumEntity>()
                       select new
                           {
                               Alumum = a.ToLite(),
                               Friends = (from b in Database.Query<AlbumEntity>()
                                          where a.Label.Is(b.Label)
                                          select b.ToLite()).ToList()
                           }).ToList();
    }

    [Fact]
    public void SelecteNestedContanins()
    {
        var neasted = (from a in Database.Query<ArtistEntity>()
                       select (from b in Database.Query<BandEntity>()
                               where b.Members.Contains(a)
                               select b.ToLite()).ToList()).ToList();
    }

    [Fact]
    public void SelecteNestedIndePendent1()
    {
        var neasted = (from a in Database.Query<LabelEntity>()
                       select (from n in Database.Query<NoteWithDateEntity>()
                               select n.ToLite()).ToList()).ToList();
    }

    [Fact]
    public void SelecteNestedIndePendent2()
    {
        var neasted = (from a in Database.Query<LabelEntity>()
                       select new
                       {
                           Label = a.ToLite(),
                           Notes = (from n in Database.Query<NoteWithDateEntity>()
                                    select n.ToLite()).ToList()
                       }).ToList();
    }

    [Fact]
    public void SelecteNestedSemiIndePendent()
    {
        var neasted = (from a in Database.Query<LabelEntity>()
                       select (from n in Database.Query<NoteWithDateEntity>()
                               select new
                               {
                                   Note = n.ToLite(),
                                   Label = a.ToLite(),
                               }).ToList()).ToList();
    }

    [Fact]
    public void SelecteNestedOuterOrder()
    {
        var neasted = (from l in Database.Query<LabelEntity>()
                       orderby l.Name
                       select new
                       {
                           Label = l.ToLite(),
                           Notes = (from a in Database.Query<AlbumEntity>()
                                    where a.Label.Is(l)
                                    select a.ToLite()).ToList()
                       }).ToList();
    }

    [Fact]
    public void SelecteNestedOuterOrderTake()
    {
        var neasted = (from l in Database.Query<LabelEntity>()
                       orderby l.Name
                       select new
                       {
                           Label = l.ToLite(),
                           Notes = (from a in Database.Query<AlbumEntity>()
                                    where a.Label.Is(l)
                                    select a.ToLite()).ToList()
                       }).Take(10).ToList();
    }

    [Fact]
    public void SelecteNestedInnerOrder()
    {
        var neasted = (from l in Database.Query<LabelEntity>()
                       select new
                       {
                           Label = l.ToLite(),
                           Notes = (from a in Database.Query<AlbumEntity>()
                                    where a.Label.Is(l)
                                    orderby a.Name
                                    select a.ToLite()).ToList()
                       }).ToList();
    }

    [Fact]
    public void SelecteNestedInnerOrderTake()
    {
        var neasted = (from l in Database.Query<LabelEntity>()
                       select new
                       {
                           Label = l.ToLite(),
                           Notes = (from a in Database.Query<AlbumEntity>()
                                    where a.Label.Is(l)
                                    orderby a.Name
                                    select a.ToLite()).Take(10).ToList()
                       }).ToList();
    }

    [Fact]
    public void SelecteDoubleNested()
    {
        var neasted = (from l in Database.Query<LabelEntity>()
                       select (from a in Database.Query<AlbumEntity>()
                               where a.Label.Is(l)
                               select (from s in a.Songs
                                       select "{0} - {1} - {2}".FormatWith(l.Name, a.Name, s.Name)).ToList()).ToList()).ToList();
    }

    [Fact]
    public void SelecteNestedDoubleOrder()
    {
        var neasted = (from l in Database.Query<LabelEntity>()
                       orderby l.Name
                       select (from a in Database.Query<AlbumEntity>()
                               where a.Label.Is(l)
                               orderby a.Name
                               select a.Name).ToList()).ToList();
    }


    [Fact]
    public void SelecteDoubleNestedDoubleOrder()
    {
        var neasted = (from l in Database.Query<LabelEntity>()
                       orderby l.Name
                       select (from a in Database.Query<AlbumEntity>()
                               where a.Label.Is(l)
                               orderby a.Name
                               select (from s in a.Songs
                                       select "{0} - {1} - {2}".FormatWith(l.Name, a.Name, s.Name)).ToList()).ToList()).ToList();
    }



    [Fact]
    public void SelectContainsInt()
    {
        var result = (from b in Database.Query<BandEntity>()
                      where b.Members.Select(a => a.Id).Contains(1)
                      select b.ToLite()).ToList();
    }

    [Fact]
    public void SelectContainsEnum()
    {
        var result = (from b in Database.Query<BandEntity>()
                      where b.Members.Select(a => a.Sex).Contains(Sex.Female)
                      select b.ToLite()).ToList();
    }


    //[Fact]
    //public void SelecteNestedAsQueryable()
    //{
    //    var neasted = (from l in Database.Query<LabelEntity>()
    //                   select (from a in Database.Query<AlbumEntity>()
    //                           where a.Label == l
    //                           select a.ToLite())).ToList();
    //}

    //[Fact]
    //public void SelecteNestedAsQueryableAnonymous()
    //{
    //    var neasted = (from l in Database.Query<LabelEntity>()
    //                   select new { Elements = (from a in Database.Query<AlbumEntity>()
    //                           where a.Label == l
    //                           select a.ToLite())} ).ToList();
    //}


}
