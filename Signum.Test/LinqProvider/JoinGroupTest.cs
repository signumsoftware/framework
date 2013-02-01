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
using Signum.Test.Enviroment;

namespace Signum.Test.LinqProvider
{
    /// <summary>
    /// Summary description for LinqProvider
    /// </summary>
    [TestClass]
    public class JoinGroupTest
    {
        [ClassInitialize()]
        public static void MyClassInitialize(TestContext testContext)
        {
            Starter.StartAndLoad();
        }

        [TestInitialize]
        public void Initialize()
        {
            Connector.CurrentLogger = new DebugTextWriter();
        }

        [TestMethod]
        public void Join()
        {
            var songsAlbum = (from a in Database.Query<AlbumDN>()
                              join b in Database.Query<AlbumDN>().SelectMany(a => a.Songs) on a.Name equals b.Name
                              select new { a.Name, Label = a.Label.Name }).ToList();
        }

        [TestMethod]
        public void JoinEntity()
        {
            var songsAlbum = (from a in Database.Query<ArtistDN>()
                              join b in Database.Query<AlbumDN>() on a equals b.Author
                              select new { Artist = a.Name, Album = b.Name }).ToList();
        }


        [TestMethod]
        public void JoinEntityTwice()
        {
            var algums = (from a1 in Database.Query<AlbumDN>()
                          join a2 in Database.Query<AlbumDN>() on a1.Label equals a2.Label
                          join a3 in Database.Query<AlbumDN>() on a2.Label equals a3.Label
                          select new { Name1 = a1.Name, Name2 = a2.Name, Name3 = a3.Name  }).ToList();
        }

        [TestMethod]
        public void JoinerExpansions()
        {
            var labels = Database.Query<AlbumDN>().Join(
                Database.Query<AlbumDN>(), 
                a => a.Year, a => a.Year, 
                (a1, a2) => a1.Label.Name + " " + a2.Label.Name).ToList();
        }


        [TestMethod]
        public void LeftOuterJoinEntity()
        {
            var songsAlbum = (from a in Database.Query<ArtistDN>().DefaultIfEmpty()
                              join b in Database.Query<AlbumDN>() on a equals b.Author
                              select new { Artist = a.Name, Album = b.Name }).ToList();
        }


        [TestMethod]
        public void LeftOuterJoinEntityNotNull()
        {
            var songsAlbum = (from a in Database.Query<ArtistDN>().DefaultIfEmpty()
                              join b in Database.Query<AlbumDN>() on a equals b.Author
                              select new { Artist = a.Name, Album = b.Name, HasArtist = a != null }).ToList();
        }


        [TestMethod]
        public void RightOuterJoinEntity()
        {
            var songsAlbum = (from a in Database.Query<ArtistDN>()
                              join b in Database.Query<AlbumDN>().DefaultIfEmpty() on a equals b.Author
                              select new { Artist = a.Name, Album = b.Name }).ToList();
        }

        [TestMethod]
        public void RightOuterJoinEntityNotNull()
        {
            var songsAlbum = (from a in Database.Query<ArtistDN>()
                              join b in Database.Query<AlbumDN>().DefaultIfEmpty() on a equals b.Author
                              select new { Artist = a.Name, Album = b.Name, HasArtist = b != null }).ToList();
        }

        [TestMethod]
        public void FullOuterJoinEntity()
        {
            var songsAlbum = (from a in Database.Query<ArtistDN>().DefaultIfEmpty()
                              join b in Database.Query<AlbumDN>().DefaultIfEmpty() on a equals b.Author
                              select new { Artist = a.Name, Album = b.Name }).ToList();
        }

        [TestMethod]
        public void FullOuterJoinEntityNotNull()
        {
            var songsAlbum = (from a in Database.Query<ArtistDN>().DefaultIfEmpty()
                              join b in Database.Query<AlbumDN>().DefaultIfEmpty() on a equals b.Author
                              select new { Artist = a.Name, Album = b.Name, HasArtist = a != null, HasAlbum = b != null }).ToList();
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
