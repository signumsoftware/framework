using Microsoft.SqlServer.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signum.Engine;
using Signum.Engine.Maps;
using Signum.Entities;
using Signum.Test.Environment;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

namespace Signum.Test.LinqProvider
{
    /// <summary>
    /// Summary description for LinqProvider
    /// </summary>
    [TestClass]
    public class SqlFunctionsTest
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
        public void StringFunctions()
        {
            var artists = Database.Query<ArtistEntity>();
            Assert.IsTrue(artists.Any(a => a.Name.IndexOf('M') == 0));
            Assert.IsTrue(artists.Any(a => a.Name.IndexOf("Mi") == 0));
            Assert.IsTrue(artists.Any(a => a.Name.Contains("Jackson")));
            Assert.IsTrue(artists.Any(a => a.Name.StartsWith("Billy")));
            Assert.IsTrue(artists.Any(a => a.Name.EndsWith("Corgan")));
            Assert.IsTrue(artists.Any(a => a.Name.Like("%Michael%")));
            Assert.IsTrue(artists.Count(a => a.Name.EndsWith("Orri Páll Dýrason")) == 1);
            Assert.IsTrue(artists.Count(a => a.Name.StartsWith("Orri Páll Dýrason")) == 1);

            Dump((ArtistEntity a) => a.Name.Length);
            Dump((ArtistEntity a) => a.Name.ToLower());
            Dump((ArtistEntity a) => a.Name.ToUpper());
            Dump((ArtistEntity a) => a.Name.TrimStart());
            Dump((ArtistEntity a) => a.Name.TrimEnd());
            Dump((ArtistEntity a) => a.Name.Substring(2).InSql());
            Dump((ArtistEntity a) => a.Name.Substring(2, 2).InSql());

            Dump((ArtistEntity a) => a.Name.Start(2).InSql());
            Dump((ArtistEntity a) => a.Name.End(2).InSql());
            Dump((ArtistEntity a) => a.Name.Reverse().InSql());
            Dump((ArtistEntity a) => a.Name.Replicate(2).InSql());
        }

        [TestMethod]
        public void StringFunctionsPolymorphicUnion()
        {
            Assert.IsTrue(Database.Query<AlbumEntity>().Any(a => a.Author.CombineUnion().Name.Contains("Jackson")));
        }

        [TestMethod]
        public void StringFunctionsPolymorphicSwitch()
        {
            Assert.IsTrue(Database.Query<AlbumEntity>().Any(a => a.Author.CombineCase().Name.Contains("Jackson")));
        }

        [TestMethod]
        public void CoalesceFirstOrDefault()
        {
            var list = Database.Query<BandEntity>()
               .Select(b => b.Members.FirstOrDefault(a => a.Sex == Sex.Female) ?? b.Members.FirstOrDefault(a => a.Sex == Sex.Male))
               .Select(a => a.ToLite()).ToList();
        }

        [TestMethod]
        public void StringContainsUnion()
        {
            var list = Database.Query<AlbumEntity>().Where(a => !a.Author.CombineUnion().ToString().Contains("Hola")).ToList();
        }

        [TestMethod]
        public void StringContainsSwitch()
        {
            var list = Database.Query<AlbumEntity>().Where(a => !a.Author.CombineCase().ToString().Contains("Hola")).ToList();
        }

        [TestMethod]
        public void DateTimeFunctions()
        {
            Dump((NoteWithDateEntity n) => n.CreationTime.Year);
            Dump((NoteWithDateEntity n) => n.CreationTime.Month);
            Dump((NoteWithDateEntity n) => n.CreationTime.Day);
            Dump((NoteWithDateEntity n) => n.CreationTime.DayOfYear);
            Dump((NoteWithDateEntity n) => n.CreationTime.Hour);
            Dump((NoteWithDateEntity n) => n.CreationTime.Minute);
            Dump((NoteWithDateEntity n) => n.CreationTime.Second);
            Dump((NoteWithDateEntity n) => n.CreationTime.Millisecond);
        }

        [TestMethod]
        public void DateTimeDayOfWeek()
        {
            //var list = Database.Query<ArtistEntity>().GroupBy(a => a.Sex).Select(gr => new { gr.Key, Count = gr.Count() }).ToList();
            var list = Database.Query<NoteWithDateEntity>().GroupBy(a => a.CreationTime.DayOfWeek).Select(gr => new { gr.Key, Count = gr.Count() }).ToList();

            var list2 = Database.Query<NoteWithDateEntity>().Where(a => a.CreationTime.DayOfWeek == DayOfWeek.Sunday).ToList();
        }

        [TestMethod]
        public void DateDiffFunctions()
        {
            Dump((NoteWithDateEntity n) => (n.CreationTime - n.CreationTime).TotalDays.InSql());
            Dump((NoteWithDateEntity n) => (n.CreationTime - n.CreationTime).TotalHours.InSql());
            Dump((NoteWithDateEntity n) => (n.CreationTime - n.CreationTime).TotalMinutes.InSql());
            Dump((NoteWithDateEntity n) => (n.CreationTime - n.CreationTime).TotalSeconds.InSql());
            Dump((NoteWithDateEntity n) => (n.CreationTime.AddDays(1) - n.CreationTime).TotalMilliseconds.InSql());
        }

