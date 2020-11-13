using System.Linq;
using Xunit;
using Signum.Engine;
using Signum.Entities;
using Signum.Utilities;
using Signum.Test.Environment;

namespace Signum.Test.LinqProvider
{
    /// <summary>
    /// Summary description for SelectManyTest
    /// </summary>
    public class SelectManyTest
    {
        public SelectManyTest()
        {
            MusicStarter.StartAndLoad();
            Connector.CurrentLogger = new DebugTextWriter();
        }

        [Fact]
        public void SelectMany()
        {
            var artistsInBands = Database.Query<BandEntity>().SelectMany(b => b.Members).Select(a => new { Artist = a.ToLite() }).ToList();
        }


        [Fact]
        public void SelectManyIndex()
        {
            var artistsInBands = Database.Query<BandEntity>().SelectMany((b, i) => b.Members.Select(m => new { Artist = m.ToLite(), i })).ToList();
        }

        [Fact]
        public void SelectMany2()
        {
            var artistsInBands = Database.Query<BandEntity>().SelectMany(b => b.Members, (b, a) => new { Artist = a.ToLite(), Band = b.ToLite() }).ToList();
        }

        [Fact]
        public void SelectMany2Index()
        {
            var artistsInBands = Database.Query<BandEntity>().SelectMany((b, i) => b.Members.Select(m => new { Artist = m.ToLite(), i }), (b, a) => new { a.Artist, a.i, Band = b.ToLite() }).ToList();
        }

        [Fact]
        public void SelectManyWhere1()
        {
            var artistsInBands = Database.Query<BandEntity>().SelectMany(b => b.Members.Where(a => a.IsMale)).Select(a => new { Artist = a.ToLite() }).ToList();
        }

        [Fact]
        public void SelectManyWhere2()
        {
            var artistsInBands = Database.Query<BandEntity>().Where(b => b.LastAward != null).SelectMany(b => b.Members.Where(a => a.IsMale)).Select(a => a.ToLite()).ToList();
        }

        [Fact]
        public void SelectManyEmbedded()
        {
            var artistsInBands = Database.Query<AlbumEntity>().SelectMany(a => a.Songs, (a, s) => s.Name).ToList();
        }

        [Fact]
        public void SelectManyLazy()
        {
            var artistsInBands = Database.Query<ArtistEntity>().SelectMany(a=>a.Friends).ToList();
        }

        [Fact]
        public void SelectManyDefaultIfEmpty()
        {
            var artistsInBands = Database.Query<BandEntity>().SelectMany(b => b.Members.DefaultIfEmpty()).Select(a => new { Artist = a!.ToLite() }).ToList();
        }

        [Fact]
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


        [Fact]
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

        [Fact]
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


        [Fact]
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

        [Fact]
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
