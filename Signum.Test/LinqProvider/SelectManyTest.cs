using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signum.Engine;
using Signum.Entities;
using Signum.Utilities;
using Signum.Entities.Basics;
using Signum.Test.Environment;

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
            MusicStarter.StartAndLoad();
        }

        [TestInitialize]
        public void Initialize()
        {
            Connector.CurrentLogger = new DebugTextWriter();
        }

        [TestMethod]
        public void SelectMany()
        {
            var artistsInBands = Database.Query<BandEntity>().SelectMany(b => b.Members).Select(a => new { Artist = a.ToLite() }).ToList();
        }


        [TestMethod]
        public void SelectManyIndex()
        {
            var artistsInBands = Database.Query<BandEntity>().SelectMany((b, i) => b.Members.Select(m => new { Artist = m.ToLite(), i })).ToList();
        }

        [TestMethod]
        public void SelectMany2()
        {
            var artistsInBands = Database.Query<BandEntity>().SelectMany(b => b.Members, (b, a) => new { Artist = a.ToLite(), Band = b.ToLite() }).ToList();
        }

        [TestMethod]
        public void SelectMany2Index()
        {
            var artistsInBands = Database.Query<BandEntity>().SelectMany((b, i) => b.Members.Select(m => new { Artist = m.ToLite(), i }), (b, a) => new { a.Artist, a.i, Band = b.ToLite() }).ToList();
        }

        [TestMethod]
        public void SelectManyWhere1()
        {
            var artistsInBands = Database.Query<BandEntity>().SelectMany(b => b.Members.Where(a => a.IsMale)).Select(a => new { Artist = a.ToLite() }).ToList();
        }

        [TestMethod]
        public void SelectManyWhere2()
        {
            var artistsInBands = Database.Query<BandEntity>().Where(b => b.LastAward != null).SelectMany(b => b.Members.Where(a => a.IsMale)).Select(a => a.ToLite()).ToList();
        }

        [TestMethod]
        public void SelectManyEmbedded()
        {
            var artistsInBands = Database.Query<AlbumEntity>().SelectMany(a => a.Songs, (a, s) => s.Name).ToList();
        }

        [TestMethod]
        public void SelectManyLazy()
        {
            var artistsInBands = Database.Query<ArtistEntity>().SelectMany(a=>a.Friends).ToList();
        }

        [TestMethod]
        public void SelectManyDefaultIfEmpty()
        {
            var artistsInBands = Database.Query<BandEntity>().SelectMany(b => b.Members.DefaultIfEmpty()).Select(a => new { Artist = a.ToLite() }).ToList();
        }

        [TestMethod]
        public void SelectManyOverload()
        {
            var artistsInBands = (from a1 in Database.Query<ArtistEntity>()
                                  from a in a1.Friends
                                  select new
                                  {
                                      Artist = a1.ToLite(),
                                      Friend = a,
                                  }).ToList();
        }


        [TestMethod]
        public void SelectManyDefaultIfEmptyTwo()
        {
            var artistsInBands = (from a1 in Database.Query<ArtistEntity>()
                                  from a in a1.Friends.DefaultIfEmpty()
                                  select new
                                  {
                                      Artist = a1.ToLite(),
                                      Friend = a,
                                  }).ToList();
        }

        [TestMethod]
        public void SelectManyDefaultIfEmptyNotNull()
        {
            var artistsInBands = (from a1 in Database.Query<ArtistEntity>()
                                  from a in a1.Friends.DefaultIfEmpty()
                                  select new
                                  {
                                      Artist = a1.ToLite(),
                                      Friend = a,
                                      HasFriend = a != null
                                  }).ToList();
        }


        [TestMethod]
        public void SelectManySingleJoinExpander()
        {
            var artistsInBands = (from b in Database.Query<BandEntity>()
                                  from a in b.Members
                                  select new
                                  {
                                      MaxAlbum = Database.Query<ArtistEntity>()
                                      .Where(n => n.Friends.Contains(a.ToLite()))
                                      .Max(n => (int?)n.Id)
                                  }).ToList();
        }

        [TestMethod]
        public void JoinSingleJoinExpander()
        {
            var artistsInBands = (from b in Database.Query<BandEntity>()
                                  join mle in Database.MListQuery((BandEntity b) => b.Members) on b equals mle.Parent
                                  select new
                                  {
                                      MaxAlbum = Database.Query<ArtistEntity>()
                                      .Where(n => n.Friends.Contains(mle.Element.ToLite()))
                                      .Max(n => (int?)n.Id)
                                  }).ToList();
        }

      
    }
}
