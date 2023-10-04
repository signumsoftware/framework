
namespace Signum.Test.LinqProvider;

/// <summary>
/// Summary description for SelectManyTest
/// </summary>
#pragma warning disable xUnit1031 // Do not use blocking task operations in test method
public class AsyncTest
{
    public AsyncTest()
    {
        MusicStarter.StartAndLoad();
        Connector.CurrentLogger = new DebugTextWriter();
    }

    [Fact]
    public void ToListAsync()
    {
        var artistsInBands = Database.Query<BandEntity>().ToListAsync().Result;
    }

    [Fact]
    public void ToArrayAsync()
    {
        var artistsInBands = Database.Query<BandEntity>().ToArrayAsync().Result;
    }

    [Fact]
    public void AverageAsync()
    {
        var artistsInBands = Database.Query<BandEntity>().AverageAsync(a=>a.Members.Count).Result;
    }


    [Fact]
    public void MinAsync()
    {
        var artistsInBands = Database.Query<BandEntity>().MinAsync(a => a.Members.Count).Result;
    }
}
#pragma warning restore xUnit1031 // Do not use blocking task operations in test method
