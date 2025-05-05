using Signum.DynamicQuery;
using Signum.Entities;
using Signum.Utilities.DataStructures;

namespace Signum.Test.LinqProvider;

public class SystemTimeTest
{
    public SystemTimeTest()
    {
        MusicStarter.StartAndLoad();
        Connector.CurrentLogger = new DebugTextWriter();
    }


    [Fact]
    public void SystemPeriodUTC()
    {
        if (!Connector.Current.SupportsTemporalTables)
            return;

        var list = (from f in Database.Query<NoteWithDateEntity>()
                    select f.CreationTime).ToList();


        Assert.Equal(DateTimeKind.Local, list.Select(a => a.Kind).Distinct().SingleEx());

        using (SystemTime.Override(new SystemTime.All(SystemTimeJoinMode.FirstCompatible)))
        {
            var listSP = (from f in Database.Query<FolderEntity>()
                          select f.SystemPeriod()).ToList();

            Assert.Equal(DateTimeKind.Utc, listSP.Select(a => a.Min!.Value.Kind).Distinct().SingleEx());


            var listMin = (from f in Database.Query<FolderEntity>()
                           select f.SystemPeriod().Min!.Value.InSql()).ToList();

            Assert.Equal(DateTimeKind.Utc, listMin.Select(a => a.Kind).Distinct().SingleEx());


            var listMinPlus1 = (from f in Database.Query<FolderEntity>()
                           select f.SystemPeriod().Min!.Value.AddDays(1).InSql()).ToList();

            Assert.Equal(DateTimeKind.Utc, listMinPlus1.Select(a => a.Kind).Distinct().SingleEx());
        }
        
    }

    [Fact]
    public void SystemValidParameterValidation()
    {
        if (!Connector.Current.SupportsTemporalTables)
            return;

        Database.Query<NoteWithDateEntity>().Count(a => a.CreationTime > DateTime.Now);
        Assert.Throws<InvalidDateTimeKindException>(() => Database.Query<NoteWithDateEntity>().Count(a => a.CreationTime > DateTime.UtcNow));


        Assert.Throws<InvalidDateTimeKindException>(() => Database.Query<FolderEntity>().Count(f => f.SystemPeriod().Min!.Value > DateTime.Now));
        Database.Query<FolderEntity>().Count(f => f.SystemPeriod().Min!.Value > DateTime.UtcNow);
    }

    [Fact]
    public void TimePresent()
    {
        if (!Connector.Current.SupportsTemporalTables)
            return;

        var list = (from f in Database.Query<FolderEntity>()
                    where f.Parent != null
                    select new { f.Name, Parent = f.Parent!.Entity.Name }).ToList();

        Assert.Empty(list);
    }


    [Fact]
    public void TimeAll()
    {
        if (!Connector.Current.SupportsTemporalTables)
            return;

        using (SystemTime.Override(new SystemTime.All(SystemTimeJoinMode.AllCompatible)))
        {
            var list = (from f in Database.Query<FolderEntity>()
                        where f.Parent != null
                        select new
                        {
                            f.Name,
                            Period = f.SystemPeriod(),
                            Parent = f.Parent!.Entity.Name,
                            ParentPeriod = f.Parent!.Entity.SystemPeriod()
                        }).ToList();

            Assert.True(list.All(a => a.Period.Overlaps(a.ParentPeriod)));
        }
    }


    [Fact]
    public void TimeBetween()
    {
        if (!Connector.Current.SupportsTemporalTables)
            return;

        NullableInterval<DateTime> period;
        using (SystemTime.Override(new SystemTime.All(SystemTimeJoinMode.AllCompatible)))
        {
            period = Database.Query<FolderEntity>().Where(a => a.Name == "X2").Select(a => a.SystemPeriod()).Single();
        }


        using (SystemTime.Override(new SystemTime.AsOf(period.Min!.Value)))
        {
            var list = Database.Query<FolderEntity>().Where(f1 => f1.Name == "X2").Select(a => a.SystemPeriod()).ToList();
        }

        using (SystemTime.Override(new SystemTime.Between(period.Max!.Value, period.Max.Value.AddSeconds(1), SystemTimeJoinMode.AllCompatible)))
        {
            var list = Database.Query<FolderEntity>().Where(f1 => f1.Name == "X2").Select(a => a.SystemPeriod()).ToList();
        }

        using (SystemTime.Override(new SystemTime.ContainedIn(period.Max.Value, period.Max.Value.AddSeconds(1), SystemTimeJoinMode.AllCompatible)))
        {
            var list = Database.Query<FolderEntity>().Where(f2 => f2.Name == "X2").Select(a => a.SystemPeriod()).ToList();
        }
    }

    [Fact]
    public void TimeSeriesOneValue()
    {
        if (!Connector.Current.SupportsTemporalTables)
            return;

        DateTime min;
        using (SystemTime.Override(new SystemTime.All(SystemTimeJoinMode.AllCompatible)))
        {
            min = Database.Query<FolderEntity>().Min(a => a.SystemPeriod().Min!.Value);
        }

        var series = QueryTimeSeriesLogic.GetDatesInRange(
              min,
              min.AddSeconds(2),
              TimeSeriesUnit.Millisecond.ToString(),
              50)
            .Select(dv =>
             new
             {
                 dv.Date,
                 Count = Database.Query<FolderEntity>().OverrideSystemTime(new SystemTime.AsOf(dv.Date)).Count()
             }).ToList();

    }

    [Fact]
    public void TimeSeriesManyValue()
    {
        if (!Connector.Current.SupportsTemporalTables)
            return;

        DateTime min;
        using (SystemTime.Override(new SystemTime.All(SystemTimeJoinMode.AllCompatible)))
        {
            min = Database.Query<FolderEntity>().Min(a => a.SystemPeriod().Min!.Value);
        }

        var series = QueryTimeSeriesLogic.GetDatesInRange(
              min,
              min.AddSeconds(2),
              TimeSeriesUnit.Millisecond.ToString(),
              50)
            .SelectMany(dv =>
             Database.Query<FolderEntity>().OverrideSystemTime(new SystemTime.AsOf(dv.Date)).Select(f => new
             {
                 dv.Date,
                 Folder = f
             })).ToList();

    }
}
