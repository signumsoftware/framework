using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signum.Engine;
using Signum.Entities;
using Signum.Utilities;
using Signum.Entities.Basics;

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

        [TestInitialize]
        public void Initialize()
        {
            Connector.CurrentLogger = new DebugTextWriter();
        }

        [TestMethod]
        public void SelectMany()
        {
            var artistsInBands = Database.Query<BandDN>().SelectMany(b => b.Members).Select(a => new { Artist = a.ToLite() }).ToList();
        }


        [TestMethod]
        public void SelectManyIndex()
        {
            var artistsInBands = Database.Query<BandDN>().SelectMany((b, i) => b.Members.Select(m => new { Artist = m.ToLite(), i })).ToList();
        }

        [TestMethod]
        public void SelectMany2()
        {
            var artistsInBands = Database.Query<BandDN>().SelectMany(b => b.Members, (b, a) => new { Artist = a.ToLite(), Band = b.ToLite() }).ToList();
        }

        [TestMethod]
        public void SelectMany2Index()
        {
            var artistsInBands = Database.Query<BandDN>().SelectMany((b, i) => b.Members.Select(m => new { Artist = m.ToLite(), i }), (b, a) => new { a.Artist, a.i, Band = b.ToLite() }).ToList();
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

        [TestMethod]
        public void SelectManyDefaultIfEmpty()
        {
            var artistsInBands = Database.Query<BandDN>().SelectMany(b => b.Members.DefaultIfEmpty()).Select(a => new { Artist = a.ToLite() }).ToList();
        }

        [TestMethod]
        public void SelectManyOverload()
        {
            var artistsInBands = (from a1 in Database.Query<ArtistDN>()
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
            var artistsInBands = (from a1 in Database.Query<ArtistDN>()
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
            var artistsInBands = (from a1 in Database.Query<ArtistDN>()
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
            var artistsInBands = (from b in Database.Query<BandDN>()
                                  from a in b.Members
                                  select new
                                  {
                                      MaxAlbum = Database.Query<NoteDN>()
                                      .Where(n => n.Target.RefersTo(a.LastAward))
                                      .Max(n => (int?)n.Id)
                                  }).ToList();
        }

        [TestMethod]
        public void JoinSingleJoinExpander()
        {
            var artistsInBands = (from b in Database.Query<BandDN>()
                                  join mle in Database.MListQuery((BandDN b) => b.Members) on b equals mle.Parent
                                  select new
                                  {
                                      MaxAlbum = Database.Query<NoteDN>()
                                      .Where(n => n.Target.RefersTo(mle.Element.LastAward))
                                      .Max(n => (int?)n.Id)
                                  }).ToList();
        }

      
    }
}
