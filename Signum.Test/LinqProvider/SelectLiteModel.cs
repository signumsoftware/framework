
namespace Signum.Test.LinqProvider;

/// <summary>
/// Summary description for LinqProvider
/// </summary>
public class SelectLiteModel
{
    public SelectLiteModel()
    {
        MusicStarter.StartAndLoad();
        Connector.CurrentLogger = new DebugTextWriter();
    }

    [Fact]
    public void SelectAwardLiteModel()
    {
        var awards = Database.Query<AwardNominationEntity>().Where(a => a.Award != null).Select(a => a.Award).ToList();

        foreach (var a in awards)
        {
            if (a is Lite<AmericanMusicAwardEntity>)
                Assert.True(a.Model is AwardLiteModel); //Override for type
            else if (a is Lite<GrammyAwardEntity>)
                Assert.True(a.Model is AwardLiteModel); //Property Attribute
            else
                Assert.True(a.Model is string); //Globally

        }
    }
}
