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
            Connection.CurrentLog = new DebugTextWriter();
        }      
     
        [TestMethod]
        public void Where()
        {
            var list = Database.Query<AlbumDN>().Where(a => a.Year < 1995).ToList();
        }

        [TestMethod]
        public void WhereSelect()
        {
            var list = Database.Query<AlbumDN>().Where(a => a.Year < 1995).Select(a => new { a.Year, Author = a.Author.ToLazy(), a.Name }).ToList();
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

            Assert2.Throws<InvalidOperationException>(() => artists.Single(a => a.Dead && !a.Dead));
            Assert.IsNotNull(artists.Single(a => a.Dead)); //michael
            Assert2.Throws<InvalidOperationException>(() => artists.Single(a => a.Sex == Sex.Male));

            Assert.IsNull(artists.SingleOrDefault(a => a.Dead && !a.Dead));
            Assert.IsNotNull(artists.SingleOrDefault(a => a.Dead)); //michael
            Assert2.Throws<InvalidOperationException>(() => artists.SingleOrDefault(a => a.Sex == Sex.Male));

            Assert2.Throws<InvalidOperationException>(() => artists.First(a => a.Dead && !a.Dead));
            Assert.IsNotNull(artists.First(a => a.Dead)); //michael
            Assert.IsNotNull(artists.First(a => a.Sex == Sex.Male));

            Assert.IsNull(artists.FirstOrDefault(a => a.Dead && !a.Dead));
            Assert.IsNotNull(artists.FirstOrDefault(a => a.Dead)); //michael
            Assert.IsNotNull(artists.FirstOrDefault(a => a.Sex == Sex.Male));
        }


        [TestMethod]
        public void WhereEntityEquals()
        {
            ArtistDN wretzky = Database.Query<ArtistDN>().Single(a => a.Sex == Sex.Female);

            BandDN smashing = (from b in Database.Query<BandDN>()
                               from a in b.Members
                               where a == wretzky
                               select b).Single(); 
        }


        [TestMethod]
        public void WhereLazyEquals()
        {
            ArtistDN wretzky = Database.Query<ArtistDN>().Single(a => a.Sex == Sex.Female);

            BandDN smashing = (from b in Database.Query<BandDN>()
                               from a in b.Members
                               where a.ToLazy() == wretzky.ToLazy()
                               select b).Single();
        }


        [TestMethod]
        public void WhereEntityEqualsIB()
        {
            ArtistDN michael = Database.Query<ArtistDN>().Single(a => a.Dead);

            var albums = (from a in Database.Query<AlbumDN>()
                          where a.Author == michael
                          select a.ToLazy()).ToList();
        }

        [TestMethod]
        public void WhereEntityEqualsIBA()
        {
            ArtistDN michael = Database.Query<ArtistDN>().Single(a => a.Dead);

            var albums = (from n in Database.Query<NoteDN>()
                          where n.Target == michael
                          select n.ToLazy()).ToList();
        }

        [TestMethod]
        public void WhereLazyEqualsIB()
        {
            ArtistDN michael = Database.Query<ArtistDN>().Single(a => a.Dead);

            var albums = (from a in Database.Query<AlbumDN>()
                          where a.Author.ToLazy() == michael.ToLazy<IAuthorDN>()
                          select a.ToLazy()).ToList();
        }

        [TestMethod]
        public void WhereLazyEqualsIBA()
        {
            ArtistDN michael = Database.Query<ArtistDN>().Single(a => a.Dead);

            var albums = (from n in Database.Query<NoteDN>()
                          where n.Target.ToLazy() == michael.ToLazy<IIdentifiable>()
                          select n.ToLazy()).ToList();
        }

        [TestMethod]
        public void WhereIs()
        {
            var albums = (from a in Database.Query<AlbumDN>()
                          where a.Author is ArtistDN
                          select a.ToLazy()).ToList();
        }
    }
}
