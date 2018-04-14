using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signum.Engine;
using Signum.Entities;
using Signum.Test.Environment;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Signum.Utilities.DataStructures;


namespace Signum.Test.LinqProvider
{
    /// <summary>
    /// Summary description for LinqProvider
    /// </summary>
    [TestClass]
    public class SelectTest
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
        public void Select()
        {
            var list = Database.Query<AlbumEntity>().Select(a => a.Name).ToList();
        }

        [TestMethod]
        public void SelectIndex()
        {
            var list = Database.Query<AlbumEntity>().Select((a, i) =>  a.Name + i).ToList();
        }

        [TestMethod]
        public void SelectIds()
        {
            var first = Database.Query<BandEntity>().Select(b => b.Id).ToList();
        }

        [TestMethod]
        public void SelectFirstId()
        {
            var first = Database.Query<BandEntity>().Select(b => b.Id).First();
        }

        [TestMethod]
        public void SelectExpansion()
        {
            var list = Database.Query<AlbumEntity>().Select(a => a.Label.Name).ToList();
        }

        [TestMethod]
        public void SelectLetExpansion()
        {
            var list = (from a in Database.Query<AlbumEntity>()
                        let l = a.Label
                        select l.Name).ToList();
        }

        [TestMethod]
        public void SelectLetExpansionRedundant()
        {
            var list = (from a in Database.Query<AlbumEntity>()
                        let label = a.Label
                        select new
                        {
                            Artist = label.Country.Name,
                            Author = a.Label.Name
                        }).ToList();

            Assert.AreEqual(Database.Query<AlbumEntity>().Count(), list.Count);
        }

        [TestMethod]
        public void SelectWhereExpansion()
        {
            var list = Database.Query<AlbumEntity>().Where(a=>a.Label != null).Select(a => a.Label.Name).ToList();
        }

        [TestMethod]
        public void SelectAnonymous()
        {
            var list = Database.Query<AlbumEntity>().Select(a => new { a.Name, a.Year }).ToList();
        }

        [TestMethod]
        public void SelectNoColumns()
        {
            var list = Database.Query<AlbumEntity>().Select(a => new { DateTime.Now, Album = (AlbumEntity)null, Artist = (Lite<ArtistEntity>)null }).ToList();
        }

        [TestMethod]
        public void SelectCount()
        {
            var list = Database.Query<AlbumEntity>().Select(a => (int?)a.Songs.Count).ToList();
        }

        [TestMethod]
        public void SelectLite()
        {
            var list = Database.Query<AlbumEntity>().Select(a => a.ToLite()).ToList();
        }

        [TestMethod]
        public void SelectLiteToStr()
        {
            var list = Database.Query<AlbumEntity>().Select(a => a.ToLite(a.Label.Name)).ToList();
        }


      
        [TestMethod]
        public void SelectBool()
        {
            var list = Database.Query<ArtistEntity>().Select(a => a.Dead).ToList();
        }

        [TestMethod]
        public void SelectConditionToBool()
        {
            var list = Database.Query<AlbumEntity>().Select(a => a.Year < 1990).ToList();
        }


        [TestMethod]
        public void SelectConditionalMember()
        {
            var list = (from l in Database.Query<LabelEntity>()
                        select (l.Owner == null ? l : l.Owner.Entity).Name).ToList();

        }

        [TestMethod]
        public void SelectConditionalToLite()
        {
            var list = (from l in Database.Query<LabelEntity>()
                        select (l.Owner == null ? l : l.Owner.Entity).ToLite()).ToList();
        }

#pragma warning disable IDE0029 // Use coalesce expression
        [TestMethod]
        public void SelectConditionalToLiteNull()
        {
            var list = (from l in Database.Query<LabelEntity>()
                        let owner = (l.Owner == null ? null : l.Owner).Entity
                        select owner.ToLite(owner.Name)).ToList();
        }
#pragma warning restore IDE0029 // Use coalesce expression

