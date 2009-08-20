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
    public class JoinTest
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
        public void Join()
        {
            var songsAlbum = (from a in Database.Query<AlbumDN>()
                              join b in Database.Query<AlbumDN>().SelectMany(a => a.Song) on a.Name equals b.Name
                              select a.Name).ToList();
        }

        [TestMethod]
        public void JoinEntity()
        {
            var songsAlbum = (from a in Database.Query<ArtistDN>()
                              join b in Database.Query<AlbumDN>() on a equals b.Author
                              select new { Artist = a.Name, Album = b.Name }).ToList();
        }


        [TestMethod]
        public void LeftOuterJoinEntity()
        {
            var songsAlbum = (from a in Database.Query<ArtistDN>().DefaultIfEmpty()
                              join b in Database.Query<AlbumDN>() on a equals b.Author
                              select new { Artist = a.Name, Album = b.Name }).ToList();
        }


        [TestMethod]
        public void RightOuterJoinEntity()
        {
            var songsAlbum = (from a in Database.Query<ArtistDN>()
                              join b in Database.Query<AlbumDN>().DefaultIfEmpty() on a equals b.Author
                              select new { Artist = a.Name, Album = b.Name }).ToList();
        }

        [TestMethod]
        public void FullOuterJoinEntity()
        {
            var songsAlbum = (from a in Database.Query<ArtistDN>().DefaultIfEmpty()
                              join b in Database.Query<AlbumDN>().DefaultIfEmpty() on a equals b.Author
                              select new { Artist = a.Name, Album = b.Name }).ToList();
        }

        [TestMethod]
        public void JoinGroup()
        {
            var songsAlbum = (from a in Database.Query<ArtistDN>()
                              join b in Database.Query<AlbumDN>() on a equals b.Author into g
                              select new { a.Name, Albums = (int?)g.Count() }).ToList();
        }

        [TestMethod]
        public void LeftOuterJoinGroup()
        {
            var songsAlbum = (from a in Database.Query<ArtistDN>()
                              join b in Database.Query<AlbumDN>().DefaultIfEmpty() on a equals b.Author into g
                              select new { a.Name, Albums = (int?)g.Count() }).ToList();
        }
    }
}
