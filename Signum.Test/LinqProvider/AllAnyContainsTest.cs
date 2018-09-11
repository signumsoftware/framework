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
            MusicStarter.StartAndLoad();
        }

        [TestInitialize]
        public void Initialize()
        {
            Connector.CurrentLogger = new DebugTextWriter();
        }      
     
        [TestMethod]
        public void ContainsIEnumerableId()
        {
            IEnumerable<PrimaryKey> ids = new PrimaryKey[] { 1, 2, 3 }.Select(a => a);

            var artist = Database.Query<ArtistEntity>().Where(a => ids.Contains(a.Id)).ToList();
        }

        [TestMethod]
        public void ContainsArrayId()
        {
            List<PrimaryKey> ids = new List<PrimaryKey> { 1, 2, 3 };

            var artist = Database.Query<ArtistEntity>().Where(a => ids.Contains(a.Id)).ToList();
        }

        [TestMethod]
        public void ContainsListId()
        {
            PrimaryKey[] ids = new PrimaryKey[] { 1, 2, 3 };

            var artist = Database.Query<ArtistEntity>().Where(a => ids.Contains(a.Id)).ToList();
        }

        [TestMethod]
        public void ContainsListLite()
        {
            var artistsInBands = Database.Query<BandEntity>().SelectMany(b => b.Members).Select(a => a.ToLite()).ToList();

            var michael = Database.Query<ArtistEntity>().SingleEx(a => !artistsInBands.Contains(a.ToLite()));
        }

        [TestMethod]
        public void ContainsListEntities()
        {
            var artistsInBands = Database.Query<BandEntity>().SelectMany(b => b.Members).Select(a => a).ToList();

            var michael = Database.Query<ArtistEntity>().SingleEx(a => !artistsInBands.Contains(a));
        }

        [TestMethod]
        public void ContainsListLiteIB()
        {
            var bands = new List<Lite<IAuthorEntity>>
            {
                Lite.Create<ArtistEntity>(5),
                Lite.Create<BandEntity>(1)
            };

            var albums = (from a in Database.Query<AlbumEntity>()
                          where !bands.Contains(a.Author.ToLite())
                          select a.ToLite()).ToList();
        }

        [TestMethod]
        public void ContainsListEntityIB()
        {
            var bands = new List<IAuthorEntity>
            {
                Database.Retrieve<ArtistEntity>(5),
                Database.Retrieve<BandEntity>(1)
            };  

            var albums = (from a in Database.Query<AlbumEntity>()
                          where !bands.Contains(a.Author)
                          select a.ToLite()).ToList();
        }

        [TestMethod]
        public void ContainsListLiteIBA()
        {
            var lites = Database.Query<ArtistEntity>().Where(a => a.Dead).Select(a => a.ToLite<IAuthorEntity>()).ToArray()
                .Concat(Database.Query<BandEntity>().Where(a => a.Name.StartsWith("Smash")).Select(a => a.ToLite<IAuthorEntity>())).ToArray();

            var albums = (from a in Database.Query<NoteWithDateEntity>()
                          where lites.Contains(a.Target.ToLite())
                          select a.ToLite()).ToList();
        }

        [TestMethod]
        public void ContainsListEntityIBA()
        {
            var entities = Database.Query<ArtistEntity>().Where(a => a.Dead).Select(a => (IEntity)a).ToArray()
                .Concat(Database.Query<BandEntity>().Where(a => a.Name.StartsWith("Smash")).Select(a => (IEntity)a)).ToArray();

            var albums = (from a in Database.Query<NoteWithDateEntity>()
                          where entities.Contains(a.Target)
                          select a.ToLite()).ToList();
        }

        [TestMethod]
        public void ContainsEnum()
        {
            var singles = new[] { Status.Single};

            var artists = Database.Query<ArtistEntity>().Where(r => singles.Contains(r.Status.Value)).Select(a => a.ToLite()).ToList();
        }


        [TestMethod]
        public void Any()
        {
            Assert.IsTrue(Database.Query<ArtistEntity>().Any(a => a.Sex == Sex.Female));
        }


        [TestMethod]
        public void None()
        {
            Assert.IsFalse(Database.Query<ArtistEntity>().None(a => a.Sex == Sex.Female));
        }


        [TestMethod]
        public void AnyCollection()
        {
            var years = new[] { 1992, 1993, 1995 };

            var list = Database.Query<AlbumEntity>().Where(a => years.Any(y => a.Year == y)).Select(a => a.Name).ToList();
        }

        [TestMethod]
        public void AnySql()
        {
            BandEntity smashing = Database.Query<BandEntity>().SingleEx(b => b.Members.Any(a => a.Sex == Sex.Female));
        }


        [TestMethod]
        public void NoneSql()
        {
            BandEntity smashing = Database.Query<BandEntity>().SingleEx(b => b.Members.None(a => a.Sex == Sex.Female));
        }

        [TestMethod]
        public void AnySqlNonPredicate()
        {
            var withFriends = Database.Query<ArtistEntity>().Where(b => b.Friends.Any()).Select(a => a.Name).ToList();
        }

        [TestMethod]
        public void All()
        {
            Assert.IsFalse(Database.Query<ArtistEntity>().All(a => a.Sex == Sex.Male));
        }

        [TestMethod]
        public void AllSql()
        {
            BandEntity sigur = Database.Query<BandEntity>().SingleEx(b => b.Members.All(a => a.Sex == Sex.Male));
        }

        [TestMethod]
        public void RetrieveBand()
        {
            BandEntity sigur = Database.Query<BandEntity>().SingleEx(b => b.Name.StartsWith("Sigur"));
        }

        [TestMethod]
        public void ArtistsAny()
        {
            List<Lite<ArtistEntity>> artists = Database.Query<ArtistEntity>().Where(a=>a.Sex == Sex.Male).Select(a => a.ToLite()).ToList();

            var query = Database.Query<ArtistEntity>().Where(a => artists.Any(b => b.Is(a)));
        }
    }
}
