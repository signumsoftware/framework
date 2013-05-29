using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signum.Engine;
using Signum.Entities;
using Signum.Test.Environment;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Signum.Test.LinqProvider
{
    [TestClass]
    public class SelectImplementationsTest1
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

            //Assert.AreEqual(2, query.QueryText().CountRepetitions("LEFT OUTER JOIN"));
            Assert.AreEqual(3, query.QueryText().CountRepetitions("LEFT OUTER JOIN"));
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
                .Select(a => a.Award.EntityType).ToList();
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
            var list = Database.Query<AwardNominationDN>().Where(a => a.Award.EntityType == typeof(GrammyAwardDN)).ToList();
        }

        [TestMethod]
        public void SelectLiteUpcast()
        {
            var list = Database.Query<ArtistDN>()
                .Select(a => a.ToLite()).ToList();
        }

        [TestMethod]
        public void SelectLiteCastUpcast()
        {
            var list = Database.Query<ArtistDN>()
                .SelectMany(a => a.Friends).Select(a=>(Lite<IAuthorDN>)a).ToList();
        }

        [TestMethod]
        public void SelectLiteCastNocast()
        {
            var list = Database.Query<ArtistDN>()
                .SelectMany(a => a.Friends).Select(a =>(Lite<ArtistDN>)a).ToList();
        }

        [TestMethod]
        public void SelectLiteCastDowncast()
        {
            var list = Database.Query<AlbumDN>()
                .Select(a => (Lite<ArtistDN>)a.Author.ToLite()).ToList();
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
                        select n.Target is ArtistDN ? ((ArtistDN)n.Target).Name : ((BandDN)n.Target).Name).ToList();
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
        public void SelectIsIBLite()
        {
            var query = (from n in Database.Query<NoteWithDateDN>()
                         where n.Target.ToLite() is Lite<AlbumDN>
                         select n.Target.ToLite()).ToList();

        }

        [TestMethod]
        public void SelectIsIBALite()
        {
            var query = (from a in Database.Query<AwardNominationDN>()
                         where a.Author is Lite<BandDN>
                         select a.Author).ToList();

        }

        [TestMethod]
        public void SelectCastIBALite()
        {
            var query = (from n in Database.Query<NoteWithDateDN>()
                         select (Lite<AlbumDN>)n.Target.ToLite()).ToList();

        }

        [TestMethod]
        public void SelectCastIBLite()
        {
            var query = (from a in Database.Query<AwardNominationDN>()
                         select (Lite<BandDN>)a.Author).ToList();

        }

        //AwardNominationDN
    }
}
