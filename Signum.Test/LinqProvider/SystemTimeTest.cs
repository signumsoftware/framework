using System;
using System.Linq;
using Xunit;
using Signum.Engine;
using Signum.Utilities;
using Signum.Entities;
using Signum.Test.Environment;
using Signum.Utilities.DataStructures;

namespace Signum.Test.LinqProvider
{
    public class SystemTimeTest
    {
        public SystemTimeTest()
        {
            MusicStarter.StartAndLoad();
            Connector.CurrentLogger = new DebugTextWriter();
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

            using (SystemTime.Override(new SystemTime.All()))
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

            Interval<DateTime> period;
            using (SystemTime.Override(new SystemTime.All()))
            {
                period = Database.Query<FolderEntity>().Where(a => a.Name == "X2").Select(a => a.SystemPeriod()).Single();
            }

            period = new Interval<DateTime>(
                new DateTime(period.Min.Ticks, DateTimeKind.Utc),
                new DateTime(period.Max.Ticks, DateTimeKind.Utc)); //Hack 

            using (SystemTime.Override(new SystemTime.AsOf(period.Min)))
            {
                var list = Database.Query<FolderEntity>().Where(f1 => f1.Name == "X2").Select(a => a.SystemPeriod()).ToList();
            }

            using (SystemTime.Override(new SystemTime.Between(period.Max, period.Max.AddSeconds(1))))
            {
                var list = Database.Query<FolderEntity>().Where(f1 => f1.Name == "X2").Select(a => a.SystemPeriod()).ToList();
            }

            using (SystemTime.Override(new SystemTime.ContainedIn(period.Max, period.Max.AddSeconds(1))))
            {
                var list = Database.Query<FolderEntity>().Where(f2 => f2.Name == "X2").Select(a => a.SystemPeriod()).ToList();
            }
        }
    }
}
