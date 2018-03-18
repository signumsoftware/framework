using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signum.Engine;
using Signum.Utilities;
using Signum.Entities;
using Signum.Utilities.ExpressionTrees;
using Signum.Test.Environment;
using Signum.Utilities.DataStructures;

namespace Signum.Test.LinqProvider
{
    [TestClass]
    public class SystemTimeTest
    {
        [ClassInitialize()]
        public static void MyClassInitialize(TestContext testContext)
        {
            MusicStarter.StartAndLoad();
        }

        [TestInitialize]
        public void Initialize()
        {
            Connector.CurrentLogger = new DebugTextWriter();
        }
        

        [TestMethod]
        public void TimePresent()
        {
            var list = (from f in Database.Query<FolderEntity>()
                        where f.Parent != null
                        select new { f.Name, Parent = f.Parent.Entity.Name }).ToList();

            Assert.AreEqual(0, list.Count);
        }


        [TestMethod]
        public void TimeAll()
        {
            using (SystemTime.Override(new SystemTime.All()))
            {
                var list = (from f in Database.Query<FolderEntity>()
                            where f.Parent != null
                            select new
                            {
                                f.Name,
                                Period = f.SystemPeriod(),
                                Parent = f.Parent.Entity.Name,
                                ParentPeriod = f.Parent.Entity.SystemPeriod()
                            }).ToList();

                Assert.IsTrue(list.All(a => a.Period.Overlaps(a.ParentPeriod)));
            }
        }


        [TestMethod]
        public void TimeBetween()
        {
            Interval<DateTime> period;
            using (SystemTime.Override(new SystemTime.All()))
            {
                period = Database.Query<FolderEntity>().Where(a => a.Name == "X2").Select(a => a.SystemPeriod()).Single();
            }

            using (SystemTime.Override(new SystemTime.AsOf(period.Min)))
            {
                var a = Database.Query<FolderEntity>().Where(f1 => f1.Name == "X2").ToList();
            }

            using (SystemTime.Override(new SystemTime.Between(period.Max, period.Max.AddSeconds(1))))
            {
                var a = Database.Query<FolderEntity>().Where(f1 => f1.Name == "X2").ToList();
            }

            using (SystemTime.Override(new SystemTime.FromTo(period.Max, period.Max.AddSeconds(1))))
            {
                var b = Database.Query<FolderEntity>().Where(f2 => f2.Name == "X2").ToList();
            }

        }



    }
}