        [TestMethod]
        public void SelectConditionalGetType()
        {
            var list = (from l in Database.Query<LabelEntity>()
                        select (l.Owner == null ? l : l.Owner.Entity).GetType()).ToList();
        }

        [TestMethod]
        public void SelectCoallesceMember()
        {
            var list = (from l in Database.Query<LabelEntity>()
                        select (l.Owner.Entity ?? l ).Name).ToList();

        }

        [TestMethod]
        public void SelectCoallesceToLite()
        {
            var list = (from l in Database.Query<LabelEntity>()
                        select (l.Owner.Entity ?? l ).ToLite()).ToList();

        }

        [TestMethod]
        public void SelectCoallesceGetType()
        {
            var list = (from l in Database.Query<LabelEntity>()
                        select (l.Owner.Entity ?? l).GetType()).ToList();

        }

        [TestMethod]
        public void SelectUpCast()
        {
            var list = (from n in Database.Query<ArtistEntity>()
                        select (IAuthorEntity)n).ToList(); //Just to full-nominate
        }

        [TestMethod]
        public void SelectEntityEquals()
        {
            ArtistEntity michael = Database.Query<ArtistEntity>().SingleEx(a => a.Dead);

            var list = Database.Query<AlbumEntity>().Select(a => a.Author == michael).ToList(); 
        }

        [TestMethod]
        public void SelectBoolExpression()
        {
            ArtistEntity michael = Database.Query<ArtistEntity>().SingleEx(a => a.Dead);

            var list = Database.Query<AlbumEntity>().Select(a => a.Author == michael).ToList();
        }

        [TestMethod]
        public void SelectExpressionProperty()
        {
            var list = Database.Query<ArtistEntity>().Where(a => a.IsMale).ToArray();
        }

        [TestMethod]
        public void SelectExpressionMethod()
        {
            var list = Database.Query<ArtistEntity>().Select(a => new { a.Name, Count = a.AlbumCount() }).ToArray();
        }

        [TestMethod]
        public void SelectPolyExpressionPropertyUnion()
        {
            var list = Database.Query<AlbumEntity>().Select(a => a.Author.CombineUnion().FullName).ToArray();
        }

        [TestMethod]
        public void SelectPolyExpressionPropertySwitch()
        {
            var list = Database.Query<AlbumEntity>().Select(a => a.Author.CombineCase().FullName).ToArray();
        }

        [TestMethod]
        public void SelectPolyExpressionMethodUnion()
        {
            var list = Database.Query<AlbumEntity>().Select(a => a.Author.CombineUnion().Lonely()).ToArray();
        }

        [TestMethod]
        public void SelectPolyExpressionMethodSwitch()
        {
            var list = Database.Query<AlbumEntity>().Select(a => a.Author.CombineCase().Lonely()).ToArray();
        }

        [TestMethod]
        public void SelectPolyExpressionMethodManual()
        {
            var list = Database.Query<AlbumEntity>().Select(a => a.Author is BandEntity ? ((BandEntity)a.Author).Lonely(): ((ArtistEntity)a.Author).Lonely()).ToArray();
        }

        [TestMethod]
        public void SelectThrowIntNullable()
        {
            Assert2.Throws<FieldReaderException>(() =>
                Database.Query<AlbumEntity>().Select(a => ((ArtistEntity)a.Author).Id).ToArray());
        }

        [TestMethod]
        public void SelectThrowBoolNullable()
        {
            Assert2.Throws<FieldReaderException>(() =>
                Database.Query<AlbumEntity>().Select(a => ((ArtistEntity)a.Author).Dead).ToArray());
        }
        
        [TestMethod]
        public void SelectThrowEnumNullable()
        {
            Assert2.Throws<FieldReaderException>(() =>
                Database.Query<AlbumEntity>().Select(a => ((ArtistEntity)a.Author).Sex).ToArray());
        }