        [TestMethod]
        public void DateFunctions()
        {
            Dump((NoteWithDateEntity n) => n.CreationTime.Date);

            if (Schema.Current.Settings.IsDbType(typeof(TimeSpan)))
            {
                Dump((NoteWithDateEntity n) => n.CreationTime.TimeOfDay);
            }
        }

        [TestMethod]
        public void DayOfWeekFunction()
        {
            var list = Database.Query<NoteWithDateEntity>().Where(n => n.CreationTime.DayOfWeek != DayOfWeek.Sunday)
                .Select(n => n.CreationTime.DayOfWeek).ToList();
        }

        [TestMethod]
        public void TimeSpanFunction()
        {
            if (!Schema.Current.Settings.IsDbType(typeof(TimeSpan)))
                return;

            var durations = Database.MListQuery((AlbumEntity a) => a.Songs).Select(mle => mle.Element.Duration).Where(d => d != null);

            Debug.WriteLine(durations.Select(d => d.Value.Hours.InSql()).ToString(", "));
            Debug.WriteLine(durations.Select(d => d.Value.Minutes.InSql()).ToString(", "));
            Debug.WriteLine(durations.Select(d => d.Value.Seconds.InSql()).ToString(", "));
            Debug.WriteLine(durations.Select(d => d.Value.Milliseconds.InSql()).ToString(", "));


            Debug.WriteLine((from n in Database.Query<NoteWithDateEntity>()
                             from d in Database.MListQuery((AlbumEntity a) => a.Songs)
                             where d.Element.Duration != null
                             select (n.CreationTime + d.Element.Duration.Value).InSql()).ToString(", "));

            Debug.WriteLine((from n in Database.Query<NoteWithDateEntity>()
                             from d in Database.MListQuery((AlbumEntity a) => a.Songs)
                             where d.Element.Duration != null
                             select (n.CreationTime - d.Element.Duration.Value).InSql()).ToString(", "));
        }


        [TestMethod]
        public void SqlHierarchyIdFunction()
        {
            if (!Schema.Current.Settings.UdtSqlName.ContainsKey(typeof(SqlHierarchyId)))
                return;


            var nodes = Database.Query<LabelEntity>().Select(a => a.Node);

            Debug.WriteLine(nodes.Select(n => n.GetAncestor(0).InSql()).ToString(", "));
            Debug.WriteLine(nodes.Select(n => n.GetAncestor(1).InSql()).ToString(", "));
            Debug.WriteLine(nodes.Select(n => (int)(short)n.GetLevel().InSql()).ToString(", "));
            Debug.WriteLine(nodes.Select(n => n.ToString().InSql()).ToString(", "));


            Debug.WriteLine(nodes.Where(n => (bool)(n.GetDescendant(SqlHierarchyId.Null, SqlHierarchyId.Null) != SqlHierarchyId.Null)).ToString(", "));
            Debug.WriteLine(nodes.Where(n => (bool)(n.GetReparentedValue(n.GetAncestor(0), SqlHierarchyId.GetRoot()) != SqlHierarchyId.Null)).ToString(", "));
        }

        [TestMethod]
        public void MathFunctions()
        {
            Dump((AlbumEntity a) => Math.Sign(a.Year));
            Dump((AlbumEntity a) => -Math.Sign(a.Year) * a.Year);
            Dump((AlbumEntity a) => Math.Abs(a.Year));
            Dump((AlbumEntity a) => Math.Sin(a.Year));
            Dump((AlbumEntity a) => Math.Asin(Math.Sin(a.Year)));
            Dump((AlbumEntity a) => Math.Cos(a.Year));
            Dump((AlbumEntity a) => Math.Acos(Math.Cos(a.Year)));
            Dump((AlbumEntity a) => Math.Tan(a.Year));
            Dump((AlbumEntity a) => Math.Atan(Math.Tan(a.Year)));
            Dump((AlbumEntity a) => Math.Atan2(1, 1).InSql());
            Dump((AlbumEntity a) => Math.Pow(a.Year, 2).InSql());
            Dump((AlbumEntity a) => Math.Sqrt(a.Year));
            Dump((AlbumEntity a) => Math.Exp(Math.Log(a.Year)));
            Dump((AlbumEntity a) => Math.Floor(a.Year + 0.5).InSql());
            Dump((AlbumEntity a) => Math.Log10(a.Year));
            Dump((AlbumEntity a) => Math.Ceiling(a.Year + 0.5).InSql());
            Dump((AlbumEntity a) => Math.Round(a.Year + 0.5).InSql());
            Dump((AlbumEntity a) => Math.Truncate(a.Year + 0.5).InSql());
        }

        public void Dump<T, S>(Expression<Func<T, S>> bla)
            where T : Entity
        {
            Debug.WriteLine(Database.Query<T>().Select(a => bla.Evaluate(a).InSql()).ToString(","));
        }

