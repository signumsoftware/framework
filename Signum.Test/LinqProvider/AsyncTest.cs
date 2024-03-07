
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
    public async void ToListAsync()
    {
        var artistsInBands = await Database.Query<BandEntity>().ToListAsync();
    }

    [Fact]
    public async void ToArrayAsync()
    {
        var artistsInBands = await Database.Query<BandEntity>().ToArrayAsync();
    }

    [Fact]
    public async void AverageAsync()
    {
        var artistsInBands = await Database.Query<BandEntity>().AverageAsync(a => a.Members.Count);
    }


    [Fact]
    public async void MinAsync()
    {
        var artistsInBands = await Database.Query<BandEntity>().MinAsync(a => a.Members.Count);
    }
}
#pragma warning restore xUnit1031 // Do not use blocking task operations in test method