        [TestMethod]
        public void SelectIntNullable()
        {
            var list = Database.Query<AlbumEntity>().Select(a => (int?)((ArtistEntity)a.Author).Id).ToArray();
        }

        [TestMethod]
        public void SelectBoolNullable()
        {
            var list = Database.Query<AlbumEntity>().Select(a => (bool?)((ArtistEntity)a.Author).Dead).ToArray();
        }

        [TestMethod]
        public void SelectEnumNullable()
        {
            var list = Database.Query<ArtistEntity>().Select(a => a.Status).ToArray();
        }

        [TestMethod]
        public void SelectEnumNullableNullable()
        {
            var list = Database.Query<AlbumEntity>().Select(a => ((ArtistEntity)a.Author).Status).ToArray();
        }

        [TestMethod]
        public void SelectThrowsIntSumNullable()
        {
            Assert2.Throws<FieldReaderException>(() =>
                Database.Query<AlbumEntity>().Select(a => (int)a.Id + (int)((ArtistEntity)a.Author).Id).ToArray());
        }

        [TestMethod]
        public void SelectThrowaIntSumNullableCasting()
        {
            Assert2.Throws<FieldReaderException>(() => 
                Database.Query<AlbumEntity>().Select(a => (int?)((int)a.Id + (int)((ArtistEntity)a.Author).Id)).ToArray());
        }

        [TestMethod]
        public void SelectThrowaIntSumNullableCastingInSql()
        {
            var list = Database.Query<AlbumEntity>().Select(a => (int?)((int)a.Id + (int)((ArtistEntity)a.Author).Id).InSql()).ToArray();
        }


        [TestMethod]
        public void SelectEnumNullableBullableCast()
        {
            var list = Database.Query<AlbumEntity>().Select(a => (Sex?)((ArtistEntity)a.Author).Sex).ToArray();
        }

        [TestMethod]
        public void SelectEnumNullableValue()
        {
            var list = Database.Query<AlbumEntity>().Where(a => a.Author is ArtistEntity)
                .Select(a => ((Sex?)((ArtistEntity)a.Author).Sex).Value).ToArray();
        }

        [TestMethod]
        public void SelectEmbeddedNullable()
        {
            var bonusTracks = Database.Query<AlbumEntity>().Select(a => a.BonusTrack).ToArray();
        }

        [TestMethod]
        public void SelectMixinThrows()
        {
            Assert2.Throws<InvalidOperationException>("without their main entity", () =>
                Database.Query<NoteWithDateEntity>().Select(a => a.Mixin<CorruptMixin>()).ToArray());
        }


        [TestMethod]
        public void SelectMixinField()
        {
            Database.Query<NoteWithDateEntity>().Select(a => a.Mixin<CorruptMixin>().Corrupt).ToArray();
        }

        [TestMethod]
        public void SelectMixinWhere()
        {
            Database.Query<NoteWithDateEntity>().Where(a => a.Mixin<CorruptMixin>().Corrupt == true).ToArray();
        }

        [TestMethod]
        public void SelectMixinCollection()
        {
            var result = (from n in Database.Query<NoteWithDateEntity>()
                          from c in n.Mixin<ColaboratorsMixin>().Colaborators
                          select c).ToArray();
        }

        [TestMethod]
        public void SelectNullable()
        {
            var durations = (from a in Database.Query<AlbumEntity>()
                             from s in a.Songs
                             where s.Seconds.HasValue
                             select s.Seconds.Value).ToArray();
        }

        [TestMethod]
        public void SelectIsNull()
        {
            var durations = (from a in Database.Query<AlbumEntity>()
                             from s in a.Songs
                             where s.Seconds.HasValue
                             select s.Seconds == null).ToArray();
        }

        [TestMethod]
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

        [TestMethod]
        public void SelectAvoidNominateEntity()
        {
            var durations =
                (from a in Database.Query<AlbumEntity>()
                 select new
                 {
                     a.Name,
                     Friend = (Lite<BandEntity>)null
                 }).ToList();
        }


