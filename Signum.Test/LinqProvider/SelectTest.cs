using Xunit;
using Signum.Engine;
using Signum.Entities;
using Signum.Test.Environment;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using System;
using System.Linq;
using System.Linq.Expressions;
using Signum.Utilities.DataStructures;
using Signum.Engine.Maps;

namespace Signum.Test.LinqProvider
{
    /// <summary>
    /// Summary description for LinqProvider
    /// </summary>
    public class SelectTest
    {
        public SelectTest()
        {
            MusicStarter.StartAndLoad();
            Connector.CurrentLogger = new DebugTextWriter();
        }

        [Fact]
        public void Select()
        {
            var list = Database.Query<AlbumEntity>().Select(a => a.Name).ToList();
        }

        [Fact]
        public void SelectIndex()
        {
            var list = Database.Query<AlbumEntity>().Select((a, i) => a.Name + i).ToList();
        }

        [Fact]
        public void SelectIds()
        {
            var first = Database.Query<BandEntity>().Select(b => b.Id).ToList();
        }

        [Fact]
        public void SelectFirstId()
        {
            var first = Database.Query<BandEntity>().Select(b => b.Id).First();
        }

        [Fact]
        public void SelectExpansion()
        {
            var list = Database.Query<AlbumEntity>().Select(a => a.Label.Name).ToList();
        }

        [Fact]
        public void SelectLetExpansion()
        {
            var list = (from a in Database.Query<AlbumEntity>()
                        let l = a.Label
                        select l.Name).ToList();
        }

        [Fact]
        public void SelectLetExpansionRedundant()
        {
            var list = (from a in Database.Query<AlbumEntity>()
                        let label = a.Label
                        select new
                        {
                            Artist = label.Country.Name,
                            Author = a.Label.Name
                        }).ToList();

            Assert.Equal(Database.Query<AlbumEntity>().Count(), list.Count);
        }

        [Fact]
        public void SelectWhereExpansion()
        {
            var list = Database.Query<AlbumEntity>().Where(a => a.Label != null).Select(a => a.Label.Name).ToList();
        }

        [Fact]
        public void SelectAnonymous()
        {
            var list = Database.Query<AlbumEntity>().Select(a => new { a.Name, a.Year }).ToList();
        }

        [Fact]
        public void SelectNoColumns()
        {
            var list = Database.Query<AlbumEntity>().Select(a => new { DateTime.Now, Album = (AlbumEntity?)null, Artist = (Lite<ArtistEntity>?)null }).ToList();
        }

        [Fact]
        public void SelectCount()
        {
            var list = Database.Query<AlbumEntity>().Select(a => (int?)a.Songs.Count).ToList();
        }

        [Fact]
        public void SelectLite()
        {
            var list = Database.Query<AlbumEntity>().Select(a => a.ToLite()).ToList();
        }

        [Fact]
        public void SelectLiteToStr()
        {
            var list = Database.Query<AlbumEntity>().Select(a => a.ToLite(a.Label.Name)).ToList();
        }



        [Fact]
        public void SelectBool()
        {
            var list = Database.Query<ArtistEntity>().Select(a => a.Dead).ToList();
        }

        [Fact]
        public void SelectConditionToBool()
        {
            var list = Database.Query<AlbumEntity>().Select(a => a.Year < 1990).ToList();
        }


        [Fact]
        public void SelectConditionalMember()
        {
            var list = (from l in Database.Query<LabelEntity>()
                        select (l.Owner == null ? l : l.Owner.Entity).Name).ToList();

        }

        [Fact]
        public void SelectConditionalToLite()
        {
            var list = (from l in Database.Query<LabelEntity>()
                        select (l.Owner == null ? l : l.Owner.Entity).ToLite()).ToList();
        }

#pragma warning disable IDE0029 // Use coalesce expression
        [Fact]
        public void SelectConditionalToLiteNull()
        {
            var list = (from l in Database.Query<LabelEntity>()
                        let owner = (l.Owner == null ? null : l.Owner)!.Entity
                        select owner.ToLite(owner.Name)).ToList();
        }
#pragma warning restore IDE0029 // Use coalesce expression

        [Fact]
        public void SelectConditionalGetType()
        {
            var list = (from l in Database.Query<LabelEntity>()
                        select (l.Owner == null ? l : l.Owner.Entity).GetType()).ToList();
        }

