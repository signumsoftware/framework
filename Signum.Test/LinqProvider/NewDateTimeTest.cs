using Signum.Engine.Maps;
using Signum.Utilities;
using System.Diagnostics;

namespace Signum.Test.LinqProvider;

public class NewDateTimeTest
{
    public NewDateTimeTest()
    {
        MusicStarter.StartAndLoad();
        Connector.CurrentLogger = new DebugTextWriter();
    }


    [Fact]
    public void NewDateTime()
    {
        var list = Database.Query<NoteWithDateEntity>()
            .OrderBy(n => new DateTime(2020, 1, 1))
            .Select(n => n.Id)
            .ToList();
    }

    [Fact]
    public void NewDateTimeHMS()
    {
        var list = Database.Query<NoteWithDateEntity>()
            .OrderBy(n => new DateTime(2020, 1, 1, 12, 30, 0))
            .Select(n => n.Id)
            .ToList();
    }

    [Fact]
    public void NewDateTimeHMSMS()
    {
        var list = Database.Query<NoteWithDateEntity>()
            .OrderBy(n => new DateTime(2020, 1, 1, 12, 30, 0, 500))
            .Select(n => n.Id)
            .ToList();
    }


    [Fact]
    public void NewDateOnly()
    {
        var list = Database.Query<NoteWithDateEntity>()
            .OrderBy(n => new DateOnly(2020, 1, 1))
            .Select(n => n.Id)
            .ToList();
    }

 

    [Fact]
    public void NewTimeOnly()
    {
        var list = Database.Query<NoteWithDateEntity>()
            .OrderBy(n => new TimeOnly(12, 30, 0))
            .Select(n => n.Id)
            .ToList();
    }

 

    [Fact]
    public void NewTimeSpan()
    {
        var list = Database.Query<NoteWithDateEntity>()
            .OrderBy(n => new TimeSpan(12, 30, 0))
            .Select(n => n.Id)
            .ToList();
    }

  
}