        [TestMethod]
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

        [TestMethod]
        public void SelectMemoryEntity()
        {
            var artist = Database.Query<ArtistEntity>().FirstEx();

            var songs = Database.Query<AlbumEntity>().Select(a => new
            {
                Lite = a.ToLite(),
                Memory = artist,
            }).ToList(); 
        }

        [TestMethod]
        public void SelectMemoryLite()
        {
            var artist = Database.Query<ArtistEntity>().Select(a=>a.ToLite()).FirstEx();

            var songs = Database.Query<AlbumEntity>().Select(a => new
            {
                Lite = a.ToLite(),
                MemoryLite = artist,
            }).ToList();
        }

        [TestMethod]
        public void SelectOutsideStringNull()
        {
            var awards = Database.Query<GrammyAwardEntity>().Select(a => ((AmericanMusicAwardEntity)(AwardEntity)a).Category).ToList();
        }

        [TestMethod]
        public void SelectOutsideLiteNull()
        {
            var awards = Database.Query<GrammyAwardEntity>().Select(a => ((AmericanMusicAwardEntity)(AwardEntity)a).ToLite()).ToList();
        }

        [TestMethod]
        public void SelectMListLite()
        {
            var lists = (from mle in Database.MListQuery((ArtistEntity a) => a.Friends)
                         select new { Artis = mle.Parent.Name, Friend = mle.Element.Entity.Name }).ToList();
        }

        [TestMethod]
        public void SelectMListEntity()
        {
            var lists = (from mle in Database.MListQuery((BandEntity a) => a.Members)
                         select new { Band = mle.Parent.Name, Artis = mle.Element.Name }).ToList();
        }

        [TestMethod]
        public void SelectMListEmbedded()
        {
            var lists = (from mle in Database.MListQuery((AlbumEntity a) => a.Songs)
                         select mle).ToList();
        }

        [TestMethod]
        public void SelectMListEmbeddedToList()
        {
            var lists = (from a in Database.Query<AlbumEntity>()
                         select new
                         {
                             a.Name,
                             Songs = a.Songs.ToList(),
                         }).ToList();
        }


        [TestMethod]
        public void SelectMListPotentialDuplicates()
        {
            var sp = (from alb in Database.Query<AlbumEntity>()
                      let mich = ((ArtistEntity)alb.Author)
                      where mich.Name.Contains("Michael")
                      select mich).ToList();

            var single = sp.Distinct(ReferenceEqualityComparer<ArtistEntity>.Default).SingleEx();

            Assert.AreEqual(single.Friends.Distinct().Count(), single.Friends.Count);
        }

        [TestMethod]
        public void SelectIBAId()
        {
            var list = Database.Query<ArtistEntity>().Select(a => (PrimaryKey?)a.LastAward.Id).ToList();
        }

        [TestMethod]
        public void SelectIBAIdObject()
        {
            Assert2.Throws<InvalidOperationException>("translated", () =>
                Database.Query<ArtistEntity>().Select(a => ((int?)a.LastAward.Id).InSql()).ToList());
        }

        [TestMethod]
        public void SelectToStrField()
        {
            var list = Database.Query<NoteWithDateEntity>().Select(a => a.ToStringProperty).ToList();
        }

        [TestMethod]
        public void SelectFakedToString()
        {
            var list = Database.Query<AlbumEntity>().Select(a => a.ToStringProperty).ToList();
        }

        [TestMethod]
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

        [TestMethod]
        public void SelectToString()
        {
            var list = Database.Query<AlbumEntity>().Select(a => a.ToString()).ToList();
        }

        [TestMethod]
        public void SelectToStringLite()
        {
            var list = Database.Query<AlbumEntity>().Select(a => a.ToLite().ToString()).ToList();
        }

