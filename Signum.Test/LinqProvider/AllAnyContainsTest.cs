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
    public class AllAnyContainsTest
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
        public void ContainsIEnumerableId()
        {
            IEnumerable<int> ids = new[] { 1, 2, 3 }.Select(a => a);

            var artist = Database.Query<ArtistDN>().Where(a => ids.Contains(a.Id)).ToList();
        }

        [TestMethod]
        public void ContainsArrayId()
        {
            List<int> ids = new List<int>{ 1, 2, 3 };

            var artist = Database.Query<ArtistDN>().Where(a => ids.Contains(a.Id)).ToList();
        }

        [TestMethod]
        public void ContainsListId()
        {
            int[] ids = new[] { 1, 2, 3 };

            var artist = Database.Query<ArtistDN>().Where(a => ids.Contains(a.Id)).ToList();
        }

        [TestMethod]
        public void ContainsListLite()
        {
            var artistsInBands = Database.Query<BandDN>().SelectMany(b => b.Members).Select(a => a.ToLite()).ToList();

            var michael = Database.Query<ArtistDN>().SingleEx(a => !artistsInBands.Contains(a.ToLite()));
        }

        [TestMethod]
        public void ContainsListEntities()
        {
            var artistsInBands = Database.Query<BandDN>().SelectMany(b => b.Members).Select(a => a).ToList();

            var michael = Database.Query<ArtistDN>().SingleEx(a => !artistsInBands.Contains(a));
        }

        [TestMethod]
        public void ContainsListLiteIB()
        {
            var bands = new List<Lite<IAuthorDN>>
            {
                new Lite<IAuthorDN>(typeof(ArtistDN), 5),
                new Lite<IAuthorDN>(typeof(BandDN), 1)
            };

            var albums = (from a in Database.Query<AlbumDN>()
                          where !bands.Contains(a.Author.ToLite())
                          select a.ToLite()).ToList();
        }

        [TestMethod]
        public void ContainsListEntityIB()
        {
            var bands = new List<IAuthorDN>
            {
                Database.Retrieve<ArtistDN>(5),
                Database.Retrieve<BandDN>(1)
            };  

            var albums = (from a in Database.Query<AlbumDN>()
                          where !bands.Contains(a.Author)
                          select a.ToLite()).ToList();
        }

        [TestMethod]
        public void ContainsListLiteIBA()
        {
            var lites = Database.Query<ArtistDN>().Where(a => a.Dead).Select(a => a.ToLite<IIdentifiable>()).ToArray()
                .Concat(Database.Query<BandDN>().Where(a => a.Name.StartsWith("Smash")).Select(a => a.ToLite<IIdentifiable>())).ToArray();

            var albums = (from a in Database.Query<NoteWithDateDN>()
                          where lites.Contains(a.Target.ToLite())
                          select a.ToLite()).ToList();
        }

        [TestMethod]
        public void ContainsListEntityIBA()
        {
            var entities = Database.Query<ArtistDN>().Where(a => a.Dead).Select(a => (IIdentifiable)a).ToArray()
                .Concat(Database.Query<BandDN>().Where(a => a.Name.StartsWith("Smash")).Select(a => (IIdentifiable)a)).ToArray();

            var albums = (from a in Database.Query<NoteWithDateDN>()
                          where entities.Contains(a.Target)
                          select a.ToLite()).ToList();
        }

        [TestMethod]
        public void Any()
        {
            Assert.IsTrue(Database.Query<ArtistDN>().Any(a => a.Sex == Sex.Female));
        }

        [TestMethod]
        public void AnyCollection()
        {
            var years = new[] { 1992, 1993, 1995 };

            var list = Database.Query<AlbumDN>().Where(a => years.Any(y => a.Year == y)).Select(a => a.Name).ToList();
        }

        [TestMethod]
        public void AnySql()
        {
            BandDN smashing = Database.Query<BandDN>().SingleEx(b => b.Members.Any(a => a.Sex == Sex.Female));
        }

        [TestMethod]
        public void AnySqlNonPredicate()
        {
            var withFriends = Database.Query<ArtistDN>().Where(b => b.Friends.Any()).Select(a => a.Name).ToList();
        }

        [TestMethod]
        public void All()
        {
            Assert.IsFalse(Database.Query<ArtistDN>().All(a => a.Sex == Sex.Male));
        }

        [TestMethod]
        public void AllSql()
        {
            BandDN sigur = Database.Query<BandDN>().SingleEx(b => b.Members.All(a => a.Sex == Sex.Male));
        }

        [TestMethod]
        public void RetrieveBand()
        {
            BandDN sigur = Database.Query<BandDN>().SingleEx(b => b.Name.StartsWith("Sigur"));
        }
    }
}