        [TestMethod]
        public void ConcatenateNull()
        {
            var list = Database.Query<ArtistEntity>().Select(a => (a.Name + null).InSql()).ToList();

            Assert.IsFalse(list.Any(string.IsNullOrEmpty));
        }

        [TestMethod]
        public void EnumToString()
        {
            var sexs = Database.Query<ArtistEntity>().Select(a => a.Sex.ToString()).ToList();
        }

        [TestMethod]
        public void NullableEnumToString()
        {   
            var sexs = Database.Query<ArtistEntity>().Select(a => a.Status.ToString()).ToList();
        }

        [TestMethod]
        public void ConcatenateStringNullableNominate()
        {
            var list2 = Database.Query<ArtistEntity>().Select(a => a.Name + " is " + a.Status).ToList();
        }

        [TestMethod]
        public void ConcatenateStringNullableEntity()
        {
            var list1 = Database.Query<AlbumEntity>().Select(a => a.Name + " is published by " + a.Label).ToList();
        }

        [TestMethod]
        public void ConcatenateStringFullNominate()
        {
            var list = Database.Query<ArtistEntity>().Where(a => (a + "").Contains("Michael")).ToList();

            Assert.IsTrue(list.Count == 1);
        }

        [TestMethod]
        public void Etc()
        {
            Assert.IsTrue(Enumerable.SequenceEqual(
                Database.Query<AlbumEntity>().Select(a => a.Name.Etc(10)).OrderBy().ToList(),
                Database.Query<AlbumEntity>().Select(a => a.Name).ToList().Select(l => l.Etc(10)).OrderBy().ToList()));

            Assert.AreEqual(
                Database.Query<AlbumEntity>().Count(a => a.Name.Etc(10).EndsWith("s")),
                Database.Query<AlbumEntity>().Count(a => a.Name.EndsWith("s")));
        }

        [TestMethod]
        public void TableValuedFunction()
        {
            var list = Database.Query<AlbumEntity>()
                .Where(a => MinimumExtensions.MinimumTableValued((int)a.Id * 2, (int)a.Id).Select(m => m.MinValue).First() > 2).Select(a => a.Id).ToList();
        }

        [TestMethod]
        public void TableValuedPerformanceTest()
        {
            var songs = Database.MListQuery((AlbumEntity a) => a.Songs).Select(a => a.Element);

            var t1 = PerfCounter.Ticks;

            var fast = (from s1 in songs
                        from s2 in songs
                        from s3 in songs
                        from s4 in songs
                        select MinimumExtensions.MinimumTableValued(
                        MinimumExtensions.MinimumTableValued(s1.Seconds, s2.Seconds).Select(a => a.MinValue).First(),
                        MinimumExtensions.MinimumTableValued(s3.Seconds, s4.Seconds).Select(a => a.MinValue).First()
                        ).Select(a => a.MinValue).First()).ToList();

            var t2 = PerfCounter.Ticks;

            var fast2 = (from s1 in songs
                         from s2 in songs
                         from s3 in songs
                         from s4 in songs
                         let x = MinimumExtensions.MinimumTableValued(s1.Seconds, s2.Seconds).Select(a => a.MinValue).First()
                         let y = MinimumExtensions.MinimumTableValued(s3.Seconds, s4.Seconds).Select(a => a.MinValue).First()
                         select MinimumExtensions.MinimumTableValued(x, y).Select(a => a.MinValue).First()).ToList();

            var t3 = PerfCounter.Ticks;

            var slow = (from s1 in songs
                        from s2 in songs
                        from s3 in songs
                        from s4 in songs
                        let x = MinimumExtensions.MinimumScalar(s1.Seconds, s2.Seconds)
                        let y = MinimumExtensions.MinimumScalar(s3.Seconds, s4.Seconds)
                        select MinimumExtensions.MinimumScalar(x, y)).ToList();

            var t4 = PerfCounter.Ticks;

            Assert.IsTrue(PerfCounter.ToMilliseconds(t1, t2) < PerfCounter.ToMilliseconds(t3, t4));
            Assert.IsTrue(PerfCounter.ToMilliseconds(t2, t3) < PerfCounter.ToMilliseconds(t3, t4));
        }

        [TestMethod]
        public void SimplifyMinimumTableValued()
        {
            var result = (from b in Database.Query<BandEntity>()
                          let min = MinimumExtensions.MinimumTableValued((int)b.Id, (int)b.Id).FirstOrDefault().MinValue
                          select b.Name).ToList();
        }

        [TestMethod]
        public void NominateEnumSwitch()
        {
            var list = Database.Query<AlbumEntity>().Select(a =>
                (a.Songs.Count > 10 ? AlbumSize.Large :
                a.Songs.Count > 5 ? AlbumSize.Medium :
                 AlbumSize.Small).InSql()).ToList();
        }

        public enum AlbumSize
        {
            Small,
            Medium,
            Large
        }
    }
}
