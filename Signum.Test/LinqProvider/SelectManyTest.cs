using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signum.Engine;
using Signum.Entities;

namespace Signum.Test.LinqProvider
{
    /// <summary>
    /// Summary description for SelectManyTest
    /// </summary>
    [TestClass]
    public class SelectManyTest
    {
        [ClassInitialize()]
        public static void MyClassInitialize(TestContext testContext)
        {
            Starter.StartAndLoad();
        }

        [TestMethod]
        public void SelectMany()
        {
            var artistsInBands = Database.Query<BandDN>().SelectMany(b => b.Members).Select(a => new { Artist = a.ToLite() }).ToList();
        }

        [TestMethod]
        public void SelectMany2()
        {
            var artistsInBands = Database.Query<BandDN>().SelectMany(b => b.Members, (b, a) => new { Artist = a.ToLite() }).ToList();
        }

        [TestMethod]
        public void SelectManyWhere1()
        {
            var artistsInBands = Database.Query<BandDN>().SelectMany(b => b.Members.Where(a => a.IsMale)).Select(a => new { Artist = a.ToLite() }).ToList();
        }

        [TestMethod]
        public void SelectManyWhere2()
        {
            var artistsInBands = Database.Query<BandDN>().Where(b => b.LastAward != null).SelectMany(b => b.Members.Where(a => a.IsMale)).Select(a => a.ToLite()).ToList();
        }

        [TestMethod]
        public void SelectManyEmbedded()
        {
            var artistsInBands = Database.Query<AlbumDN>().SelectMany(a => a.Songs, (a, s) => s.Name).ToList();
        }

        [TestMethod]
        public void SelectManyLazy()
        {
            var artistsInBands = Database.Query<ArtistDN>().SelectMany(a=>a.Friends).ToList();
        }
    }
}
