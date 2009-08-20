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
    public class GroupByTest
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
        public void GroupStringByEnum()
        {
            var songsAlbum = Database.Query<ArtistDN>().GroupBy(a => a.Sex, a => a.Name).ToList(); 
        }

        [TestMethod]
        public void GroupEntityByEnum()
        {
            var songsAlbum = Database.Query<ArtistDN>().GroupBy(a => a.Sex).ToList();
        }

        [TestMethod]
        public void GroupWhere()
        {
            var songsAlbum = Database.Query<ArtistDN>().Where(a=>a.Dead).GroupBy(a => a.Sex).ToList();
        }

        [TestMethod]
        public void GroupWhere2()
        {
            var songsAlbum = (from a in Database.Query<ArtistDN>()
                              group a by a.Sex into g
                              select new { Sex = g.Key, DeadArtists = g.Where(a => a.Dead).ToList() }).ToList();        
        }

        [TestMethod]
        public void GroupCount()
        {
            var songsAlbum = (from a in Database.Query<ArtistDN>()
                              group a by a.Sex into g
                              select new { Sex = g.Key, Count = g.Count() }).ToList();
        }

        [TestMethod]
        public void GroupMax()
        {
            var songsAlbum = (from a in Database.Query<ArtistDN>()
                              group a by a.Sex into g
                              select new { Sex = g.Key, Count = g.Max(a => a.Name.Length) }).ToList();
        }

        [TestMethod]
        public void GroupMin()
        {
            var songsAlbum = (from a in Database.Query<ArtistDN>()
                              group a by a.Sex into g
                              select new { Sex = g.Key, Count = g.Min(a => a.Name.Length) }).ToList();
        }

        [TestMethod]
        public void GroupAverage()
        {
            var songsAlbum = (from a in Database.Query<ArtistDN>()
                              group a by a.Sex into g
                              select new { Sex = g.Key, Count = g.Average(a=>a.Name.Length) }).ToList();
        }
    }
}
