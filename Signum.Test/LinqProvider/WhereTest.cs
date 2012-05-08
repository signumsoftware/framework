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
            Starter.StartAndLoad();
        }

        [TestInitialize]
        public void Initialize()
        {
            Connector.CurrentLogger = new DebugTextWriter();
        }      
     
        [TestMethod]
        public void Where()
        {
            var list = Database.Query<AlbumDN>().Where(a => a.Year < 1995).ToList();
        }

        [TestMethod]
        public void WhereIndex()
        {
            var list = Database.Query<AlbumDN>().Where((a,i) => i % 2 == 0).ToList();
        }

        [TestMethod]
        public void WhereExplicitConvert()
        {
            var list = Database.Query<AlbumDN>().Where(a=>a.Id.ToString() == "1").ToList();
        }

        [TestMethod]
        public void WhereImplicitConvert()
        {
            Database.Query<AlbumDN>().Where(a => ("C" + a.Id) == "C1").Any();
        }


        [TestMethod]
        public void WhereSelect()
        {
            var list = Database.Query<AlbumDN>().Where(a => a.Year < 1995).Select(a => new { a.Year, Author = a.Author.ToLite(), a.Name }).ToList();
        }
        
        [TestMethod]
        public void WhereBool()
        {
            var list = Database.Query<ArtistDN>().Where(a => a.Dead).ToList();
        }

        [TestMethod]
        public void WhereNotNull()
        {
            var list = Database.Query<ArtistDN>().Where(a => a.LastAward != null).ToList();
        }

        [TestMethod]
        public void SingleFirstLast()
        {
            var artists = Database.Query<ArtistDN>();

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
            var artists = Database.Query<ArtistDN>();

            Assert2.Throws<InvalidOperationException>(() => artists.Where(a => a.Dead && !a.Dead).SingleEx(() => "Y"), "Y");
            Assert2.Throws<InvalidOperationException>(() => artists.Where(a => a.Sex == Sex.Male).SingleEx(() => "X", ()=>"Y"), "Y");
            Assert2.Throws<InvalidOperationException>(() => artists.Where(a => a.Sex == Sex.Male).SingleOrDefaultEx(() => "Y"), "Y");
            Assert2.Throws<InvalidOperationException>(() => artists.Where(a => a.Dead && !a.Dead).FirstEx(() => "X"), "X");

            Assert2.Throws<InvalidOperationException>(() => artists.SingleEx(a => a.Dead && !a.Dead),typeof(ArtistDN).Name);
            Assert2.Throws<InvalidOperationException>(() => artists.SingleEx(a => a.Sex == Sex.Male), typeof(ArtistDN).Name);
            Assert2.Throws<InvalidOperationException>(() => artists.SingleOrDefaultEx(a => a.Sex == Sex.Male), typeof(ArtistDN).Name);
            Assert2.Throws<InvalidOperationException>(() => artists.FirstEx(a => a.Dead && !a.Dead), typeof(ArtistDN).Name);


            Assert2.Throws<InvalidOperationException>(() => artists.Where(a => a.Dead && !a.Dead).SingleOrManyEx(() => "X"), "X");
        }


        [TestMethod]
        public void WhereEntityEquals()
        {
            ArtistDN wretzky = Database.Query<ArtistDN>().SingleEx(a => a.Sex == Sex.Female);

            BandDN smashing = (from b in Database.Query<BandDN>()
                               from a in b.Members
                               where a == wretzky
                               select b).SingleEx(); 
        }


        [TestMethod]
        public void WhereLiteEquals()
        {
            ArtistDN wretzky = Database.Query<ArtistDN>().SingleEx(a => a.Sex == Sex.Female);

            BandDN smashing = (from b in Database.Query<BandDN>()
                               from a in b.Members
                               where a.ToLite() == wretzky.ToLite()
                               select b).SingleEx();
        }


        [TestMethod]
        public void WhereEntityEqualsIB()
        {
            ArtistDN michael = Database.Query<ArtistDN>().SingleEx(a => a.Dead);

            var albums = (from a in Database.Query<AlbumDN>()
                          where a.Author == michael
                          select a.ToLite()).ToList();
        }

        [TestMethod]
        public void WhereEntityEqualsIBA()
        {
            ArtistDN michael = Database.Query<ArtistDN>().SingleEx(a => a.Dead);

            var albums = (from n in Database.Query<NoteWithDateDN>()
                          where n.Target == michael
                          select n.ToLite()).ToList();
        }

        [TestMethod]
        public void WhereLiteEqualsIB()
        {
            ArtistDN michael = Database.Query<ArtistDN>().SingleEx(a => a.Dead);

            var albums = (from a in Database.Query<AlbumDN>()
                          where a.Author.ToLite() == michael.ToLite<IAuthorDN>()
                          select a.ToLite()).ToList();
        }

        [TestMethod]
        public void WhereLiteEqualsIBA()
        {
            ArtistDN michael = Database.Query<ArtistDN>().SingleEx(a => a.Dead);

            var albums = (from n in Database.Query<NoteWithDateDN>()
                          where n.Target.ToLite() == michael.ToLite<IIdentifiable>()
                          select n.ToLite()).ToList();
        }

        [TestMethod]
        public void WhereIs()
        {
            var albums = (from a in Database.Query<AlbumDN>()
                          where a.Author is ArtistDN
                          select a.ToLite()).ToList();
        }

        [TestMethod]
        public void WhereCase()
        {
            var list = (from a in Database.Query<ArtistDN>()
                        where a.Dead ? a.Name.Contains("Michael") : a.Name.Contains("Billy")
                        select a).ToArray();
        }

        [TestMethod]
        public void WherePolyExpressionMethod()
        {
            var list = Database.Query<AlbumDN>().Where(a => a.Author.Lonely()).ToArray();
        }


        [TestMethod]
        public void WhereOptimize()
        {
            var list = Database.Query<ArtistDN>().Where(a => a.Dead && true).Select(a => a.Name).ToList();
            list = Database.Query<ArtistDN>().Where(a => a.Dead && false).Select(a => a.Name).ToList();
            list = Database.Query<ArtistDN>().Where(a => a.Dead || true).Select(a => a.Name).ToList();
            list = Database.Query<ArtistDN>().Where(a => a.Dead || false).Select(a => a.Name).ToList();
            list = Database.Query<ArtistDN>().Where(a => a.Dead == true).Select(a => a.Name).ToList();
            list = Database.Query<ArtistDN>().Where(a => a.Dead == false).Select(a => a.Name).ToList();
            list = Database.Query<ArtistDN>().Where(a => a.Dead != true).Select(a => a.Name).ToList();
            list = Database.Query<ArtistDN>().Where(a => a.Dead != false).Select(a => a.Name).ToList();
            list = Database.Query<ArtistDN>().Where(a => true ? a.Dead : false).Select(a => a.Name).ToList();
            list = Database.Query<ArtistDN>().Where(a => false ? false : a.Dead).Select(a => a.Name).ToList();

            list = Database.Query<ArtistDN>().Where(a => true).Select(a => a.Name).ToList();
            list = Database.Query<ArtistDN>().Where(a => !false).Select(a => a.Name).ToList();
            list = Database.Query<ArtistDN>().Where(a => true ? true : false).Select(a => a.Name).ToList();
            list = Database.Query<ArtistDN>().Where(a => false ? false : true).Select(a => a.Name).ToList();
        }

        [TestMethod]
        public void WhereInnerQueryable()
        {
            var females = Database.Query<ArtistDN>().Where(a=>a.Sex == Sex.Female);
            string f = females.ToString();

            var female = Database.Query<ArtistDN>().SingleEx(a=>females.Contains(a));
        }

        [TestMethod]
        public void WhereEmbeddedNull()
        {
            var albumsWithBonusTrack = Database.Query<AlbumDN>().Where(a => a.BonusTrack == null).ToList();
        }

        [TestMethod]
        public void WhereEmbeddedNotNull()
        {
            var albumsWithBonusTrack = Database.Query<AlbumDN>().Where(a => a.BonusTrack != null).ToList();
        }

        [TestMethod]
        public void WhereBindTuple()
        {
            var albums = Database.Query<AlbumDN>().Select(a => Tuple.Create(a.Name, a.Label)).Where(t => t.Item2 == null).ToList(); 
        }

        [TestMethod]
        public void WhereBindBigTuple()
        {
            var albums = Database.Query<AlbumDN>().Select(a => Tuple.Create(a.Name, a.Name, a.Name, a.Name, a.Name, a.Name, a.Name, a.Label)).Where(t => t.Rest.Item1 == null).ToList();
        }

        [TestMethod]
        public void WhereOutsideIs()
        {
            var albums = Database.Query<BandDN>().Where(a => a.LastAward is PersonalAwardDN).ToList();
        }

        [TestMethod]
        public void WhereOutsideCast()
        {
            var albums = Database.Query<BandDN>().Where(a => ((PersonalAwardDN)a.LastAward) != null).ToList();
        }

        [TestMethod]
        public void WhereOutsideEquals()
        {
            var pa = Database.Query<PersonalAwardDN>().FirstEx();

            var albums = Database.Query<BandDN>().Where(a => a.LastAward == pa).ToList();
        }

    }
}
