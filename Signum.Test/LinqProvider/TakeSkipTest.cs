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
using Signum.Utilities.ExpressionTrees;

namespace Signum.Test.LinqProvider
{
    [TestClass]
    public class TakeSkipTest
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
        public void Take()
        {
            var takeArtist = Database.Query<ArtistEntity>().Take(2).ToList();
            Assert.AreEqual(takeArtist.Count, 2);
        }

        [TestMethod]
        public void TakeOrder()
        {
            var takeArtist = Database.Query<ArtistEntity>().OrderBy(a => a.Name).Take(2).ToList();
            Assert.AreEqual(takeArtist.Count, 2);
        }

        [TestMethod]
        public void TakeSql()
        {
            var takeAlbum = Database.Query<AlbumEntity>().Select(a => new { a.Name, TwoSongs = a.Songs.Take(2) }).ToList();
            Assert.IsTrue(takeAlbum.All(a => a.TwoSongs.Count() <= 2));
        }

        [TestMethod]
        public void Skip()
        {
            var skipArtist = Database.Query<ArtistEntity>().Skip(2).ToList();
        }


        [TestMethod]
        public void SkipAllAggregates()
        {
            var allAggregates = Database.Query<ArtistEntity>().GroupBy(a => new { }).Select(gr => new { Count = gr.Count(), MaxId = gr.Max(a=>a.Id) }).Skip(2).ToList();

        }

        [TestMethod]
        public void AllAggregatesOrderByAndByKeys()
        {
            var allAggregates = Database.Query<ArtistEntity>().GroupBy(a => new { }).Select(gr => new { Count = gr.Count(), MaxId = gr.Max(a => a.Id) }).OrderBy(a => a.Count).OrderAlsoByKeys().ToList();
        }

        [TestMethod]
        public void SkipAllAggregatesOrderBy()
        {
            var allAggregates = Database.Query<ArtistEntity>().GroupBy(a => new { }).Select(gr => new { Count = gr.Count(), MaxId = gr.Max(a => a.Id) }).OrderBy(a=>a.Count).Skip(2).ToList();
        }

        [TestMethod]
        public void AllAggregatesCount()
        {
            var count = Database.Query<ArtistEntity>().GroupBy(a => new { }).Select(gr => new { Count = gr.Count(), MaxId = gr.Max(a => a.Id) }).OrderBy(a => a.Count).Count();
            Assert.AreEqual(1, count);
        }

        [TestMethod]
        public void SkipOrder()
        {
            var skipArtist = Database.Query<ArtistEntity>().OrderBy(a => a.Name).Skip(2).ToList();
        }

        [TestMethod]
        public void SkipSql()
        {
            var takeAlbum = Database.Query<AlbumEntity>().Select(a => new { a.Name, TwoSongs = a.Songs.Skip(2) }).ToList();
        }

        [TestMethod]
        public void SkipTake()
        {
            var skipArtist = Database.Query<ArtistEntity>().Skip(2).Take(1).ToList();
        }

        [TestMethod]
        public void SkipTakeOrder()
        {
            var skipArtist = Database.Query<ArtistEntity>().OrderBy(a => a.Name).Skip(2).Take(1).ToList();
        }

        [TestMethod]
        public void InnerTake()
        {
            var result = Database.Query<AlbumEntity>().Where(dr => dr.Songs.OrderByDescending(a => a.Seconds).Take(1).Where(a => a.Name.Contains("1976")).Any()).Select(a => a.ToLite()).ToList();
            Assert.AreEqual(0, result.Count); 
        }

        [TestMethod]
        public void OrderByCommonSelectPaginate()
        {
            TestPaginate(Database.Query<ArtistEntity>().OrderBy(a => a.Sex).Select(a => a.Name));
        }

        [TestMethod]
        public void OrderBySelectPaginate()
        {
            TestPaginate(Database.Query<ArtistEntity>().OrderBy(a => a.Name).Select(a => a.Name));
        }

        [TestMethod]
        public void OrderByDescendingSelectPaginate()
        {
            TestPaginate(Database.Query<ArtistEntity>().OrderByDescending(a => a.Name).Select(a => a.Name));
        }

        [TestMethod]
        public void OrderByThenBySelectPaginate()
        {
            TestPaginate(Database.Query<ArtistEntity>().OrderBy(a => a.Name).ThenBy(a => a.Id).Select(a => a.Name));
        }

        [TestMethod]
        public void SelectOrderByPaginate()
        {
            TestPaginate(Database.Query<ArtistEntity>().Select(a => a.Name).OrderBy(a => a));
        }

        [TestMethod]
        public void SelectOrderByDescendingPaginate()
        {
            TestPaginate(Database.Query<ArtistEntity>().Select(a => a.Name).OrderByDescending(a => a));
        }

        private void TestPaginate<T>(IQueryable<T> query)
        {
            var list = query.ToList();

            int pageSize = 2;

            var list2 = 0.To(((list.Count / pageSize) + 1)).SelectMany(page =>
                query.OrderAlsoByKeys().Skip(pageSize * page).Take(pageSize).ToList()).ToList();

            Assert.IsTrue(list.SequenceEqual(list2)); 
        }
    }
}