        [Fact]
        public void SelectCoalesceMember()
        {
            var list = (from l in Database.Query<LabelEntity>()
                        select (l.Owner!.Entity ?? l).Name).ToList();

        }

        [Fact]
        public void SelectCoalesceToLite()
        {
            var list = (from l in Database.Query<LabelEntity>()
                        select (l.Owner!.Entity ?? l).ToLite()).ToList();

        }

        [Fact]
        public void SelectCoalesceGetType()
        {
            var list = (from l in Database.Query<LabelEntity>()
                        select (l.Owner!.Entity ?? l).GetType()).ToList();

        }

        [Fact]
        public void SelectUpCast()
        {
            var list = (from n in Database.Query<ArtistEntity>()
                        select (IAuthorEntity)n).ToList(); //Just to full-nominate
        }

        [Fact]
        public void SelectEntityEquals()
        {
            ArtistEntity michael = Database.Query<ArtistEntity>().SingleEx(a => a.Dead);

            var list = Database.Query<AlbumEntity>().Select(a => a.Author == michael).ToList();
        }

        [Fact]
        public void SelectBoolExpression()
        {
            ArtistEntity michael = Database.Query<ArtistEntity>().SingleEx(a => a.Dead);

            var list = Database.Query<AlbumEntity>().Select(a => a.Author == michael).ToList();
        }

        [Fact]
        public void SelectExpressionProperty()
        {
            var list = Database.Query<ArtistEntity>().Where(a => a.IsMale).ToArray();
        }

        [Fact]
        public void SelectExpressionMethod()
        {
            var list = Database.Query<ArtistEntity>().Select(a => new { a.Name, Count = a.AlbumCount() }).ToArray();
        }

        [Fact]
        public void SelectPolyExpressionPropertyUnion()
        {
            var list = Database.Query<AlbumEntity>().Select(a => a.Author.CombineUnion().FullName).ToArray();
        }

        [Fact]
        public void SelectPolyExpressionPropertySwitch()
        {
            var list = Database.Query<AlbumEntity>().Select(a => a.Author.CombineCase().FullName).ToArray();
        }

        [Fact]
        public void SelectPolyExpressionMethodUnion()
        {
            var list = Database.Query<AlbumEntity>().Select(a => a.Author.CombineUnion().Lonely()).ToArray();
        }

        [Fact]
        public void SelectPolyExpressionMethodSwitch()
        {
            var list = Database.Query<AlbumEntity>().Select(a => a.Author.CombineCase().Lonely()).ToArray();
        }

        [Fact]
        public void SelectPolyExpressionMethodManual()
        {
            var list = Database.Query<AlbumEntity>().Select(a => a.Author is BandEntity ? ((BandEntity)a.Author).Lonely() : ((ArtistEntity)a.Author).Lonely()).ToArray();
        }

        [Fact]
        public void SelectThrowIntNullable()
        {
            Assert.Throws<FieldReaderException>(() =>
                Database.Query<AlbumEntity>().Select(a => ((ArtistEntity)a.Author).Id).ToArray());
        }

        [Fact]
        public void SelectThrowBoolNullable()
        {
            Assert.Throws<FieldReaderException>(() =>
                Database.Query<AlbumEntity>().Select(a => ((ArtistEntity)a.Author).Dead).ToArray());
        }

        [Fact]
        public void SelectThrowEnumNullable()
        {
            Assert.Throws<FieldReaderException>(() =>
                Database.Query<AlbumEntity>().Select(a => ((ArtistEntity)a.Author).Sex).ToArray());
        }

        [Fact]
        public void SelectIntNullable()
        {
            var list = Database.Query<AlbumEntity>().Select(a => (int?)((ArtistEntity)a.Author).Id).ToArray();
        }

        [Fact]
        public void SelectBoolNullable()
        {
            var list = Database.Query<AlbumEntity>().Select(a => (bool?)((ArtistEntity)a.Author).Dead).ToArray();
        }

        [Fact]
        public void SelectEnumNullable()
        {
            var list = Database.Query<ArtistEntity>().Select(a => a.Status).ToArray();
        }

        [Fact]
        public void SelectEnumNullableNullable()
        {
            var list = Database.Query<AlbumEntity>().Select(a => ((ArtistEntity)a.Author).Status).ToArray();
        }

