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
    [TestClass]
    public class TakeSkipTest
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
        public void Take()
        {
            var takeArtist = Database.Query<ArtistDN>().Take(2).ToList();
            Assert.AreEqual(takeArtist.Count, 2);
        }

        [TestMethod]
        public void TakeSql()
        {
            var takeAlbum = Database.Query<AlbumDN>().Select(a => new { a.Name, TwoSongs = a.Song.Take(2) }).ToList();
            Assert.IsTrue(takeAlbum.All(a => a.TwoSongs.Count() <= 2));
        }

        [TestMethod]
        public void Skip()
        {
            var skipArtist = Database.Query<ArtistDN>().Skip(2).ToList();
        }

        [TestMethod]
        public void SkipSql()
        {
            var takeAlbum = Database.Query<AlbumDN>().Select(a => new { a.Name, TwoSongs = a.Song.Skip(2) }).ToList();
        }
    }
}
