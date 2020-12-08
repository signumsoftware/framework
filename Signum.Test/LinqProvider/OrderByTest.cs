using System;
using System.Linq;
using Xunit;
using Signum.Engine;
using System.Diagnostics;
using System.IO;
using Signum.Utilities;
using Signum.Test.Environment;

namespace Signum.Test.LinqProvider
{
    /// <summary>
    /// Summary description for LinqProvider
    /// </summary>
    public class OrderByTest
    {
        public OrderByTest()
        {
            MusicStarter.StartAndLoad();
            Connector.CurrentLogger = new DebugTextWriter();
        }

        [Fact]
        public void OrderByString()
        {
            var songsAlbum = Database.Query<AlbumEntity>().Select(a => a.Name).OrderBy(n => n).ToList();
        }

        [Fact]
        public void OrderByIntDescending()
        {
            var songsAlbum = Database.Query<AlbumEntity>().OrderByDescending(a => a.Year).ToList();
        }

        [Fact]
        public void OrderByGetType()
        {
            var songsAlbum = Database.Query<AlbumEntity>().OrderBy(a => a.Author.GetType()).ToList();
        }

        [Fact]
        public void OrderByFirst()
        {
            var songsAlbum = Database.Query<ArtistEntity>().OrderBy(a => a.Dead).FirstEx();
        }

        [Fact]
        public void OrderByReverse()
        {
            var artists = Database.Query<ArtistEntity>().OrderBy(a => a.Dead).Reverse().Select(a => a.Name);
        }

        [Fact]
        public void OrderByLast()
        {
            var michael = Database.Query<ArtistEntity>().OrderBy(a => a.Dead).Last();
            Assert.Contains("Michael", michael.Name);
        }

        [Fact]
        public void OrderByLastPredicate()
        {
            var michael = Database.Query<ArtistEntity>().OrderBy(a => a.Dead).Last(a => a.Name.Length > 1);
            Assert.Contains("Michael", michael.Name);
        }

        [Fact]
        public void OrderByLastOrDefault()
        {
            var michael = Database.Query<ArtistEntity>().OrderBy(a => a.Dead).LastOrDefault()!;
            Assert.Contains("Michael", michael.Name);
        }

        [Fact]
        public void OrderByLastOrDefaultPredicate()
        {
            var michael = Database.Query<ArtistEntity>().OrderBy(a => a.Dead).LastOrDefault(a => a.Name.Length > 1)!;
            Assert.Contains("Michael", michael.Name);
        }

        [Fact]
        public void OrderByThenByReverseLast()
        {
            var michael = Database.Query<ArtistEntity>().OrderByDescending(a => a.Dead).ThenBy(a=>a.Name).Reverse().Last();
        }

        [Fact]
        public void OrderByTakeReverse()
        {
            var michael = Database.Query<ArtistEntity>().OrderByDescending(a => a.Dead).Take(2).Reverse().FirstEx(); //reverse ignored
        }

        [Fact]
        public void OrderByTakeOrderBy()
        {
            var michael = Database.Query<ArtistEntity>().OrderByDescending(a => a.Dead).Take(2).OrderBy(a=>a.Name).FirstEx();
        }

        [Fact]
        public void OrderByTop()
        {
            var songsAlbum = Database.Query<ArtistEntity>().OrderBy(a => a.Dead).Take(3);
        }

        [Fact]
        public void OrderByNotLast()
        {
            var songsAlbum = Database.Query<ArtistEntity>().OrderBy(a => a.Dead).Where(a => a.Id != 0).ToList();
        }

        [Fact]
        public void OrderByDistinct()
        {
            var songsAlbum = Database.Query<ArtistEntity>().OrderBy(a => a.Dead).Distinct().ToList();
        }

        [Fact]
        public void OrderByGroupBy()
        {
            var songsAlbum = Database.Query<ArtistEntity>().OrderBy(a => a.Dead)
                .GroupBy(a => a.Sex, (s, gr) => new { Sex = s, Count = gr.Count() }).ToList();
        }


        [Fact]
        public void OrderByIgnore()
        {
            using (AsserNoQueryWith("ORDER"))
                Database.Query<AlbumEntity>().Where(a => a.Songs.OrderBy(s => s.Name).Count() > 1).Select(a => a.Id).ToList();

            using (AsserNoQueryWith("ORDER"))
                Database.Query<AlbumEntity>().Where(a => a.Songs.OrderBy(s => s.Name).Sum(s => s.Name.Length) > 1).Select(a => a.Id).ToList();

            using (AsserNoQueryWith("ORDER"))
                Database.Query<AlbumEntity>().Where(a => a.Songs.OrderBy(s => s.Name).Any(s => s.Name.StartsWith("a"))).Select(a => a.Id).ToList();

            using (AsserNoQueryWith("ORDER"))
                Database.Query<AlbumEntity>().Where(a => a.Songs.OrderBy(s => s.Name).All(s => s.Name.StartsWith("a"))).Select(a => a.Id).ToList();

            using (AsserNoQueryWith("ORDER"))
                Database.Query<AlbumEntity>().Where(a => a.Songs.OrderBy(s => s.Name).Contains(null!)).Select(a => a.Id).ToList();



            using (AsserNoQueryWith("ORDER"))
                Database.Query<AlbumEntity>().OrderBy(a => a.Name).Count();

            using (AsserNoQueryWith("ORDER"))
                Database.Query<AlbumEntity>().OrderBy(a => a.Name).Sum(s => s.Name.Length);

            using (AsserNoQueryWith("ORDER"))
                Database.Query<AlbumEntity>().OrderBy(a => a.Name).Any(s => s.Name.StartsWith("a"));

            using (AsserNoQueryWith("ORDER"))
                Database.Query<AlbumEntity>().OrderBy(a => a.Name).All(s => s.Name.StartsWith("a"));

            using (AsserNoQueryWith("ORDER"))
                Database.Query<AlbumEntity>().OrderBy(a => a.Name).Contains(null!);
        }



        public IDisposable AsserNoQueryWith(string text)
        {
            var oldLogger = Connector.CurrentLogger;
            var sw =  new StringWriter();
            Connector.CurrentLogger = sw;
            return new Disposable(() =>
            {
                Connector.CurrentLogger = oldLogger;
                string str = sw.ToString();

                sw.Dispose();
                Debug.Write(str);

                Assert.True(!str.Contains(text));
            });
        }


    }
}