        [Fact]
        public void SelectThrowsIntSumNullable()
        {
            Assert.Throws<FieldReaderException>(() =>
                Database.Query<AlbumEntity>().Select(a => (int)a.Id + (int)((ArtistEntity)a.Author).Id).ToArray());
        }

        [Fact]
        public void SelectThrowaIntSumNullableCasting()
        {
            Assert.Throws<FieldReaderException>(() =>
                Database.Query<AlbumEntity>().Select(a => (int?)((int)a.Id + (int)((ArtistEntity)a.Author).Id)).ToArray());
        }

        [Fact]
        public void SelectThrowaIntSumNullableCastingInSql()
        {
            var list = Database.Query<AlbumEntity>().Select(a => (int?)((int)a.Id + (int)((ArtistEntity)a.Author).Id).InSql()).ToArray();
        }


        [Fact]
        public void SelectEnumNullableBullableCast()
        {
            var list = Database.Query<AlbumEntity>().Select(a => (Sex?)((ArtistEntity)a.Author).Sex).ToArray();
        }

        [Fact]
        public void SelectEnumNullableValue()
        {
            var list = Database.Query<AlbumEntity>().Where(a => a.Author is ArtistEntity)
                .Select(a => ((Sex?)((ArtistEntity)a.Author).Sex).Value).ToArray();
        }

        [Fact]
        public void CoallesceNullable()
        {
            var list = Database.Query<ArtistEntity>()
                .Where(a => a.Status != null)
                .Select(a => (a.Status ?? a.Status)!.Value)
                .ToArray();
        }

        [Fact]
        public void SelectEmbeddedNullable()
        {
            var bonusTracks = Database.Query<AlbumEntity>().Select(a => a.BonusTrack).ToArray();
        }

        [Fact]
        public void SelectMixinThrows()
        {
            var e = Assert.Throws<InvalidOperationException>(() =>
                Database.Query<NoteWithDateEntity>().Select(a => a.Mixin<CorruptMixin>()).ToArray());

            Assert.Contains("without their main entity", e.Message);
        }


        [Fact]
        public void SelectMixinField()
        {
            Database.Query<NoteWithDateEntity>().Select(a => a.Mixin<CorruptMixin>().Corrupt).ToArray();
        }

        [Fact]
        public void SelectMixinWhere()
        {
            Database.Query<NoteWithDateEntity>().Where(a => a.Mixin<CorruptMixin>().Corrupt == true).ToArray();
        }

        [Fact]
        public void SelectMixinCollection()
        {
            var result = (from n in Database.Query<NoteWithDateEntity>()
                          from c in n.Mixin<ColaboratorsMixin>().Colaborators
                          select c).ToArray();
        }

        [Fact]
        public void SelectNullable()
        {
            var durations = (from a in Database.Query<AlbumEntity>()
                             from s in a.Songs
                             where s.Seconds.HasValue
                             select s.Seconds!.Value).ToArray();
        }

        [Fact]
        public void SelectIsNull()
        {
            var durations = (from a in Database.Query<AlbumEntity>()
                             from s in a.Songs
                             where s.Seconds.HasValue
                             select s.Seconds == null).ToArray();
        }

        [Fact]
        public void SelectAvoidNominate()
        {
            var durations =
                (from a in Database.Query<AlbumEntity>()
                 select new
                 {
                     a.Name,
                     Value = 3,
                 }).ToList();
        }

        [Fact]
        public void SelectAvoidNominateEntity()
        {
            var durations =
                (from a in Database.Query<AlbumEntity>()
                 select new
                 {
                     a.Name,
                     Friend = (Lite<BandEntity>?)null
                 }).ToList();
        }


        [Fact]
        public void SelectSingleCellAggregate()
        {
            var list = Database.Query<BandEntity>()
                .Select(b => new
                {
                    Count = b.Members.Count,
                    AnyDead = b.Members.Any(m => m.Dead),
                    DeadCount = b.Members.Count(m => m.Dead),
                    MinId = b.Members.Min(m => m.Id),
                    MaxId = b.Members.Max(m => m.Id),
                    AvgId = b.Members.Average(m => (int)m.Id),
                    SumId = b.Members.Sum(m => (int)m.Id),
                }).ToList();
        }

