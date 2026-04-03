using System.IO;
using System.Text;

namespace Signum.Test.LinqProvider;

public class TestOutputTextWriter : TextWriter
{
    public int Lines = 0;

    public ITestOutputHelper OutputHelper;

    public TestOutputTextWriter(ITestOutputHelper outputHelper)
    {
        OutputHelper = outputHelper;
    }

    public override void Write(char[] buffer, int index, int count)
    {
        Lines++;
        OutputHelper.WriteLine(new String(buffer, index, count));
    }

    public override void Write(string? value)
    {
        Lines++;
        OutputHelper.WriteLine(value ?? "");
    }

    public override Encoding Encoding
    {
        get { return Encoding.Default; }
    }
}

public class GetTypeAndNewTest
{
    public GetTypeAndNewTest(ITestOutputHelper output)
    {
        MusicStarter.StartAndLoad();
        Connector.CurrentLogger = new TestOutputTextWriter(output);
    }

    [Fact]
    public void TestGetType()
    {
        var list = (from f in Database.Query<ArtistEntity>()
                    where f.GetType() == typeof(ArtistEntity)
                    select new { f.Name }).ToList();

        Assert.NotEmpty(list);
    }

    [Fact]
    public void TestGetTypeFullName()
    {
        var list = (from f in Database.Query<ArtistEntity>()
                    where f.GetType().FullName == typeof(ArtistEntity).FullName
                    select new { f.Name }).ToList();

        Assert.NotEmpty(list);
    }


    [Fact]
    public void TestGetTypeIBFullName()
    {
        var list = (from f in Database.Query<AlbumEntity>()
                    where f.Author.GetType().FullName == typeof(ArtistEntity).FullName
                    select new { f.Name }).ToList();

        Assert.NotEmpty(list);
    }

    [Fact]
    public void TestGetTypeNiceName()
    {
        var list = (from f in Database.Query<ArtistEntity>()
                    where f.GetType().NiceName() == typeof(ArtistEntity).NiceName()
                    select new { f.Name }).ToList();

        Assert.NotEmpty(list);
    }

    [Fact]
    public void TestGetTypeIBNiceName()
    {
        var list = (from f in Database.Query<AlbumEntity>()
                    where f.Author.GetType().NiceName() == typeof(ArtistEntity).NiceName()
                    select new { f.Name }).ToList();

        Assert.NotEmpty(list);
    }

    [Fact]
    public void TestIsNew()
    {
        var list = (from f in Database.Query<AlbumEntity>()
                    where (f.IsNew ? "New" : "Old") == "Old"
                    select new { f.Name }).ToList();

        Assert.NotEmpty(list);
    }

    [Fact]
    public void SelectToTypeEntity()
    {
        var list = (from f in Database.Query<ArtistEntity>()
                    select f.GetType().ToTypeEntity())
                    .ToList();

        Assert.NotEmpty(list);
    }

    [Fact]
    public void SelectToTypeEntity_UpCast()
    {
        var list = (from f in Database.Query<ArtistEntity>()
                    select ((Entity)f).GetType().ToTypeEntity())
                    .ToList();

        Assert.NotEmpty(list);
    }

    [Fact]
    public void SelectToTypeEntity_UpCast_Pushed()
    {
        var list = (from f in Database.Query<ArtistEntity>()
                    select ((Lite<Entity>)f.ToLite()).Entity.GetType().ToTypeEntity())
                    .ToList();

        Assert.NotEmpty(list);
    }

    [Fact]
    public void SelectToTypeLite()
    {
        var list = (from f in Database.Query<ArtistEntity>()
                    where f.ToLite().EntityType.ToTypeEntity().Is(typeof(ArtistEntity).ToTypeEntity())
                    select f)
                    .ToList();

        Assert.NotEmpty(list);
    }

    //class Juas
    //{
    //    public Lite<IAuthorEntity> Author;
    //}

    //[Fact]
    //public void SelectToTypeEntity_UpCast_Pushed2()
    //{
    //    var query = (from f in Database.Query<ArtistEntity>()
    //                 select new Juas
    //                 {
    //                     Author = f.ToLite()
    //                 });

    //    var list = query.Where(a => a.Author.EntityType.ToTypeEntity().Is(typeof(ArtistEntity).ToTypeEntity())).ToList();

    //    Assert.NotEmpty(list);
    //}


    [Fact]
    public void WhereToTypeEntity()
    {
        var list = (from f in Database.Query<ArtistEntity>()
                    where f.GetType().ToTypeEntity().Is(typeof(ArtistEntity).ToTypeEntity())
                    select f)
                    .ToList();


        Assert.NotEmpty(list);
    }

    [Fact]
    public void SelectToTypeEntityIB()
    {
        var list = (from f in Database.Query<BandEntity>()
                    select f.LastAward!.GetType().ToTypeEntity())
                    .ToList();

        Assert.NotEmpty(list);
    }

    [Fact]
    public void WhereToTypeEntityIB()
    {
        var list = (from f in Database.Query<BandEntity>()
                    where f.LastAward!.GetType().ToTypeEntity().Is(typeof(GrammyAwardEntity).ToTypeEntity())
                    select f)
                    .ToList();

        Assert.NotEmpty(list);
    }

    [Fact]
    public void WhereToTypeEntityIBGroupBy()
    {
        var list = (from f in Database.Query<BandEntity>()
                    group f by f.LastAward!.GetType().ToTypeEntity() into g
                    select new { g.Key, Count = g.Count() })
                    .ToList();

        Assert.NotEmpty(list);
    }

    [Fact]
    public void SelectToTypeEntityIBA()
    {
        var list = (from f in Database.Query<NoteWithDateEntity>()
                    select f.Target.GetType().ToTypeEntity())
                    .ToList();

        Assert.NotEmpty(list);
    }

    [Fact]
    public void WhereToTypeEntityIBA()
    {
        var list = (from f in Database.Query<NoteWithDateEntity>()
                    where f.Target.GetType().ToTypeEntity().Is(typeof(ArtistEntity).ToTypeEntity())
                    select f)
                    .ToList();

        Assert.NotEmpty(list);
    }

    [Fact]
    public void WhereToTypeEntityIBAGroupBy()
    {
        var list = (from f in Database.Query<NoteWithDateEntity>()
                    group f by f.Target.GetType().ToTypeEntity() into g
                    select new { g.Key, Count = g.Count() })
                    .ToList();

        Assert.NotEmpty(list);
    }
}
