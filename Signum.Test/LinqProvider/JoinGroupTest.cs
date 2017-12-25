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
            MusicStarter.StartAndLoad();
        }

        [TestInitialize]
        public void Initialize()
        {
            Connector.CurrentLogger = new DebugTextWriter();
        }

        [TestMethod]
        public void Join()
        {
            var songsAlbum = (from a in Database.Query<AlbumEntity>()
                              join b in Database.Query<AlbumEntity>().SelectMany(a => a.Songs) on a.Name equals b.Name
                              select new { a.Name, Label = a.Label.Name }).ToList();
        }

        [TestMethod]
        public void JoinEntity()
        {
            var songsAlbum = (from a in Database.Query<ArtistEntity>()
                              join b in Database.Query<AlbumEntity>() on a equals b.Author
                              select new { Artist = a.Name, Album = b.Name }).ToList();
        }


        [TestMethod]
        public void JoinEntityTwice()
        {
            var algums = (from a1 in Database.Query<AlbumEntity>()
                          join a2 in Database.Query<AlbumEntity>() on a1.Label equals a2.Label
                          join a3 in Database.Query<AlbumEntity>() on a2.Label equals a3.Label
                          select new { Name1 = a1.Name, Name2 = a2.Name, Name3 = a3.Name  }).ToList();
        }

        [TestMethod]
        public void JoinerExpansions()
        {
            var labels = Database.Query<AlbumEntity>().Join(
                Database.Query<AlbumEntity>(), 
                a => a.Year, a => a.Year, 
                (a1, a2) => a1.Label.Name + " " + a2.Label.Name).ToList();
        }


        [TestMethod]
        public void LeftOuterJoinEntity()
        {
            var songsAlbum = (from a in Database.Query<ArtistEntity>().DefaultIfEmpty()
                              join b in Database.Query<AlbumEntity>() on a equals b.Author
                              select new { Artist = a.Name, Album = b.Name }).ToList();
        }


        [TestMethod]
        public void LeftOuterJoinEntityNotNull()
        {
            var songsAlbum = (from a in Database.Query<ArtistEntity>().DefaultIfEmpty()
                              join b in Database.Query<AlbumEntity>() on a equals b.Author
                              select new { Artist = a.Name, Album = b.Name, HasArtist = a != null }).ToList();
        }


        [TestMethod]
        public void RightOuterJoinEntity()
        {
            var songsAlbum = (from a in Database.Query<ArtistEntity>()
                              join b in Database.Query<AlbumEntity>().DefaultIfEmpty() on a equals b.Author
                              select new { Artist = a.Name, Album = b.Name }).ToList();
        }

        [TestMethod]
        public void RightOuterJoinEntityNotNull()
        {
            var songsAlbum = (from a in Database.Query<ArtistEntity>()
                              join b in Database.Query<AlbumEntity>().DefaultIfEmpty() on a equals b.Author
                              select new { Artist = a.Name, Album = b.Name, HasArtist = b != null }).ToList();
        }

        [TestMethod]
        public void FullOuterJoinEntity()
        {
            var songsAlbum = (from a in Database.Query<ArtistEntity>().DefaultIfEmpty()
                              join b in Database.Query<AlbumEntity>().DefaultIfEmpty() on a equals b.Author
                              select new { Artist = a.Name, Album = b.Name }).ToList();
        }

        [TestMethod]
        public void FullOuterJoinEntityNotNull()
        {
            var songsAlbum = (from a in Database.Query<ArtistEntity>().DefaultIfEmpty()
                              join b in Database.Query<AlbumEntity>().DefaultIfEmpty() on a equals b.Author
                              select new { Artist = a.Name, Album = b.Name, HasArtist = a != null, HasAlbum = b != null }).ToList();
        }

        [TestMethod]
        public void JoinGroup()
        {
            var songsAlbum = (from a in Database.Query<ArtistEntity>()
                              join b in Database.Query<AlbumEntity>() on a equals b.Author into g
                              select new { a.Name, Albums = (int?)g.Count() }).ToList();
        }

        [TestMethod]
        public void LeftOuterJoinGroup()
        {
            var songsAlbum = (from a in Database.Query<ArtistEntity>()
                              join b in Database.Query<AlbumEntity>().DefaultIfEmpty() on a equals b.Author into g
                              select new { a.Name, Albums = (int?)g.Count() }).ToList();
        }

        [TableName("#MyView")]
        class MyTempView : IView
        {
            [ViewPrimaryKey]
            public Lite<ArtistEntity> Artist { get; set; }
        }

        [TestMethod]
        public void LeftOuterMyView()
        {
            using (Transaction tr = new Transaction())
            {
                Administrator.CreateTemporaryTable<MyTempView>();

                Database.Query<ArtistEntity>().Where(a => a.Name.StartsWith("M")).UnsafeInsertView(a => new MyTempView { Artist = a.ToLite() });
                
                var artists = (from a in Database.Query<ArtistEntity>()
                             join b in Database.View<MyTempView>() on a.ToLite() equals b.Artist into g
                             select a.ToLite()).ToList();

                Assert.IsTrue(artists.All(a => a.ToString().StartsWith("M")));

                var list1 = Database.View<MyTempView>().ToList();
                var list2 = Database.Query<ArtistEntity>().Where(a => a.Name.StartsWith("M")).ToList();
                Assert.AreEqual(list1.Count, list2.Count);

                tr.Commit();
            }

        }
    }
}
