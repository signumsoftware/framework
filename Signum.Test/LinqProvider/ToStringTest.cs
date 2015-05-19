using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signum.Engine;
using Signum.Utilities;
using Signum.Entities;
using Signum.Utilities.ExpressionTrees;
using Signum.Test.Environment;

namespace Signum.Test.LinqProvider
{
    [TestClass]
    public class ToStringTest
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
        public void ToStringMainQuery()
        {
            Assert.AreEqual(
                Database.Query<ArtistEntity>().Select(a => a.Name).ToString(" | "),
                Database.Query<ArtistEntity>().ToString(a => a.Name, " | "));
        }

        [TestMethod]
        public void ToStringEntity()
        {
            Assert.AreEqual(
                Database.Query<ArtistEntity>().Select(a => a.Name).ToString(" | "),
                Database.Query<ArtistEntity>().ToString(" | "));
        }


        [TestMethod]
        public void ToStringSubCollection()
        {
            var result1 = (from b in Database.Query<BandEntity>()
                           orderby b.Name
                           select new
                           {
                               b.Name,
                               MembersToString = b.Members.OrderBy(a => a.Name).ToString(a => a.Name, " | "),
                           }).ToList();

            var result2 = (from b in Database.Query<BandEntity>()
                           orderby b.Name
                           select new
                           {
                               b.Name,
                               MembersToString = b.Members.OrderBy(a => a.Name).Select(a => a.Name).ToList().ToString(" | "),
                           }).ToList();

            Assert.IsTrue(Enumerable.SequenceEqual(result1, result2));

        }

        [TestMethod]
        public void ToStringSubQuery()
        {
            var result1 = (from b in Database.Query<BandEntity>()
                           orderby b.Name
                           select new
                           {
                               b.Name,
                               MembersToString = Database.Query<AlbumEntity>().Where(a => a.Author == b).OrderBy(a => a.Name).ToString(a => a.Name, " | "),
                           }).ToList();

            var result2 = (from b in Database.Query<BandEntity>()
                           orderby b.Name
                           select new
                           {
                               b.Name,
                               MembersToString = Database.Query<AlbumEntity>().Where(a => a.Author == b).OrderBy(a => a.Name).Select(a => a.Name).ToList().ToString(" | "),
                           }).ToList();

            Assert.IsTrue(Enumerable.SequenceEqual(result1, result2));
        }


        [TestMethod]
        public void ToStringNumbers()
        {
            var result1 = (from b in Database.Query<BandEntity>()
                           orderby b.Name
                           select new
                           {
                               b.Name,
                               MembersToString = Database.Query<AlbumEntity>().Where(a => a.Author == b).OrderBy(a => a.Name).ToString(a => a.Id.ToString(), " | "),
                           }).ToList();

            var result2 = (from b in Database.Query<BandEntity>()
                           orderby b.Name
                           select new
                           {
                               b.Name,
                               MembersToString = Database.Query<AlbumEntity>().Where(a => a.Author == b).OrderBy(a => a.Name).Select(a => a.Id).ToString(" | "),
                           }).ToList();

            Func<List<PrimaryKey>, string> toString = list => list.ToString(" | ");

            var result3 = (from b in Database.Query<BandEntity>()
                           orderby b.Name
                           select new
                           {
                               b.Name,
                               MembersToString = toString(Database.Query<AlbumEntity>().Where(a => a.Author == b).OrderBy(a => a.Name).Select(a => a.Id).ToList()),
                           }).ToList();


            Assert.IsTrue(Enumerable.SequenceEqual(result1, result2));
            Assert.IsTrue(Enumerable.SequenceEqual(result2, result3));

        }

    }
}