        [Fact]
        public void SelectMemoryEntity()
        {
            var artist = Database.Query<ArtistEntity>().FirstEx();

            var songs = Database.Query<AlbumEntity>().Select(a => new
            {
                Lite = a.ToLite(),
                Memory = artist,
            }).ToList();
        }

        [Fact]
        public void SelectMemoryLite()
        {
            var artist = Database.Query<ArtistEntity>().Select(a => a.ToLite()).FirstEx();

            var songs = Database.Query<AlbumEntity>().Select(a => new
            {
                Lite = a.ToLite(),
                MemoryLite = artist,
            }).ToList();
        }

        [Fact]
        public void SelectOutsideStringNull()
        {
            var awards = Database.Query<GrammyAwardEntity>().Select(a => ((AmericanMusicAwardEntity)(AwardEntity)a).Category).ToList();
        }

        [Fact]
        public void SelectOutsideLiteNull()
        {
            var awards = Database.Query<GrammyAwardEntity>().Select(a => ((AmericanMusicAwardEntity)(AwardEntity)a).ToLite()).ToList();
        }

        [Fact]
        public void SelectMListLite()
        {
            var lists = (from mle in Database.MListQuery((ArtistEntity a) => a.Friends)
                         select new { Artis = mle.Parent.Name, Friend = mle.Element.Entity.Name }).ToList();
        }

        [Fact]
        public void SelectMListEntity()
        {
            var lists = (from mle in Database.MListQuery((BandEntity a) => a.Members)
                         select new { Band = mle.Parent.Name, Artis = mle.Element.Name }).ToList();
        }

        [Fact]
        public void SelectMListEmbedded()
        {
            var lists = (from mle in Database.MListQuery((AlbumEntity a) => a.Songs)
                         select mle).ToList();
        }

        [Fact]
        public void SelectMListEmbeddedToList()
        {
            var lists = (from a in Database.Query<AlbumEntity>()
                         select new
                         {
                             a.Name,
                             Songs = a.Songs.ToList(),
                         }).ToList();
        }


        [Fact]
        public void SelectMListPotentialDuplicates()
        {
            var sp = (from alb in Database.Query<AlbumEntity>()
                      let mich = ((ArtistEntity)alb.Author)
                      where mich.Name.Contains("Michael")
                      select mich).ToList();

            var single = sp.Distinct(ReferenceEqualityComparer<ArtistEntity>.Default).SingleEx();

            Assert.Equal(single.Friends.Distinct().Count(), single.Friends.Count);
        }

        [Fact]
        public void SelectIBAId()
        {
            var list = Database.Query<ArtistEntity>().Select(a => a.LastAward.Try(la => la.Id)).ToList();
        }

        [Fact]
        public void SelectIBAIdObject()
        {
            var e = Assert.Throws<InvalidOperationException>(() =>
                Database.Query<ArtistEntity>().Select(a => a.LastAward.Try(la => (int?)la.Id).InSql()).ToList());

            Assert.Contains("translated", e.Message);
        }

        [Fact]
        public void SelectToStrField()
        {
            var list = Database.Query<NoteWithDateEntity>().Select(a => a.ToStringProperty).ToList();
        }

        [Fact]
        public void SelectFakedToString()
        {
            var list = Database.Query<AlbumEntity>().Select(a => a.ToStringProperty).ToList();
        }

        [Fact]
        public void SelectConditionFormat()
        {
            var list = Database.Query<AlbumEntity>().Select(a =>
                new
                {
                    Wrong = a.Author.GetType() == typeof(BandEntity) ?
                        "Band {0}".FormatWith(((BandEntity)a.Author).ToString()) :
                        "Artist {0}".FormatWith(((ArtistEntity)a.Author).ToString()),

                    Right = a.Author is BandEntity ?
                          "Band {0}".FormatWith(((BandEntity)a.Author).ToString()) :
                          "Artist {0}".FormatWith(((ArtistEntity)a.Author).ToString()),
                }).ToList();
        }

        [Fact]
        public void SelectToString()
        {
            var list = Database.Query<AlbumEntity>().Select(a => a.ToString()).ToList();
        }

        [Fact]
        public void SelectToStringLite()
        {
            var list = Database.Query<AlbumEntity>().Select(a => a.ToLite().ToString()).ToList();
        }

        [Fact]
        public void SelectConditionEnum()
        {
            var results = from b in Database.Query<BandEntity>()
                          let ga = (GrammyAwardEntity?)b.LastAward
                          select (AwardResult?)(ga.Result < ga.Result ? (int)ga.Result : (int)ga.Result).InSql();

            results.ToList();
        }

