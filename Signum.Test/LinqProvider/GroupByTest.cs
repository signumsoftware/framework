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
            var list = Database.Query<ArtistDN>().GroupBy(a => a.Sex, a => a.Name).ToList(); 
        }

        [TestMethod]
        public void GroupEntityByEnum()
        {
            var list = Database.Query<ArtistDN>().GroupBy(a => a.Sex).ToList();
        }

        [TestMethod]
        public void WhereGroup()
        {
            var list = Database.Query<ArtistDN>().Where(a=>a.Dead).GroupBy(a => a.Sex).ToList();
        }

        [TestMethod]
        public void GroupWhere()
        {
            var list = (from a in Database.Query<ArtistDN>()
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
        public void GroupWhereCount()
        {
            var songsAlbum = (from a in Database.Query<ArtistDN>()
                              group a by a.Sex into g
                              select new { Sex = g.Key, DeadArtists = (int?)g.Count(a => a.Dead) }).ToList();
        }

        [TestMethod]
        public void GroupMax()
        {
            var songsAlbum = (from a in Database.Query<ArtistDN>()
                              group a by a.Sex into g
                              select new { Sex = g.Key, Max = g.Max(a => a.Name.Length) }).ToList();
        }

        [TestMethod]
        public void GroupMin()
        {
            var songsAlbum = (from a in Database.Query<ArtistDN>()
                              group a by a.Sex into g
                              select new { Sex = g.Key, Min = g.Min(a => a.Name.Length) }).ToList();
        }

        [TestMethod]
        public void GroupAverage()
        {
            var songsAlbum = (from a in Database.Query<ArtistDN>()
                              group a by a.Sex into g
                              select new { Sex = g.Key, Avg = g.Average(a=>a.Name.Length) }).ToList();
        }


        [TestMethod]
        public void RootCount()
        {
            var songsAlbum = Database.Query<ArtistDN>().Count();
        }

        [TestMethod]
        public void RootCountWhere()
        {
            var songsAlbum = Database.Query<ArtistDN>().Count(a => a.Name.StartsWith("M"));
        }

        [TestMethod]
        public void RootMax()
        {
            var songsAlbum = Database.Query<ArtistDN>().Max(a => a.Name.Length);
        }

        [TestMethod]
        public void RootMin()
        {
            var songsAlbum = Database.Query<ArtistDN>().Min(a => a.Name.Length);
        }

        [TestMethod]
        public void RootAverage()
        {
            var songsAlbum = Database.Query<ArtistDN>().Average(a => a.Name.Length);
        }

        [TestMethod]
        public void GroupBySelectSelect()
        {
            var artistsBySex =
                Database.Query<ArtistDN>()
                .GroupBy(a=>a.Sex)
                .Select(g=>g)
                .Select(g=>new { Sex = g.Key, Count = g.Count() }).ToList();


        }
    }
}
