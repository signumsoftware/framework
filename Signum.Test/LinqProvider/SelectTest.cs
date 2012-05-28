using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signum.Engine;
using Signum.Entities;
using System.Diagnostics;
using System.IO;
using Signum.Engine.Linq;
using Signum.Utilities;
using System.Linq.Expressions;
using System.Data.SqlTypes;
using System.Reflection;
using Signum.Utilities.ExpressionTrees;

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
            Starter.StartAndLoad();
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
        public void SelectType()
        {
            var list = Database.Query<AlbumDN>()
                .Select(a => a.GetType()).ToList();
        }

        [TestMethod]
        public void SelectTypeNull()
        {
            var list = Database.Query<LabelDN>()
                .Select(a => new { Label = a.ToLite(), Owner = a.Owner, OwnerType = a.Owner.Entity.GetType() }).ToList();
        }

        [TestMethod]
        public void SelectLiteIB()
        {
            var list = Database.Query<AlbumDN>()
                .Select(a => a.Author.ToLite()).ToList();
        }

        [TestMethod]
        public void SelectLiteIBDouble()
        {
            var query = Database.Query<AlbumDN>()
                .Select(a => new
                {
                    ToStr1 = a.Author.ToLite(),
                    ToStr2 = a.Author.ToLite()
                });

            Assert.AreEqual(2, query.QueryText().CountRepetitions("LEFT OUTER JOIN"));
            query.ToList(); 
        }


        [TestMethod]
        public void SelectLiteIBDoubleWhere()
        {
            var query = Database.Query<AlbumDN>()
                .Where(a => a.Author.ToLite().ToString().Length > 0)
                .Select(a => a.Author.ToLite());

            Assert.AreEqual(2, query.QueryText().CountRepetitions("LEFT OUTER JOIN"));
            query.ToList();
        }

        [TestMethod]
        public void SelectTypeIBA()
        {
            var list = Database.Query<NoteWithDateDN>()
                .Select(a => new { Type = a.Target.GetType(), Target = a.Target.ToLite() }).ToList();
        }

        [TestMethod]
        public void SelectTypeLiteIB()
        {
            var list = Database.Query<AwardNominationDN>()
                .Select(a => a.Award.RuntimeType).ToList();
        }

        [TestMethod]
        public void SelectEntityWithLiteIb()
        {
            var list = Database.Query<AwardNominationDN>().Where(a => a.Award.Entity is GrammyAwardDN).ToList();
        }

        [TestMethod]
        public void SelectEntityWithLiteIbType()
        {
            var list = Database.Query<AwardNominationDN>().Where(a => a.Award.Entity.GetType() == typeof(GrammyAwardDN)).ToList();
        }

        [TestMethod]
        public void SelectEntityWithLiteIbTypeContains()
        {
            Type[] types = new Type[] { typeof(GrammyAwardDN) }; 

            var list = Database.Query<AwardNominationDN>().Where(a => types.Contains(a.Award.Entity.GetType())).ToList();
        }

        [TestMethod]
        public void SelectEntityWithLiteIbRuntimeType()
        {
            var list = Database.Query<AwardNominationDN>().Where(a => a.Award.RuntimeType == typeof(GrammyAwardDN)).ToList();
        }

        [TestMethod]
        public void SelectLiteUpcast()
        {
            var list = Database.Query<ArtistDN>()
                .Select(a => a.ToLite<IAuthorDN>()).ToList();
        }

        [TestMethod]
        public void SelectLiteCastUpcast()
        {
            var list = Database.Query<ArtistDN>()
                .SelectMany(a => a.Friends).Select(a=>a.ToLite<IAuthorDN>()).ToList();
        }

        [TestMethod]
        public void SelectLiteCastNocast()
        {
            var list = Database.Query<ArtistDN>()
                .SelectMany(a => a.Friends).Select(a => a.ToLite<ArtistDN>()).ToList();
        }

        [TestMethod]
        public void SelectLiteCastDowncast()
        {
            var list = Database.Query<AlbumDN>()
                .Select(a => a.Author.ToLite().ToLite<ArtistDN>()).ToList();
        }


        [TestMethod]
        public void SelectLiteGenericUpcast()
        {
            var list = SelectAuthorsLite<ArtistDN, IAuthorDN>();
        }

        public List<Lite<LT>> SelectAuthorsLite<T, LT>()
            where T : IdentifiableEntity, LT
            where LT : class, IIdentifiable
        {
            return Database.Query<T>().Select(a => a.ToLite<LT>()).ToList(); //an explicit convert is injected in this scenario
        }

        [TestMethod]
        public void SelectLiteIBRedundant()
        {
            var list = (from a in Database.Query<AlbumDN>()
                        let band = (BandDN)a.Author
                        select new { Artist = band.ToString(), Author = a.Author.ToString() }).ToList();

            Assert.AreEqual(Database.Query<AlbumDN>().Count(), list.Count);
        }

        [TestMethod]
        public void SelectLiteIBWhere()
        {
            var list = Database.Query<AlbumDN>()
                .Select(a => a.Author.ToLite())
                .Where(a => a.ToString().StartsWith("Michael")).ToList();
        }

        [TestMethod]
        public void SelectLiteIBA()
        {
            var list = Database.Query<NoteWithDateDN>().Select(a => a.Target.ToLite()).ToList();
        }

        [TestMethod]
        public void SelectEntity()
        {
            var list3 = Database.Query<AlbumDN>().ToList();
        }

        [TestMethod]
        public void SelectEntityNone()
        {
            var list = Database.Query<AlbumDN>().ToList();
        }

        [TestMethod]
        public void SelectEntitySelect()
        {
            var list = Database.Query<AlbumDN>().Select(a => a).ToList();
        }

        [TestMethod]
        public void SelectEntityIB()
        {
            var list = Database.Query<AlbumDN>().Select(a => a.Author).ToList();
        }

        [TestMethod]
        public void SelectEntityIBRedundant()
        {
            var list = (from a in Database.Query<AlbumDN>()
                        let aut = a.Author
                        select new { aut, a.Author }).ToList();
        }

        [TestMethod]
        public void SelectEntityIBA()
        {
            var list = Database.Query<NoteWithDateDN>().Select(a => a.Target).ToList();
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
        public void SelectCastIB()
        {
            var list = (from a in Database.Query<AlbumDN>()
                       select ((ArtistDN)a.Author).Name ?? 
                              ((BandDN)a.Author).Name).ToList();
        }

        [TestMethod]
        public void SelectCastIBPolymorphic()
        {
            var list = (from a in Database.Query<AlbumDN>()
                        select a.Author.Name).ToList();
        }

        [TestMethod]
        public void SelectCastIBPolymorphicIB()
        {
            var list = (from a in Database.Query<AlbumDN>()
                        select a.Author.LastAward.ToLite()).ToList();
        }

        [TestMethod]
        public void SelectCastIBA()
        {
            var list = (from n in Database.Query<NoteWithDateDN>()
                        select 
                        ((ArtistDN)n.Target).Name ??
                        ((AlbumDN)n.Target).Name ??
                        ((BandDN)n.Target).Name).ToList();

        }

        [TestMethod]
        public void SelectCastIBACastOperator()
        {
            var list = (from n in Database.Query<NoteWithDateDN>()
                        select n.Target).Cast<BandDN>().ToList();
        }


        [TestMethod]
        public void SelectCastIBAOfTypeOperator()
        {
            var list = (from n in Database.Query<NoteWithDateDN>()
                        select n.Target).OfType<BandDN>().ToList();
        }

        [TestMethod]
        public void SelectCastIsIB()
        {
            var list = (from a in Database.Query<AlbumDN>()
                        select (a.Author is ArtistDN ? ((ArtistDN)a.Author).Name :
                                                        ((BandDN)a.Author).Name)).ToList();
        }

        [TestMethod]
        public void SelectCastIsIBA()
        {
            var list = (from n in Database.Query<NoteWithDateDN>()
                        select n.Target is ArtistDN ?((ArtistDN)n.Target).Name : ((BandDN)n.Target).Name).ToList(); 
        }

        [TestMethod]
        public void SelectCastIsIBADouble()
        {
            var query = (from n in Database.Query<NoteWithDateDN>()
                        select new
                        {
                            Name = n.Target is ArtistDN ? ((ArtistDN)n.Target).Name : ((BandDN)n.Target).Name,
                            FullName = n.Target is ArtistDN ? ((ArtistDN)n.Target).FullName : ((BandDN)n.Target).FullName
                        });

            Assert.AreEqual(1, query.QueryText().CountRepetitions("ArtistDN"));

            query.ToList();
        }

        [TestMethod]
        public void SelectCastIsIBADoubleWhere()
        {
            var query = (from n in Database.Query<NoteWithDateDN>()
                         where (n.Target is ArtistDN ? ((ArtistDN)n.Target).Name : ((BandDN)n.Target).Name).Length > 0
                         select n.Target is ArtistDN ? ((ArtistDN)n.Target).FullName : ((BandDN)n.Target).FullName);

            Assert.AreEqual(1, query.QueryText().CountRepetitions("ArtistDN"));

            query.ToList();
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
        public void SelectPolyExpressionProperty()
        {
            var list = Database.Query<AlbumDN>().Select(a => a.Author.FullName).ToArray();
        }

        [TestMethod]
        public void SelectPolyExpressionMethod()
        {
            var list2 = Database.Query<AlbumDN>().Select(a => a.Author is BandDN ? ((BandDN)a.Author).Members.Count : ((ArtistDN)a.Author).Friends.Count).ToArray();
            var list = Database.Query<AlbumDN>().Select(a => a.Author.Lonely()).ToArray();
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
                    AvgId = b.Members.Average(m => m.Id),
                    SumId = b.Members.Sum(m => m.Id),
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
        public void SelectToStrField()
        {
            var list = Database.Query<NoteWithDateDN>().Select(a => a.ToStringProperty).ToList();
        }

        [TestMethod]
        public void SelectFakedToString()
        {
            var list = Database.Query<AlbumDN>().Select(a => a.ToStringProperty).ToList();
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
