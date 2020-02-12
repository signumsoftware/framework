using System.Linq;
using Xunit;
using Signum.Engine;
using Signum.Entities;
using Signum.Utilities;
using Signum.Test.Environment;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Test.LinqProvider
{
    public class TakeSkipTest
    {
        public TakeSkipTest()
        {
            MusicStarter.StartAndLoad();
            Connector.CurrentLogger = new DebugTextWriter();
        }

        [Fact]
        public void Take()
        {
            var takeArtist = Database.Query<ArtistEntity>().Take(2).ToList();
            Assert.Equal(2, takeArtist.Count);
        }

        [Fact]
        public void TakeOrder()
        {
            var takeArtist = Database.Query<ArtistEntity>().OrderBy(a => a.Name).Take(2).ToList();
            Assert.Equal(2, takeArtist.Count);
        }

        [Fact]
        public void TakeSql()
        {
            var takeAlbum = Database.Query<AlbumEntity>().Select(a => new { a.Name, TwoSongs = a.Songs.Take(2) }).ToList();
            Assert.True(takeAlbum.All(a => a.TwoSongs.Count() <= 2));
        }

        [Fact]
        public void Skip()
        {
            var skipArtist = Database.Query<ArtistEntity>().Skip(2).ToList();
        }


        [Fact]
        public void SkipAllAggregates()
        {
            var allAggregates = Database.Query<ArtistEntity>().GroupBy(a => new { }).Select(gr => new { Count = gr.Count(), MaxId = gr.Max(a=>a.Id) }).Skip(2).ToList();

        }

        [Fact]
        public void AllAggregatesOrderByAndByKeys()
        {
            var allAggregates = Database.Query<ArtistEntity>().GroupBy(a => new { }).Select(gr => new { Count = gr.Count(), MaxId = gr.Max(a => a.Id) }).OrderBy(a => a.Count).OrderAlsoByKeys().ToList();
        }

        [Fact]
        public void SkipAllAggregatesOrderBy()
        {
            var allAggregates = Database.Query<ArtistEntity>().GroupBy(a => new { }).Select(gr => new { Count = gr.Count(), MaxId = gr.Max(a => a.Id) }).OrderBy(a=>a.Count).Skip(2).ToList();
        }

        [Fact]
        public void AllAggregatesCount()
        {
            var count = Database.Query<ArtistEntity>().GroupBy(a => new { }).Select(gr => new { Count = gr.Count(), MaxId = gr.Max(a => a.Id) }).OrderBy(a => a.Count).Count();
            Assert.Equal(1, count);
        }

        [Fact]
        public void SkipOrder()
        {
            var skipArtist = Database.Query<ArtistEntity>().OrderBy(a => a.Name).Skip(2).ToList();
        }

        [Fact]
        public void SkipSql()
        {
            var takeAlbum = Database.Query<AlbumEntity>().Select(a => new { a.Name, TwoSongs = a.Songs.Skip(2) }).ToList();
        }

        [Fact]
        public void SkipTake()
        {
            var skipArtist = Database.Query<ArtistEntity>().Skip(2).Take(1).ToList();
        }

        [Fact]
        public void SkipTakeOrder()
        {
            var skipArtist = Database.Query<ArtistEntity>().OrderBy(a => a.Name).Skip(2).Take(1).ToList();
        }

        [Fact]
        public void InnerTake()
        {
            var result = Database.Query<AlbumEntity>()
                .Where(dr => dr.Songs.OrderByDescending(a => a.Seconds).Take(1).Where(a => a.Name.Contains("Zero")).Any())
                .Select(a => a.ToLite())
                .ToList();

            Assert.Empty(result);
        }

        [Fact]
        public void OrderByCommonSelectPaginate()
        {
            TestPaginate(Database.Query<ArtistEntity>().OrderBy(a => a.Sex).Select(a => a.Name));
        }

        [Fact]
        public void OrderBySelectPaginate()
        {
            TestPaginate(Database.Query<ArtistEntity>().OrderBy(a => a.Name).Select(a => a.Name));
        }
        
        [Fact]
        public void OrderByDescendingSelectPaginate()
        {
            TestPaginate(Database.Query<ArtistEntity>().OrderByDescending(a => a.Name).Select(a => a.Name));
        }

        [Fact]
        public void OrderByThenBySelectPaginate()
        {
            TestPaginate(Database.Query<ArtistEntity>().OrderBy(a => a.Name).ThenBy(a => a.Id).Select(a => a.Name));
        }

        [Fact]
        public void SelectOrderByPaginate()
        {
            TestPaginate(Database.Query<ArtistEntity>().Select(a => a.Name).OrderBy(a => a));
        }

        [Fact]
        public void SelectOrderByDescendingPaginate()
        {
            TestPaginate(Database.Query<ArtistEntity>().Select(a => a.Name).OrderByDescending(a => a));
        }

        private void TestPaginate<T>(IQueryable<T> query)
        {
            var list = query.OrderAlsoByKeys().ToList();

            int pageSize = 2;

            var list2 = 0.To(((list.Count / pageSize) + 1)).SelectMany(page =>
                query.OrderAlsoByKeys().Skip(pageSize * page).Take(pageSize).ToList()).ToList();

            Assert.Equal(list, list2);
        }
    }
}
