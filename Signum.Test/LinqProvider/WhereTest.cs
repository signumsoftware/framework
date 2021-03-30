using System;
using System.Linq;
using Xunit;
using Signum.Engine;
using Signum.Entities;
using Signum.Utilities;
using Signum.Test.Environment;
using Signum.Utilities.ExpressionTrees;
using System.Text.RegularExpressions;

namespace Signum.Test.LinqProvider
{
    /// <summary>
    /// Summary description for LinqProvider
    /// </summary>
    public class WhereTest
    {
        public WhereTest()
        {
            MusicStarter.StartAndLoad();
            Connector.CurrentLogger = new DebugTextWriter();
        }

        [Fact]
        public void Where()
        {
            var list = Database.Query<AlbumEntity>().Where(a => a.Year < 1995).ToList();
        }

        [Fact]
        public void WhereWithExpressionOr()
        {


            var list = (from a in Database.Query<ArtistEntity>()
                        where a.Sex.IsDefined()
                        select a).ToList();
        }

        [Fact]
        public void WhereIndex()
        {
            var list = Database.Query<AlbumEntity>().Where((a, i) => i % 2 == 0).ToList();
        }

        [Fact]
        public void WhereExplicitConvert()
        {
            var list = Database.Query<AlbumEntity>().Where(a => a.Id.ToString() == "1").ToList();
        }

        [Fact]
        public void WhereImplicitConvert()
        {
            Database.Query<AlbumEntity>().Where(a => ("C" + a.Id) == "C1").Any();
        }

        [Fact]
        public void WhereCombineConvert()
        {
            var query = Database.Query<AlbumEntity>().Where(a => ("C" + a.Id) + "B" == "C1B").Select(a => a.Id);

            Assert.Single(new Regex("CONCAT").Matches(query.QueryText()));

            query.ToList();
        }


        [Fact]
        public void WhereSelect()
        {
            var list = Database.Query<AlbumEntity>().Where(a => a.Year < 1995).Select(a => new { a.Year, Author = a.Author.ToLite(), a.Name }).ToList();
        }

        [Fact]
        public void WhereBool()
        {
            var list = Database.Query<ArtistEntity>().Where(a => a.Dead).ToList();
        }

        [Fact]
        public void WhereNotNull()
        {
            var list = Database.Query<ArtistEntity>().Where(a => a.LastAward != null).ToList();
        }

        [Fact]
        public void SingleFirstLast()
        {
            var artists = Database.Query<ArtistEntity>();

            Assert.Throws<InvalidOperationException>(() => artists.SingleEx(a => a.Dead && !a.Dead));
            Assert.NotNull(artists.SingleEx(a => a.Dead)); //michael
            Assert.Throws<InvalidOperationException>(() => artists.SingleEx(a => a.Sex == Sex.Male));

            Assert.Null(artists.SingleOrDefaultEx(a => a.Dead && !a.Dead));
            Assert.NotNull(artists.SingleOrDefaultEx(a => a.Dead)); //michael
            Assert.Throws<InvalidOperationException>(() => artists.SingleOrDefaultEx(a => a.Sex == Sex.Male));

            Assert.Throws<InvalidOperationException>(() => artists.FirstEx(a => a.Dead && !a.Dead));
            Assert.NotNull(artists.FirstEx(a => a.Dead)); //michael
            Assert.NotNull(artists.FirstEx(a => a.Sex == Sex.Male));

            Assert.Null(artists.FirstOrDefault(a => a.Dead && !a.Dead));
            Assert.NotNull(artists.FirstOrDefault(a => a.Dead)); //michael
            Assert.NotNull(artists.FirstOrDefault(a => a.Sex == Sex.Male));
        }