        [Fact]
        public void SelectMListId()
        {
            var list = Database.Query<ArtistEntity>().SelectMany(a => a.Friends).Select(a => a.Id).ToList();
        }

        [Fact]
        public void SelectMListIdCovariance()
        {
            var list = Database.Query<ArtistEntity>().SelectMany(a => a.FriendsCovariant()).Select(a => a.Id).ToList();
        }

        [Fact]
        public void SelectEmbeddedListNotNullableNull()
        {
            var list = (from a in Database.Query<AlbumEntity>()
                        from s in a.Songs.Where(s => s.Seconds < 0).DefaultIfEmpty()
                        select new { a, s }).ToList();

            Assert.True(list.All(p => p.s == null));
        }

        [Fact]
        public void SelectEmbeddedListElementNotNullableNull()
        {
            var list = (from a in Database.Query<AlbumEntity>()
                        from s in a.MListElements(_ => _.Songs).Where(s => s.Element.Seconds < 0).DefaultIfEmpty()
                        select new { a, s }).ToList();

            Assert.True(list.All(p => p.s == null));
        }

        [Fact]
        public void SelectWhereExpressionInSelectMany()
        {
            var max = 0;
            Expression<Func<AlbumEntity, bool>> blas = a => a.Id > max;

            var list = (from a in Database.Query<AlbumEntity>()
                        from s in Database.Query<AlbumEntity>().Where(blas)
                        select new { a, s }).ToList();
        }

        [Fact]
        public void SelectExplicitInterfaceImplementedField()
        {
            var list = (from a in Database.Query<AlbumEntity>()
                        select ((ISecretContainer)a).Secret.InSql()).ToList();
        }

        [Fact]
        public void SelectEmbedded()
        {
            var list = Database.Query<AlbumEntity>().SelectMany(a => a.Songs).ToList();
        }

        [Fact]
        public void SelectView()
        {
            if (Schema.Current.Settings.IsPostgres)
            {
                var list = Database.View<Signum.Engine.PostgresCatalog.PgClass>().ToList();
            }
            else
            {
                var list = Database.View<Signum.Engine.SchemaInfoTables.SysDatabases>().ToList();
            }
        }

        [Fact]
        public void SelectRetrieve()
        {
            var e = Assert.Throws<InvalidOperationException>(() => Database.Query<LabelEntity>().Select(l => l.Owner!.RetrieveAndRemember()).ToList());
            Assert.Contains("not supported", e.Message);
        }

        [Fact]
        public void SelectWithHint()
        {
            if (!Schema.Current.Settings.IsPostgres)
            {
                var list = Database.Query<AlbumEntity>().WithHint("INDEX(IX_Album_LabelID)").Select(a => a.Label.Name).ToList();
            }
        }

        [Fact]
        public void SelectAverageBool()
        {
            Expression<Func<AlbumEntity, bool>> selector = a => a.Id > 10;
            Expression<Func<AlbumEntity, double>> selectorDouble = Expression.Lambda<Func<AlbumEntity, double>>(Expression.Convert(selector.Body, typeof(double)), selector.Parameters.SingleEx());

            var list = Database.Query<AlbumEntity>().Average(selectorDouble);
        }

        [Fact]
        public void SelectVirtualMListNoDistinct()
        {
            var list = Database.Query<ArtistEntity>().ToList();

            Assert.True(!Database.Query<ArtistEntity>().QueryText().Contains("DISTINCT"));
        }

        [Fact]
        public void AvoidDecimalCastinInSql()
        {
            var list = Database.Query<ArtistEntity>()
                .Select(a => ((int)a.Id / 10m))
                .Select(a => ((decimal?)a).InSql()) //Avoid Cast( as decimal) in SQL because of https://stackoverflow.com/questions/4169520/casting-as-decimal-and-rounding
                .ToList();

            Assert.Contains(list, a => a!.Value != Math.Round(a!.Value)); //Decimal places are preserved

        }
    }

    public static class AuthorExtensions
    {
        [AutoExpressionField]
        public static int AlbumCount(this IAuthorEntity author) => 
            As.Expression(() => Database.Query<AlbumEntity>().Count(a => a.Author == author));
    }
}
