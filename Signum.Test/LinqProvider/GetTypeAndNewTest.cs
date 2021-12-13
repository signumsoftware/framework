using Signum.Utilities.DataStructures;
using System.IO;
using System.Text;
using Xunit.Abstractions;

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
        OutputHelper.WriteLine(value);
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


}
