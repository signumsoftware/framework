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
using Signum.Engine.Exceptions;

namespace Signum.Test.LinqProvider
{
    /// <summary>
    /// Summary description for LinqProvider
    /// </summary>
    [TestClass]
    public class OrderByTest
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
        public void OrderByString()
        {
            var songsAlbum = Database.Query<AlbumDN>().Select(a => a.Name).OrderBy(n => n).ToList();
        }

        [TestMethod]
        public void OrderByIntDescending()
        {
            var songsAlbum = Database.Query<AlbumDN>().OrderByDescending(a => a.Year).ToList();
        }

        [TestMethod]
        public void OrderByThenBy()
        {
            var songsAlbum = Database.Query<ArtistDN>().OrderBy(a => a.Dead).ThenBy(a => a.Sex).ToList();
        }

        [TestMethod]
        public void OrderByFirst()
        {
            var songsAlbum = Database.Query<ArtistDN>().OrderBy(a => a.Dead).First();
        }

        [TestMethod]
        public void OrderByTop()
        {
            var songsAlbum = Database.Query<ArtistDN>().OrderBy(a => a.Dead).Take(3);
        }

        [TestMethod]
        public void OrderByNotLast()
        {
            var songsAlbum = Database.Query<ArtistDN>().OrderBy(a => a.Dead).Where(a => a.Id != 0).ToList();
        }

        [TestMethod]
        public void OrderByDistinct()
        {
            var songsAlbum = Database.Query<ArtistDN>().OrderBy(a => a.Dead).Distinct().ToList();
        }

        [TestMethod]
        public void OrderByGroupBy()
        {
            var songsAlbum = Database.Query<ArtistDN>().OrderBy(a => a.Dead).GroupBy(a => a.Sex, (s, gr) => new { Sex = s, Count = gr.Count() }).ToList();
        }

    }
}
