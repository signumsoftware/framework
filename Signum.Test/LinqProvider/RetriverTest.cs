
namespace Signum.Test.LinqProvider;

/// <summary>
/// Summary description for LinqProvider
/// </summary>
public class RetrieverTest
{
    public RetrieverTest()
    {
        MusicStarter.StartAndLoad();
        Connector.CurrentLogger = new DebugTextWriter();
    }

    [Fact]
    public void RetrieveSimple()
    {
        var list = Database.Query<CountryEntity>().ToList();

        AssertRetrieved(list);
    }

    [Fact]
    public void RetrieveWithEnum()
    {
        var list = Database.Query<GrammyAwardEntity>().ToList();

        AssertRetrieved(list);
    }


    [Fact]
    public void RetrieveWithRelatedEntityAndLite()
    {
        var list = Database.Query<LabelEntity>().ToList();

        AssertRetrieved(list);
    }

    [Fact]
    public void RetrieveWithIBA()
    {
        var list = Database.Query<NoteWithDateEntity>().ToList();

        AssertRetrieved(list);
    }

    [Fact]
    public void RetrieveWithMList()
    {
        var list = Database.Query<ArtistEntity>().ToList();

        AssertRetrieved(list);
    }

    [Fact]
    public void RetrieveWithMListEmbedded()
    {
        var list = Database.Query<AlbumEntity>().ToList();

        AssertRetrieved(list);
    }

    private void AssertRetrieved<T>(List<T> list) where T:Modifiable
    {
        var graph = GraphExplorer.FromRoots(list);

        var problematic = graph.Where(a =>
            a.IsGraphModified &&
            a is Entity && (((Entity)a).IdOrNull == null || ((Entity)a).IsNew));

        if (problematic.Any())
            Assert.Fail("Some non-retrived elements: {0}".FormatWith(problematic.ToString(", ")));
    }


    [Fact]
    public void RetrieveWithMListCount()
    {
        var artist = Database.Query<ArtistEntity>().OrderBy(a => a.Name).First();

        Assert.Equal(artist.ToLite().RetrieveAndRemember().Friends.Count, artist.Friends.Count);
    }
}
