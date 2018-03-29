using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signum.Engine;
using Signum.Entities;
using System.Diagnostics;
using System.IO;
using Signum.Utilities;
using Signum.Test.Environment;
using System.Linq.Expressions;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Test.LinqProvider
{
    /// <summary>
    /// Summary description for LinqProvider
    /// </summary>
    [TestClass]
    public class WhereTest
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
        public void Where()
        {
            var list = Database.Query<AlbumEntity>().Where(a => a.Year < 1995).ToList();
        }

        [TestMethod]
        public void WhereIndex()
        {
            var list = Database.Query<AlbumEntity>().Where((a, i) => i % 2 == 0).ToList();
        }

        [TestMethod]
        public void WhereExplicitConvert()
        {
            var list = Database.Query<AlbumEntity>().Where(a => a.Id.ToString() == "1").ToList();
        }

        [TestMethod]
        public void WhereImplicitConvert()
        {
            Database.Query<AlbumEntity>().Where(a => ("C" + a.Id) == "C1").Any();
        }


        [TestMethod]
        public void WhereSelect()
        {
            var list = Database.Query<AlbumEntity>().Where(a => a.Year < 1995).Select(a => new { a.Year, Author = a.Author.ToLite(), a.Name }).ToList();
        }

        [TestMethod]
        public void WhereBool()
        {
            var list = Database.Query<ArtistEntity>().Where(a => a.Dead).ToList();
        }

        [TestMethod]
        public void WhereNotNull()
        {
            var list = Database.Query<ArtistEntity>().Where(a => a.LastAward != null).ToList();
        }

        [TestMethod]
        public void SingleFirstLast()
        {
            var artists = Database.Query<ArtistEntity>();

            Assert2.Throws<InvalidOperationException>(() => artists.SingleEx(a => a.Dead && !a.Dead));
            Assert.IsNotNull(artists.SingleEx(a => a.Dead)); //michael
            Assert2.Throws<InvalidOperationException>(() => artists.SingleEx(a => a.Sex == Sex.Male));

            Assert.IsNull(artists.SingleOrDefaultEx(a => a.Dead && !a.Dead));
            Assert.IsNotNull(artists.SingleOrDefaultEx(a => a.Dead)); //michael
            Assert2.Throws<InvalidOperationException>(() => artists.SingleOrDefaultEx(a => a.Sex == Sex.Male));

            Assert2.Throws<InvalidOperationException>(() => artists.FirstEx(a => a.Dead && !a.Dead));
            Assert.IsNotNull(artists.FirstEx(a => a.Dead)); //michael
            Assert.IsNotNull(artists.FirstEx(a => a.Sex == Sex.Male));

            Assert.IsNull(artists.FirstOrDefault(a => a.Dead && !a.Dead));
            Assert.IsNotNull(artists.FirstOrDefault(a => a.Dead)); //michael
            Assert.IsNotNull(artists.FirstOrDefault(a => a.Sex == Sex.Male));

            Assert2.Throws<InvalidOperationException>(() => artists.Where(a => a.Dead && !a.Dead).SingleOrMany());
            Assert.IsNotNull(artists.Where(a => a.Dead).SingleOrMany()); //michael
            Assert.IsNull(artists.Where(a => a.Sex == Sex.Male).SingleOrMany());
        }

        [TestMethod]
        public void SingleFirstLastError()
        {
            var artists = Database.Query<ArtistEntity>();

            Assert2.Throws<InvalidOperationException>("Y", () => artists.Where(a => a.Dead && !a.Dead).SingleEx(() => "Y"));
            Assert2.Throws<InvalidOperationException>("Y", () => artists.Where(a => a.Sex == Sex.Male).SingleEx(() => "X", () => "Y"));
            Assert2.Throws<InvalidOperationException>("Y", () => artists.Where(a => a.Sex == Sex.Male).SingleOrDefaultEx(() => "Y"));
            Assert2.Throws<InvalidOperationException>("X", () => artists.Where(a => a.Dead && !a.Dead).FirstEx(() => "X"));

            Assert2.Throws<InvalidOperationException>(typeof(ArtistEntity).Name, () => artists.SingleEx(a => a.Dead && !a.Dead));
            Assert2.Throws<InvalidOperationException>(typeof(ArtistEntity).Name, () => artists.SingleEx(a => a.Sex == Sex.Male));
            Assert2.Throws<InvalidOperationException>(typeof(ArtistEntity).Name, () => artists.SingleOrDefaultEx(a => a.Sex == Sex.Male));
            Assert2.Throws<InvalidOperationException>(typeof(ArtistEntity).Name, () => artists.FirstEx(a => a.Dead && !a.Dead));


            Assert2.Throws<InvalidOperationException>("X", () => artists.Where(a => a.Dead && !a.Dead).SingleOrManyEx(() => "X"));
        }


        [TestMethod]
        public void WhereEntityEquals()
        {
            ArtistEntity wretzky = Database.Query<ArtistEntity>().SingleEx(a => a.Sex == Sex.Female);

            BandEntity smashing = (from b in Database.Query<BandEntity>()
                                   from a in b.Members
                                   where a == wretzky
                                   select b).SingleEx();
        }


        [TestMethod]
        public void WhereLiteEquals()
        {
            ArtistEntity wretzky = Database.Query<ArtistEntity>().SingleEx(a => a.Sex == Sex.Female);

            BandEntity smashing = (from b in Database.Query<BandEntity>()
                                   from a in b.Members
                                   where a.ToLite() == wretzky.ToLite()
                                   select b).SingleEx();
        }


        [TestMethod]
        public void WhereEntityEqualsIB()
        {
            ArtistEntity michael = Database.Query<ArtistEntity>().SingleEx(a => a.Dead);

            var albums = (from a in Database.Query<AlbumEntity>()
                          where a.Author == michael
                          select a.ToLite()).ToList();
        }

        [TestMethod]
        public void WhereEntityEqualsIBA()
        {
            ArtistEntity michael = Database.Query<ArtistEntity>().SingleEx(a => a.Dead);

            var albums = (from n in Database.Query<NoteWithDateEntity>()
                          where n.Target == michael
                          select n.ToLite()).ToList();
        }

        [TestMethod]
        public void WhereLiteEqualsIB()
        {
            ArtistEntity michael = Database.Query<ArtistEntity>().SingleEx(a => a.Dead);

            var albums = (from a in Database.Query<AlbumEntity>()
                          where a.Author.ToLite() == michael.ToLite()
                          select a.ToLite()).ToList();
        }

        [TestMethod]
        public void WhereLiteEqualsIBA()
        {
            ArtistEntity michael = Database.Query<ArtistEntity>().SingleEx(a => a.Dead);

            var albums = (from n in Database.Query<NoteWithDateEntity>()
                          where n.Target.ToLite() == michael.ToLite()
                          select n.ToLite()).ToList();
        }

        [TestMethod]
        public void WhereIs()
        {
            var albums = (from a in Database.Query<AlbumEntity>()
                          where a.Author is ArtistEntity
                          select a.ToLite()).ToList();
        }


        [TestMethod]
        public void WhereRefersTo1()
        {
            var lite = (Lite<BandEntity>)null;

            var first = Database.Query<BandEntity>().Where(b => lite.RefersTo(b)).FirstOrDefault();

            Assert.AreEqual(null, first);
        }

        [TestMethod]
        public void WhereRefersTo2()
        {
            var entity = (BandEntity)null;

            var first = Database.Query<BandEntity>().Where(b => b.ToLite().RefersTo(entity)).FirstOrDefault();

            Assert.AreEqual(null, first);
        }

        [TestMethod]
        public void WhereCase()
        {
            var list = (from a in Database.Query<ArtistEntity>()
                        where a.Dead ? a.Name.Contains("Michael") : a.Name.Contains("Billy")
                        select a).ToArray();
        }

        [TestMethod]
        public void WherePolyExpressionMethodUnion()
        {
            var list = Database.Query<AlbumEntity>().Where(a => a.Author.CombineUnion().Lonely()).ToArray();
        }

        [TestMethod]
        public void WherePolyExpressionMethodSwitch()
        {
            var list = Database.Query<AlbumEntity>().Where(a => a.Author.CombineCase().Lonely()).ToArray();
        }


        [TestMethod]
        public void WhereOptimize()
        {
            var list = Database.Query<ArtistEntity>().Where(a => a.Dead && true).Select(a => a.Name).ToList();
            list = Database.Query<ArtistEntity>().Where(a => a.Dead && false).Select(a => a.Name).ToList();
            list = Database.Query<ArtistEntity>().Where(a => a.Dead || true).Select(a => a.Name).ToList();
            list = Database.Query<ArtistEntity>().Where(a => a.Dead || false).Select(a => a.Name).ToList();
            list = Database.Query<ArtistEntity>().Where(a => a.Dead == true).Select(a => a.Name).ToList();
            list = Database.Query<ArtistEntity>().Where(a => a.Dead == false).Select(a => a.Name).ToList();
            list = Database.Query<ArtistEntity>().Where(a => a.Dead != true).Select(a => a.Name).ToList();
            list = Database.Query<ArtistEntity>().Where(a => a.Dead != false).Select(a => a.Name).ToList();
            list = Database.Query<ArtistEntity>().Where(a => true ? a.Dead : false).Select(a => a.Name).ToList();
            list = Database.Query<ArtistEntity>().Where(a => false ? false : a.Dead).Select(a => a.Name).ToList();

            list = Database.Query<ArtistEntity>().Where(a => true).Select(a => a.Name).ToList();
            list = Database.Query<ArtistEntity>().Where(a => !false).Select(a => a.Name).ToList();
            list = Database.Query<ArtistEntity>().Where(a => true ? true : false).Select(a => a.Name).ToList();
            list = Database.Query<ArtistEntity>().Where(a => false ? false : true).Select(a => a.Name).ToList();
        }

        [TestMethod]
        public void WhereInnerQueryable()
        {
            var females = Database.Query<ArtistEntity>().Where(a => a.Sex == Sex.Female);
            string f = females.ToString();

            var female = Database.Query<ArtistEntity>().SingleEx(a => females.Contains(a));
        }

        [TestMethod]
        public void WhereEnumToString()
        {
            var females = Database.Query<ArtistEntity>().Count(a => a.Sex.ToString() == Sex.Female.ToString());
            var females2 = Database.Query<ArtistEntity>().Count(a => a.Sex == Sex.Female);
            Assert.AreEqual(females, females2);

            var bla = Database.Query<ArtistEntity>().Count(a => a.Sex.ToString() == a.Name);
            Assert.AreEqual(bla, 0);
        }

        [TestMethod]
        public void WhereNullableEnumToString()
        {
            var females = Database.Query<ArtistEntity>().Count(a => a.Status.ToString() == Status.Married.ToString());
            var females2 = Database.Query<ArtistEntity>().Count(a => a.Status == Status.Married);
            Assert.AreEqual(females, females2);
        }


        [TestMethod]
        public void WhereEmbeddedNull()
        {
            var albumsWithBonusTrack = Database.Query<AlbumEntity>().Where(a => a.BonusTrack == null).ToList();
        }

        [TestMethod]
        public void WhereEmbeddedNotNull()
        {
            var albumsWithBonusTrack = Database.Query<AlbumEntity>().Where(a => a.BonusTrack != null).ToList();
        }

        [TestMethod]
        public void WhereMixinNullThrows()
        {
            Assert2.Throws<InvalidOperationException>(() =>
               Database.Query<NoteWithDateEntity>().Where(n => n.Mixin<CorruptMixin>() == null).ToList());
        }

        [TestMethod]
        public void WhereMixinField()
        {
            var list = Database.Query<NoteWithDateEntity>().Where(n => n.Mixin<CorruptMixin>().Corrupt == false).ToList();
        }

        [TestMethod]
        public void WhereMixinMainEntityField()
        {
            var list = Database.Query<NoteWithDateEntity>().Where(n => n.Mixin<CorruptMixin>().MainEntity == n).ToList();
        }

        [TestMethod]
        public void WhereBindTuple()
        {
            var albums = Database.Query<AlbumEntity>().Select(a => Tuple.Create(a.Name, a.Label)).Where(t => t.Item2 == null).ToList();
        }

        [TestMethod]
        public void WhereBindBigTuple()
        {
            var albums = Database.Query<AlbumEntity>().Select(a => Tuple.Create(a.Name, a.Name, a.Name, a.Name, a.Name, a.Name, a.Name, a.Label)).Where(t => t.Rest.Item1 == null).ToList();
        }

        [TestMethod]
        public void WhereOutsideIs()
        {
            var albums = Database.Query<BandEntity>().Where(a => a.LastAward is PersonalAwardEntity).ToList();
        }

        [TestMethod]
        public void WhereOutsideCast()
        {
            var albums = Database.Query<BandEntity>().Where(a => ((PersonalAwardEntity)a.LastAward) != null).ToList();
        }

        [TestMethod]
        public void WhereOutsideEquals()
        {
            var pa = Database.Query<PersonalAwardEntity>().FirstEx();

            var albums = Database.Query<BandEntity>().Where(a => a.LastAward == pa).ToList();
        }

        [TestMethod]
        public void WhereMListContains()
        {
            var female = Database.Query<ArtistEntity>().Single(a => a.Sex == Sex.Female);

            var albums = Database.Query<BandEntity>().Where(a => a.Members.Contains(female)).Select(a => a.ToLite()).ToList();
        }

        [TestMethod]
        public void WhereMListLiteContains()
        {
            var female = Database.Query<ArtistEntity>().Select(a => a.ToLite()).Single(a => a.Entity.Sex == Sex.Female);

            var albums = Database.Query<ArtistEntity>().Where(a => a.Friends.Contains(female)).Select(a => a.ToLite()).ToList();
        }

        [TestMethod]
        public void WhereMListContainsSingle()
        {
            var albums = Database.Query<BandEntity>().Where(a => a.Members.Contains(
                Database.Query<ArtistEntity>().Single(a2 => a2.Sex == Sex.Female)
                )).Select(a => a.ToLite()).ToList();
        }

        [TestMethod]
        public void WhereMListLiteContainsSingle()
        {
            var albums = Database.Query<ArtistEntity>().Where(a =>
                a.Friends.Contains(Database.Query<ArtistEntity>().Single(a2 => a2.Sex == Sex.Female).ToLite())
                ).Select(a => a.ToLite()).ToList();
        }

        [TestMethod]
        public void NullableBoolFix()
        {
            var artist = Database.Query<ArtistEntity>().Where(a => ((bool?)(a.Dead ? a.Friends.Any() : false)) == true).ToList();
        }

        [TestMethod]
        public void ExceptionTest()
        {
            Assert2.Throws<FieldReaderException>(() =>
                Database.Query<ArtistEntity>().Select(a => Throw(a.Id)).ToList());
        }

        public static bool Throw(PrimaryKey a)
        {
            throw new ArgumentException("a");
        }

        [TestMethod]
        public void DistinctWithNulls()
        {
            var id = Database.Query<AlbumEntity>().Select(a => a.Id).FirstEx();

            var nullRight = Database.Query<AlbumEntity>().Where(alb => LinqHints.DistinctNull(alb.Id, (PrimaryKey?)null)).Count();
            var notNullRight = Database.Query<AlbumEntity>().Where(alb => LinqHints.DistinctNull(alb.Id, (PrimaryKey?)id)).Count();

            var nullLeft = Database.Query<AlbumEntity>().Where(alb => LinqHints.DistinctNull((PrimaryKey?)null, alb.Id)).Count();
            var notNullLeft = Database.Query<AlbumEntity>().Where(alb => LinqHints.DistinctNull((PrimaryKey?)id, alb.Id)).Count();
        }

        [TestMethod]
        public void WhereEqualsNew()
        {
            GrammyAwardEntity award = new GrammyAwardEntity();

            var count = Database.Query<BandEntity>().Count(a => a.LastAward == award);

            Assert.AreEqual(0, count);
        }


        [TestMethod]
        public void WhereNotEqualsNew()
        {
            GrammyAwardEntity award = new GrammyAwardEntity();

            var count = Database.Query<BandEntity>().Count(a => a.LastAward != award);

            Assert.IsTrue(count > 0);
        }

        [TestMethod]
        public void WhereEqualsNewIBA()
        {
            GrammyAwardEntity award = new GrammyAwardEntity();

            var count = Database.Query<ArtistEntity>().Count(a => a.LastAward == award);

            Assert.AreEqual(0, count);
        }

        [TestMethod]
        public void WhereCount()
        {
            var album = Database.Query<ArtistEntity>()
                .Where(a => Database.Query<AlbumEntity>().Where(al => al.Author.Is(a)).Count() > 0)
                .Select(a => a.Name)
                .ToList();
        }


        [TestMethod]
        public void WhereFormat()
        {
            var album = Database.Query<ArtistEntity>()
                .Where(a => $"Hi {(a.IsMale ? "Mr." : "Ms.")} {a}".Contains("Mr. Michael"))
                .Select(a => a.ToLite())
                .ToList();
        }


        [TestMethod]
        public void WhereFormat4()
        {
            var album = Database.Query<ArtistEntity>()
                .Where(a => $"Hi {a.Name} {a.Name} {a.Name} {a.Name}".Contains("Mr. Michael"))
                .Select(a => a.ToLite())
                .ToList();
        }


        [TestMethod]
        public void WhereNoFormat()
        {
            var album = Database.Query<ArtistEntity>()
                .Where(a => ("Hi " + (a.IsMale ? "Mr." : "Ms.") + " " + a.Name + " ToStr " + a).Contains("Mr. Michael"))
                .Select(a => a.ToLite())
                .ToList();
        }
    }
}
