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
            Connection.CurrentLog = new DebugTextWriter();
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
        public void ContainsListLazy()
        {
            var artistsInBands = Database.Query<BandDN>().SelectMany(b => b.Members).Select(a => a.ToLazy()).ToList();

            var michael = Database.Query<ArtistDN>().Single(a => !artistsInBands.Contains(a.ToLazy()));
        }

        [TestMethod]
        public void ContainsListEntities()
        {
            var artistsInBands = Database.Query<BandDN>().SelectMany(b => b.Members).Select(a => a).ToList();

            var michael = Database.Query<ArtistDN>().Single(a => !artistsInBands.Contains(a));
        }

        [TestMethod]
        public void ContainsListLazyIB()
        {
            var bands = new List<Lazy<IAuthorDN>>
            {
                new Lazy<IAuthorDN>(typeof(ArtistDN), 5),
                new Lazy<IAuthorDN>(typeof(BandDN), 1)
            };

            var albums = (from a in Database.Query<AlbumDN>()
                          where !bands.Contains(a.Author.ToLazy())
                          select a.ToLazy()).ToList();
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
                          select a.ToLazy()).ToList();
        }

        [TestMethod]
        public void ContainsListLazyIBA()
        {
            var lazies = Database.Query<ArtistDN>().Where(a => a.Dead).Select(a => a.ToLazy<IIdentifiable>()).ToArray()
                .Concat(Database.Query<BandDN>().Where(a => a.Name.StartsWith("Smash")).Select(a => a.ToLazy<IIdentifiable>())).ToArray();

            var albums = (from a in Database.Query<NoteDN>()
                          where lazies.Contains(a.Target.ToLazy())
                          select a.ToLazy()).ToList();
        }

        [TestMethod]
        public void ContainsListEntityIBA()
        {
            var entities = Database.Query<ArtistDN>().Where(a => a.Dead).Select(a => (IIdentifiable)a).ToArray()
                .Concat(Database.Query<BandDN>().Where(a => a.Name.StartsWith("Smash")).Select(a => (IIdentifiable)a)).ToArray();

            var albums = (from a in Database.Query<NoteDN>()
                          where entities.Contains(a.Target)
                          select a.ToLazy()).ToList();
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
            BandDN smashing = Database.Query<BandDN>().Single(b => b.Members.Any(a => a.Sex == Sex.Female));
        }

        [TestMethod]
        public void All()
        {
            Assert.IsFalse(Database.Query<ArtistDN>().All(a => a.Sex == Sex.Male));
        }

        [TestMethod]
        public void AllSql()
        {
            BandDN sigur = Database.Query<BandDN>().Single(b => b.Members.All(a => a.Sex == Sex.Male));
        }
    }
}