        [Fact]
        public void SingleFirstLastError()
        {
            var artists = Database.Query<ArtistEntity>();

            AssertThrows<InvalidOperationException>("Y", () => artists.Where(a => a.Dead && !a.Dead).SingleEx(() => "Y"));
            AssertThrows<InvalidOperationException>("Y", () => artists.Where(a => a.Sex == Sex.Male).SingleEx(() => "X", () => "Y"));
            AssertThrows<InvalidOperationException>("Y", () => artists.Where(a => a.Sex == Sex.Male).SingleOrDefaultEx(() => "Y"));
            AssertThrows<InvalidOperationException>("X", () => artists.Where(a => a.Dead && !a.Dead).FirstEx(() => "X"));

            AssertThrows<InvalidOperationException>(typeof(ArtistEntity).Name, () => artists.SingleEx(a => a.Dead && !a.Dead));
            AssertThrows<InvalidOperationException>(typeof(ArtistEntity).Name, () => artists.SingleEx(a => a.Sex == Sex.Male));
            AssertThrows<InvalidOperationException>(typeof(ArtistEntity).Name, () => artists.SingleOrDefaultEx(a => a.Sex == Sex.Male));
            AssertThrows<InvalidOperationException>(typeof(ArtistEntity).Name, () => artists.FirstEx(a => a.Dead && !a.Dead));


            AssertThrows<InvalidOperationException>("X", () => artists.Where(a => a.Dead && !a.Dead).SingleOrManyEx(() => "X"));
        }

        static void AssertThrows<T>(string message, Action action) where T : Exception
        {
            var e = Assert.Throws<T>(action);
            Assert.Contains(message, e.Message);
        }


        [Fact]
        public void WhereEntityEquals()
        {
            ArtistEntity wretzky = Database.Query<ArtistEntity>().SingleEx(a => a.Sex == Sex.Female);

            BandEntity smashing = (from b in Database.Query<BandEntity>()
                                   from a in b.Members
                                   where a == wretzky
                                   select b).SingleEx();
        }


        [Fact]
        public void WhereLiteEquals()
        {
            ArtistEntity wretzky = Database.Query<ArtistEntity>().SingleEx(a => a.Sex == Sex.Female);

            BandEntity smashing = (from b in Database.Query<BandEntity>()
                                   from a in b.Members
                                   where a.ToLite() == wretzky.ToLite()
                                   select b).SingleEx();
        }


        [Fact]
        public void WhereEntityEqualsIB()
        {
            ArtistEntity michael = Database.Query<ArtistEntity>().SingleEx(a => a.Dead);

            var albums = (from a in Database.Query<AlbumEntity>()
                          where a.Author == michael
                          select a.ToLite()).ToList();
        }

        [Fact]
        public void WhereEntityEqualsIBA()
        {
            ArtistEntity michael = Database.Query<ArtistEntity>().SingleEx(a => a.Dead);

            var albums = (from n in Database.Query<NoteWithDateEntity>()
                          where n.Target == michael
                          select n.ToLite()).ToList();
        }

        [Fact]
        public void WhereLiteEqualsIB()
        {
            ArtistEntity michael = Database.Query<ArtistEntity>().SingleEx(a => a.Dead);

            var albums = (from a in Database.Query<AlbumEntity>()
                          where a.Author.ToLite() == michael.ToLite()
                          select a.ToLite()).ToList();
        }

        [Fact]
        public void WhereLiteEqualsIBA()
        {
            ArtistEntity michael = Database.Query<ArtistEntity>().SingleEx(a => a.Dead);

            var albums = (from n in Database.Query<NoteWithDateEntity>()
                          where n.Target.ToLite() == michael.ToLite()
                          select n.ToLite()).ToList();
        }

        [Fact]
        public void WhereIs()
        {
            var albums = (from a in Database.Query<AlbumEntity>()
                          where a.Author is ArtistEntity
                          select a.ToLite()).ToList();
        }


        [Fact]
        public void WhereRefersTo1()
        {
            var lite = (Lite<BandEntity>?)null;

            var first = Database.Query<BandEntity>().Where(b => lite.Is(b)).FirstOrDefault();

            Assert.Null(first);
        }

        [Fact]
        public void WhereRefersTo2()
        {
            var entity = (BandEntity?)null;

            var first = Database.Query<BandEntity>().Where(b => b.ToLite().Is(entity)).FirstOrDefault();

            Assert.Null(first);
        }

        [Fact]
        public void WhereCase()
        {
            var list = (from a in Database.Query<ArtistEntity>()
                        where a.Dead ? a.Name.Contains("Michael") : a.Name.Contains("Billy")
                        select a).ToArray();
        }

