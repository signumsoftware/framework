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
            var list = Database.Query<AlbumDN>().Select(a => a.Name).ToList();
        }

        [TestMethod]
        public void SelectIndex()
        {
            var list = Database.Query<AlbumDN>().Select((a, i) =>  a.Name + i).ToList();
        }

        [TestMethod]
        public void SelectIds()
        {
            var first = Database.Query<BandDN>().Select(b => b.Id).ToList();
        }

        [TestMethod]
        public void SelectFirstId()
        {
            var first = Database.Query<BandDN>().Select(b => b.Id).First();
        }

        [TestMethod]
        public void SelectExpansion()
        {
            var list = Database.Query<AlbumDN>().Select(a => a.Label.Name).ToList();
        }

        [TestMethod]
        public void SelectLetExpansion()
        {
            var list = (from a in Database.Query<AlbumDN>()
                        let l = a.Label
                        select l.Name).ToList();
        }

        [TestMethod]
        public void SelectLetExpansionRedundant()
        {
            var list = (from a in Database.Query<AlbumDN>()
                        let label = a.Label
                        select new
                        {
                            Artist = label.Country.Name,
                            Author = a.Label.Name
                        }).ToList();

            Assert.AreEqual(Database.Query<AlbumDN>().Count(), list.Count);
        }

        [TestMethod]
        public void SelectWhereExpansion()
        {
            var list = Database.Query<AlbumDN>().Where(a=>a.Label != null).Select(a => a.Label.Name).ToList();
        }

        [TestMethod]
        public void SelectAnonymous()
        {
            var list = Database.Query<AlbumDN>().Select(a => new { a.Name, a.Year }).ToList();
        }

        [TestMethod]
        public void SelectNoColumns()
        {
            var list = Database.Query<AlbumDN>().Select(a => new { DateTime.Now, Album = (AlbumDN)null, Artist = (Lite<ArtistDN>)null }).ToList();
        }

        [TestMethod]
        public void SelectCount()
        {
            var list = Database.Query<AlbumDN>().Select(a => (int?)a.Songs.Count).ToList();
        }

        [TestMethod]
        public void SelectLite()
        {
            var list = Database.Query<AlbumDN>().Select(a => a.ToLite()).ToList();
        }

        [TestMethod]
        public void SelectLiteToStr()
        {
            var list = Database.Query<AlbumDN>().Select(a => a.ToLite(a.Label.Name)).ToList();
        }


      
        [TestMethod]
        public void SelectBool()
        {
            var list = Database.Query<ArtistDN>().Select(a => a.Dead).ToList();
        }

        [TestMethod]
        public void SelectConditionToBool()
        {
            var list = Database.Query<AlbumDN>().Select(a => a.Year < 1990).ToList();
        }


        [TestMethod]
        public void SelectConditionalMember()
        {
            var list = (from l in Database.Query<LabelDN>()
                        select (l.Owner == null ? l : l.Owner.Entity).Name).ToList();

        }

        [TestMethod]
        public void SelectConditionalToLite()
        {
            var list = (from l in Database.Query<LabelDN>()
                        select (l.Owner == null ? l : l.Owner.Entity).ToLite()).ToList();
        }

        [TestMethod]
        public void SelectConditionalGetType()
        {
            var list = (from l in Database.Query<LabelDN>()
                        select (l.Owner == null ? l : l.Owner.Entity).GetType()).ToList();
        }

        [TestMethod]
        public void SelectCoallesceMember()
        {
            var list = (from l in Database.Query<LabelDN>()
                        select (l.Owner.Entity ?? l ).Name).ToList();

        }

        [TestMethod]
        public void SelectCoallesceToLite()
        {
            var list = (from l in Database.Query<LabelDN>()
                        select (l.Owner.Entity ?? l ).ToLite()).ToList();

        }

        [TestMethod]
        public void SelectCoallesceGetType()
        {
            var list = (from l in Database.Query<LabelDN>()
                        select (l.Owner.Entity ?? l).GetType()).ToList();

        }

        [TestMethod]
        public void SelectUpCast()
        {
            var list = (from n in Database.Query<ArtistDN>()
                        select (IAuthorDN)n).ToList(); //Just to full-nominate
        }

        [TestMethod]
        public void SelectEntityEquals()
        {
            ArtistDN michael = Database.Query<ArtistDN>().SingleEx(a => a.Dead);

            var list = Database.Query<AlbumDN>().Select(a => a.Author == michael).ToList(); 
        }

        [TestMethod]
        public void SelectBoolExpression()
        {
            ArtistDN michael = Database.Query<ArtistDN>().SingleEx(a => a.Dead);

            var list = Database.Query<AlbumDN>().Select(a => a.Author == michael).ToList();
        }

        [TestMethod]
        public void SelectExpressionProperty()
        {
            var list = Database.Query<ArtistDN>().Where(a => a.IsMale).ToArray();
        }

        [TestMethod]
        public void SelectExpressionMethod()
        {
            var list = Database.Query<ArtistDN>().Select(a => new { a.Name, Count = a.AlbumCount() }).ToArray();
        }

        [TestMethod]
        public void SelectPolyExpressionPropertyUnion()
        {
            var list = Database.Query<AlbumDN>().Select(a => a.Author.CombineUnion().FullName).ToArray();
        }

        [TestMethod]
        public void SelectPolyExpressionPropertySwitch()
        {
            var list = Database.Query<AlbumDN>().Select(a => a.Author.CombineCase().FullName).ToArray();
        }

        [TestMethod]
        public void SelectPolyExpressionMethodUnion()
        {
            var list = Database.Query<AlbumDN>().Select(a => a.Author.CombineUnion().Lonely()).ToArray();
        }

        [TestMethod]
        public void SelectPolyExpressionMethodSwitch()
        {
            var list = Database.Query<AlbumDN>().Select(a => a.Author.CombineCase().Lonely()).ToArray();
        }

        [TestMethod]
        public void SelectPolyExpressionMethodManual()
        {
            var list = Database.Query<AlbumDN>().Select(a => a.Author is BandDN ? ((BandDN)a.Author).Lonely(): ((ArtistDN)a.Author).Lonely()).ToArray();
        }

        [TestMethod]
        public void SelectThrowIntNullable()
        {
            Assert2.Throws<FieldReaderException>(() =>
                Database.Query<AlbumDN>().Select(a => ((ArtistDN)a.Author).Id).ToArray());
        }

        [TestMethod]
        public void SelectThrowBoolNullable()
        {
            Assert2.Throws<FieldReaderException>(() =>
                Database.Query<AlbumDN>().Select(a => ((ArtistDN)a.Author).Dead).ToArray());
        }
        
        [TestMethod]
        public void SelectThrowEnumNullable()
        {
            Assert2.Throws<FieldReaderException>(() =>
                Database.Query<AlbumDN>().Select(a => ((ArtistDN)a.Author).Sex).ToArray());
        }

        [TestMethod]
        public void SelectIntNullable()
        {
            var list = Database.Query<AlbumDN>().Select(a => (int?)((ArtistDN)a.Author).Id).ToArray();
        }

        [TestMethod]
        public void SelectBoolNullable()
        {
            var list = Database.Query<AlbumDN>().Select(a => (bool?)((ArtistDN)a.Author).Dead).ToArray();
        }

        [TestMethod]
        public void SelectEnumNullable()
        {
            var list = Database.Query<ArtistDN>().Select(a => a.Status).ToArray();
        }

        [TestMethod]
        public void SelectEnumNullableNullable()
        {
            var list = Database.Query<AlbumDN>().Select(a => ((ArtistDN)a.Author).Status).ToArray();
        }

        [TestMethod]
        public void SelectThrowsIntSumNullable()
        {
            Assert2.Throws<FieldReaderException>(() =>
                Database.Query<AlbumDN>().Select(a => (int)a.Id + (int)((ArtistDN)a.Author).Id).ToArray());
        }

        [TestMethod]
        public void SelectThrowaIntSumNullableCasting()
        {
            Assert2.Throws<FieldReaderException>(() => 
                Database.Query<AlbumDN>().Select(a => (int?)((int)a.Id + (int)((ArtistDN)a.Author).Id)).ToArray());
        }

        [TestMethod]
        public void SelectThrowaIntSumNullableCastingInSql()
        {
            var list = Database.Query<AlbumDN>().Select(a => (int?)((int)a.Id + (int)((ArtistDN)a.Author).Id).InSql()).ToArray();
        }


        [TestMethod]
        public void SelectEnumNullableBullableCast()
        {
            var list = Database.Query<AlbumDN>().Select(a => (Sex?)((ArtistDN)a.Author).Sex).ToArray();
        }

        [TestMethod]
        public void SelectEnumNullableValue()
        {
            var list = Database.Query<AlbumDN>().Where(a => a.Author is ArtistDN)
                .Select(a => ((Sex?)((ArtistDN)a.Author).Sex).Value).ToArray();
        }

        [TestMethod]
        public void SelectEmbeddedNullable()
        {
            var bonusTracks = Database.Query<AlbumDN>().Select(a => a.BonusTrack).ToArray();
        }

        [TestMethod]
        public void SelectMixinThrows()
        {
            Assert2.Throws<InvalidOperationException>("without their main entity", () =>
                Database.Query<NoteWithDateDN>().Select(a => a.Mixin<CorruptMixin>()).ToArray());
        }


        [TestMethod]
        public void SelectMixinField()
        {
            Database.Query<NoteWithDateDN>().Select(a => a.Mixin<CorruptMixin>().Corrupt).ToArray();
        }

        [TestMethod]
        public void SelectMixinWhere()
        {
            Database.Query<NoteWithDateDN>().Where(a => a.Mixin<CorruptMixin>().Corrupt == true).ToArray();
        }

        [TestMethod]
        public void SelectMixinCollection()
        {
            var result = (from n in Database.Query<NoteWithDateDN>()
                          from c in n.Mixin<ColaboratorsMixin>().Colaborators
                          select c).ToArray();
        }

        [TestMethod]
        public void SelectNullable()
        {
            var durations = (from a in Database.Query<AlbumDN>()
                             from s in a.Songs
                             where s.Seconds.HasValue
                             select s.Seconds.Value).ToArray();
        }

        [TestMethod]
        public void SelectIsNull()
        {
            var durations = (from a in Database.Query<AlbumDN>()
                             from s in a.Songs
                             where s.Seconds.HasValue
                             select s.Seconds == null).ToArray();
        }

        [TestMethod]
        public void SelectAvoidNominate()
        {
            var durations =
                (from a in Database.Query<AlbumDN>()
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
                (from a in Database.Query<AlbumDN>()
                 select new
                 {
                     a.Name,
                     Friend = (Lite<BandDN>)null
                 }).ToList();
        }


        [TestMethod]
        public void SelectSingleCellAggregate()
        {
            var list = Database.Query<BandDN>()
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
            var artist = Database.Query<ArtistDN>().FirstEx();

            var songs = Database.Query<AlbumDN>().Select(a => new
            {
                Lite = a.ToLite(),
                Memory = artist,
            }).ToList(); 
        }

        [TestMethod]
        public void SelectMemoryLite()
        {
            var artist = Database.Query<ArtistDN>().Select(a=>a.ToLite()).FirstEx();

            var songs = Database.Query<AlbumDN>().Select(a => new
            {
                Lite = a.ToLite(),
                MemoryLite = artist,
            }).ToList();
        }

        [TestMethod]
        public void SelectOutsideStringNull()
        {
            var awards = Database.Query<GrammyAwardDN>().Select(a => ((AmericanMusicAwardDN)(AwardDN)a).Category).ToList();
        }

        [TestMethod]
        public void SelectOutsideLiteNull()
        {
            var awards = Database.Query<GrammyAwardDN>().Select(a => ((AmericanMusicAwardDN)(AwardDN)a).ToLite()).ToList();
        }

        [TestMethod]
        public void SelectMListLite()
        {
            var lists = (from mle in Database.MListQuery((ArtistDN a) => a.Friends)
                         select new { Artis = mle.Parent.Name, Friend = mle.Element.Entity.Name }).ToList();
        }

        [TestMethod]
        public void SelectMListEntity()
        {
            var lists = (from mle in Database.MListQuery((BandDN a) => a.Members)
                         select new { Band = mle.Parent.Name, Artis = mle.Element.Name }).ToList();
        }

        [TestMethod]
        public void SelectMListEmbedded()
        {
            var lists = (from mle in Database.MListQuery((AlbumDN a) => a.Songs)
                         select mle).ToList();
        }

        [TestMethod]
        public void SelectMListEmbeddedToList()
        {
            var lists = (from a in Database.Query<AlbumDN>()
                         select new
                         {
                             a.Name,
                             Songs = a.Songs.ToList(),
                         }).ToList();
        }


        [TestMethod]
        public void SelectMListPotentialDuplicates()
        {
            var sp = (from alb in Database.Query<AlbumDN>()
                      let mich = ((ArtistDN)alb.Author)
                      where mich.Name.Contains("Michael")
                      select mich).ToList();

            var single = sp.Distinct(ReferenceEqualityComparer<ArtistDN>.Default).SingleEx();

            Assert.AreEqual(single.Friends.Distinct().Count(), single.Friends.Count);
        }

        [TestMethod]
        public void SelectIBAId()
        {
            var list = Database.Query<ArtistDN>().Select(a => (PrimaryKey?)a.LastAward.Id).ToList();
        }

        [TestMethod]
        public void SelectIBAIdObject()
        {
            Assert2.Throws<InvalidOperationException>("translated", () =>
                Database.Query<ArtistDN>().Select(a => ((int?)a.LastAward.Id).InSql()).ToList());
        }

        [TestMethod]
        public void SelectToStrField()
        {
            var list = Database.Query<NoteWithDateDN>().Select(a => a.ToStringProperty).ToList();
        }

        [TestMethod]
        public void SelectFakedToString()
        {
            var list = Database.Query<AlbumDN>().Select(a => a.ToStringProperty).ToList();
        }

        [TestMethod]
        public void SelectConditionFormat()
        {
            var list = Database.Query<AlbumDN>().Select(a =>
                new
                {
                    Wrong = a.Author.GetType() == typeof(BandDN) ?
                        "Band {0}".Formato(((BandDN)a.Author).ToString()) :
                        "Artist {0}".Formato(((ArtistDN)a.Author).ToString()),

                    Right = a.Author is BandDN ?
                          "Band {0}".Formato(((BandDN)a.Author).ToString()) :
                          "Artist {0}".Formato(((ArtistDN)a.Author).ToString()),
                }).ToList();
        }

        [TestMethod]
        public void SelectToString()
        {
            var list = Database.Query<AlbumDN>().Select(a => a.ToString()).ToList();
        }

        [TestMethod]
        public void SelectTryToString()
        {
            var list = Database.Query<AlbumDN>().Select(a => a.TryToString()).ToList();
        }

        [TestMethod]
        public void SelectToStringLite()
        {
            var list = Database.Query<AlbumDN>().Select(a => a.ToLite().ToString()).ToList();
        }

        [TestMethod]
        public void SelectTryToStringLite()
        {
            var list = Database.Query<AlbumDN>().Select(a => a.ToLite().TryToString()).ToList();
        }


        [TestMethod]
        public void SelectConditionEnum()
        {
            var results = from b in Database.Query<BandDN>()
                      let ga = (GrammyAwardDN)b.LastAward
                      select (AwardResult?)(ga.Result < ga.Result ? (int)ga.Result : (int)ga.Result).InSql();

            results.ToList();
        }

        [TestMethod]
        public void SelectMListId()
        {
            var list = Database.Query<ArtistDN>().SelectMany(a => a.Friends).Select(a => a.Id).ToList();
        }

        [TestMethod]
        public void SelectMListIdCovariance()
        {
            var list = Database.Query<ArtistDN>().SelectMany(a => a.FriendsCovariant()).Select(a => a.Id).ToList();
        }

        [TestMethod]
        public void SelectEmbeddedListNotNullableNull()
        {
            var list = (from a in Database.Query<AlbumDN>()
                        from s in a.Songs.Where(s => s.Seconds < 0).DefaultIfEmpty()
                        select new { a, s }).ToList();

            Assert.IsTrue(list.All(p => p.s == null));
        }

        [TestMethod]
        public void SelectEmbeddedListElementNotNullableNull()
        {
            var list = (from a in Database.Query<AlbumDN>()
                        from s in a.MListElements(_=>_.Songs).Where(s => s.Element.Seconds < 0).DefaultIfEmpty()
                        select new { a, s }).ToList();

            Assert.IsTrue(list.All(p => p.s == null));
        }

        [TestMethod]
        public void SelectWhereExpressionInSelectMany()
        {
            var max = 0;
            Expression<Func<AlbumDN, bool>> blas = a=>a.Id > max;

            var list = (from a in Database.Query<AlbumDN>()
                        from s in Database.Query<AlbumDN>().Where(blas)
                        select new { a, s }).ToList();
        }

        [TestMethod]
        public void SelectExplicitInterfaceImplementedField()
        {
            var list = (from a in Database.Query<AlbumDN>()
                        select ((ISecretContainer)a).Secret.InSql()).ToList();
        }
    }

    public static class AuthorExtensions
    {
        static Expression<Func<IAuthorDN, int>> AlbumCountExpression = auth => Database.Query<AlbumDN>().Count(a => a.Author == auth);
        public static int AlbumCount(this IAuthorDN author)
        {
            return Database.Query<AlbumDN>().Count(a => a.Author == author);
        }
    }
}