        [TestMethod]
        public void SelectConditionEnum()
        {
            var results = from b in Database.Query<BandEntity>()
                      let ga = (GrammyAwardEntity)b.LastAward
                      select (AwardResult?)(ga.Result < ga.Result ? (int)ga.Result : (int)ga.Result).InSql();

            results.ToList();
        }

        [TestMethod]
        public void SelectMListId()
        {
            var list = Database.Query<ArtistEntity>().SelectMany(a => a.Friends).Select(a => a.Id).ToList();
        }

        [TestMethod]
        public void SelectMListIdCovariance()
        {
            var list = Database.Query<ArtistEntity>().SelectMany(a => a.FriendsCovariant()).Select(a => a.Id).ToList();
        }

        [TestMethod]
        public void SelectEmbeddedListNotNullableNull()
        {
            var list = (from a in Database.Query<AlbumEntity>()
                        from s in a.Songs.Where(s => s.Seconds < 0).DefaultIfEmpty()
                        select new { a, s }).ToList();

            Assert.IsTrue(list.All(p => p.s == null));
        }

        [TestMethod]
        public void SelectEmbeddedListElementNotNullableNull()
        {
            var list = (from a in Database.Query<AlbumEntity>()
                        from s in a.MListElements(_=>_.Songs).Where(s => s.Element.Seconds < 0).DefaultIfEmpty()
                        select new { a, s }).ToList();

            Assert.IsTrue(list.All(p => p.s == null));
        }

        [TestMethod]
        public void SelectWhereExpressionInSelectMany()
        {
            var max = 0;
            Expression<Func<AlbumEntity, bool>> blas = a=>a.Id > max;

            var list = (from a in Database.Query<AlbumEntity>()
                        from s in Database.Query<AlbumEntity>().Where(blas)
                        select new { a, s }).ToList();
        }

        [TestMethod]
        public void SelectExplicitInterfaceImplementedField()
        {
            var list = (from a in Database.Query<AlbumEntity>()
                        select ((ISecretContainer)a).Secret.InSql()).ToList();
        }

        [TestMethod]
        public void SelectEmbedded()
        {
            var list = Database.Query<AlbumEntity>().SelectMany(a => a.Songs).ToList();
        }

        [TestMethod]
        public void SelectView()
        {
            var list = Database.View<Signum.Engine.SchemaInfoTables.SysDatabases>().ToList();
        }

        [TestMethod]
        public void SelectRetrieve()
        {
            Assert2.Throws<InvalidOperationException>("not supported",
                () => Database.Query<LabelEntity>().Select(l => l.Owner.Retrieve()).ToList());
        }

        [TestMethod]
        public void SelectWithHint()
        {
            var list = Database.Query<AlbumEntity>().WithHint("INDEX(IX_LabelID)").Select(a => a.Label.Name).ToList();
        }

        [TestMethod]
        public void SelectAverageBool()
        {
            Expression<Func<AlbumEntity, bool>> selector = a => a.Id > 10;
            Expression<Func<AlbumEntity, double>> selectorDouble = Expression.Lambda<Func<AlbumEntity, double>>(Expression.Convert(selector.Body, typeof(double)), selector.Parameters.SingleEx());

            var list = Database.Query<AlbumEntity>().Average(selectorDouble);
        }

        [TestMethod]
        public void SelectVirtualMListNoDistinct()
        {
            var list = Database.Query<ArtistEntity>().ToList();

            Assert.IsTrue(!Database.Query<ArtistEntity>().QueryText().Contains("DISTINCT"));
        }
    }

    public static class AuthorExtensions
    {
        static Expression<Func<IAuthorEntity, int>> AlbumCountExpression = auth => Database.Query<AlbumEntity>().Count(a => a.Author == auth);
        [ExpressionField]
        public static int AlbumCount(this IAuthorEntity author)
        {
            return AlbumCountExpression.Evaluate(author);
        }
    }
}