        [Fact]
        public void WherePolyExpressionMethodUnion()
        {
            var list = Database.Query<AlbumEntity>().Where(a => a.Author.CombineUnion().Lonely()).ToArray();
        }

        [Fact]
        public void WherePolyExpressionMethodSwitch()
        {
            var list = Database.Query<AlbumEntity>().Where(a => a.Author.CombineCase().Lonely()).ToArray();
        }


        [Fact]
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

        [Fact]
        public void WhereInnerQueryable()
        {
            var females = Database.Query<ArtistEntity>().Where(a => a.Sex == Sex.Female);
            string f = females.ToString()!;

            var female = Database.Query<ArtistEntity>().SingleEx(a => females.Contains(a));
        }

        [Fact]
        public void WhereEnumToString()
        {
            var females = Database.Query<ArtistEntity>().Count(a => a.Sex.ToString() == Sex.Female.ToString());
            var females2 = Database.Query<ArtistEntity>().Count(a => a.Sex == Sex.Female);
            Assert.Equal(females, females2);

            var bla = Database.Query<ArtistEntity>().Count(a => a.Sex.ToString() == a.Name);
            Assert.Equal(0, bla);
        }

        [Fact]
        public void WhereNullableEnumToString()
        {
            var females = Database.Query<ArtistEntity>().Count(a => a.Status.ToString() == Status.Married.ToString());
            var females2 = Database.Query<ArtistEntity>().Count(a => a.Status == Status.Married);
            Assert.Equal(females, females2);
        }


        [Fact]
        public void WhereEmbeddedNull()
        {
            var albumsWithBonusTrack = Database.Query<AlbumEntity>().Where(a => a.BonusTrack == null).ToList();
        }

        [Fact]
        public void WhereEmbeddedNotNull()
        {
            var albumsWithBonusTrack = Database.Query<AlbumEntity>().Where(a => a.BonusTrack != null).ToList();
        }

        [Fact]
        public void WhereMixinNullThrows()
        {
            Assert.Throws<InvalidOperationException>(() =>
               Database.Query<NoteWithDateEntity>().Where(n => n.Mixin<CorruptMixin>() == null).ToList());
        }

        [Fact]
        public void WhereMixinField()
        {
            var list = Database.Query<NoteWithDateEntity>().Where(n => n.Mixin<CorruptMixin>().Corrupt == false).ToList();
        }

        [Fact]
        public void WhereMixinMainEntityField()
        {
            var list = Database.Query<NoteWithDateEntity>().Where(n => n.Mixin<CorruptMixin>().MainEntity == n).ToList();
        }

        [Fact]
        public void WhereBindTuple()
        {
            var albums = Database.Query<AlbumEntity>().Select(a => Tuple.Create(a.Name, a.Label)).Where(t => t.Item2 == null).ToList();
        }

        [Fact]
        public void WhereBindBigTuple()
        {
            var albums = Database.Query<AlbumEntity>().Select(a => Tuple.Create(a.Name, a.Name, a.Name, a.Name, a.Name, a.Name, a.Name, a.Label)).Where(t => t.Rest.Item1 == null).ToList();
        }

        [Fact]
        public void WhereOutsideIs()
        {
            var albums = Database.Query<BandEntity>().Where(a => a.LastAward is PersonalAwardEntity).ToList();
        }

        [Fact]
        public void WhereOutsideCast()
        {
            var albums = Database.Query<BandEntity>().Where(a => ((PersonalAwardEntity?)a.LastAward) != null).ToList();
        }

        [Fact]
        public void WhereOutsideEquals()
        {
            var pa = Database.Query<PersonalAwardEntity>().FirstEx();

            var albums = Database.Query<BandEntity>().Where(a => a.LastAward == pa).ToList();
        }

        [Fact]
        public void WhereMListContains()
        {
            var female = Database.Query<ArtistEntity>().Single(a => a.Sex == Sex.Female);

            var albums = Database.Query<BandEntity>().Where(a => a.Members.Contains(female)).Select(a => a.ToLite()).ToList();
        }

        [Fact]
        public void WhereMListLiteContains()
        {
            var female = Database.Query<ArtistEntity>().Select(a => a.ToLite()).Single(a => a.Entity.Sex == Sex.Female);

            var albums = Database.Query<ArtistEntity>().Where(a => a.Friends.Contains(female)).Select(a => a.ToLite()).ToList();
        }

        [Fact]
        public void WhereMListContainsSingle()
        {
            var albums = Database.Query<BandEntity>().Where(a => a.Members.Contains(
                Database.Query<ArtistEntity>().Single(a2 => a2.Sex == Sex.Female)
                )).Select(a => a.ToLite()).ToList();
        }

        [Fact]
        public void WhereMListLiteContainsSingle()
        {
            var albums = Database.Query<ArtistEntity>().Where(a =>
                a.Friends.Contains(Database.Query<ArtistEntity>().Single(a2 => a2.Sex == Sex.Female).ToLite())
                ).Select(a => a.ToLite()).ToList();
        }

        [Fact]
        public void NullableBoolFix()
        {
            var artist = Database.Query<ArtistEntity>().Where(a => ((bool?)(a.Dead ? a.Friends.Any() : false)) == true).ToList();
        }

        [Fact]
        public void ExceptionTest()
        {
            Assert.Throws<FieldReaderException>(() =>
                Database.Query<ArtistEntity>().Select(a => Throw(a.Id)).ToList());
        }

        public static bool Throw(PrimaryKey a)
        {
            throw new ArgumentException("a");
        }

        [Fact]
        public void DistinctWithNulls()
        {
            var id = Database.Query<AlbumEntity>().Select(a => a.Id).FirstEx();

            var nullRight = Database.Query<AlbumEntity>().Where(alb => LinqHints.DistinctNull(alb.Id, (PrimaryKey?)null)).Count();
            var notNullRight = Database.Query<AlbumEntity>().Where(alb => LinqHints.DistinctNull(alb.Id, (PrimaryKey?)id)).Count();

            var nullLeft = Database.Query<AlbumEntity>().Where(alb => LinqHints.DistinctNull((PrimaryKey?)null, alb.Id)).Count();
            var notNullLeft = Database.Query<AlbumEntity>().Where(alb => LinqHints.DistinctNull((PrimaryKey?)id, alb.Id)).Count();
        }

        [Fact]
        public void WhereEqualsNew()
        {
            GrammyAwardEntity award = new GrammyAwardEntity();

            var count = Database.Query<BandEntity>().Count(a => a.LastAward == award);

            Assert.Equal(0, count);
        }


        [Fact]
        public void WhereNotEqualsNew()
        {
            GrammyAwardEntity award = new GrammyAwardEntity();

            var count = Database.Query<BandEntity>().Count(a => a.LastAward != award);

            Assert.True(count > 0);
        }

        [Fact]
        public void WhereEqualsNewIBA()
        {
            GrammyAwardEntity award = new GrammyAwardEntity();

            var count = Database.Query<ArtistEntity>().Count(a => a.LastAward == award);

            Assert.Equal(0, count);
        }

        [Fact]
        public void WhereCount()
        {
            var album = Database.Query<ArtistEntity>()
                .Where(a => Database.Query<AlbumEntity>().Where(al => al.Author.Is(a)).Count() > 0)
                .Select(a => a.Name)
                .ToList();
        }


        [Fact]
        public void WhereFormat()
        {
            var album = Database.Query<ArtistEntity>()
                .Where(a => $"Hi {(a.IsMale ? "Mr." : "Ms.")} {a}".Contains("Mr. Michael"))
                .Select(a => a.ToLite())
                .ToList();
        }


        [Fact]
        public void WhereFormat4()
        {
            var album = Database.Query<ArtistEntity>()
                .Where(a => $"Hi {a.Name} {a.Name} {a.Name} {a.Name}".Contains("Mr. Michael"))
                .Select(a => a.ToLite())
                .ToList();
        }


        [Fact]
        public void WhereNoFormat()
        {
            var album = Database.Query<ArtistEntity>()
                .Where(a => ("Hi " + (a.IsMale ? "Mr." : "Ms.") + " " + a.Name + " ToStr " + a).Contains("Mr. Michael"))
                .Select(a => a.ToLite())
                .ToList();
        }
    }
}
