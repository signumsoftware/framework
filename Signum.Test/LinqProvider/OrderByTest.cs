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
using Signum.Utilities.ExpressionTrees;
using Signum.Test.Environment;

namespace Signum.Test.LinqProvider
{
    /// <summary>
    /// Summary description for LinqProvider
    /// </summary>
    [TestClass]
    public class OrderByTest
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
        public void OrderByString()
        {
            var songsAlbum = Database.Query<AlbumEntity>().Select(a => a.Name).OrderBy(n => n).ToList();
        }

        [TestMethod]
        public void OrderByIntDescending()
        {
            var songsAlbum = Database.Query<AlbumEntity>().OrderByDescending(a => a.Year).ToList();
        }

        [TestMethod]
        public void OrderByGetType()
        {
            var songsAlbum = Database.Query<AlbumEntity>().OrderBy(a => a.Author.GetType()).ToList();
        }

        [TestMethod]
        public void OrderByFirst()
        {
            var songsAlbum = Database.Query<ArtistEntity>().OrderBy(a => a.Dead).FirstEx();
        }

        [TestMethod]
        public void OrderByReverse()
        {
            var artists = Database.Query<ArtistEntity>().OrderBy(a => a.Dead).Reverse().Select(a => a.Name);
        }

        [TestMethod]
        public void OrderByLast()
        {
            var michael = Database.Query<ArtistEntity>().OrderBy(a => a.Dead).Last();
            Assert.IsTrue(michael.Name.Contains("Michael"));
        }

        [TestMethod]
        public void OrderByLastPredicate()
        {
            var michael = Database.Query<ArtistEntity>().OrderBy(a => a.Dead).Last(a => a.Name.Length > 1);
            Assert.IsTrue(michael.Name.Contains("Michael"));
        }

        [TestMethod]
        public void OrderByLastOrDefault()
        {
            var michael = Database.Query<ArtistEntity>().OrderBy(a => a.Dead).LastOrDefault();
            Assert.IsTrue(michael.Name.Contains("Michael"));
        }

        [TestMethod]
        public void OrderByLastOrDefaultPredicate()
        {
            var michael = Database.Query<ArtistEntity>().OrderBy(a => a.Dead).LastOrDefault(a => a.Name.Length > 1);
            Assert.IsTrue(michael.Name.Contains("Michael"));
        }

        [TestMethod]
        public void OrderByThenByReverseLast()
        {
            var michael = Database.Query<ArtistEntity>().OrderByDescending(a => a.Dead).ThenBy(a=>a.Name).Reverse().Last();
        }

        [TestMethod]
        public void OrderByTakeReverse()
        {
            var michael = Database.Query<ArtistEntity>().OrderByDescending(a => a.Dead).Take(2).Reverse().FirstEx(); //reverse ignored
        }

        [TestMethod]
        public void OrderByTakeOrderBy()
        {
            var michael = Database.Query<ArtistEntity>().OrderByDescending(a => a.Dead).Take(2).OrderBy(a=>a.Name).FirstEx();
        }

        [TestMethod]
        public void OrderByTop()
        {
            var songsAlbum = Database.Query<ArtistEntity>().OrderBy(a => a.Dead).Take(3);
        }

        [TestMethod]
        public void OrderByNotLast()
        {
            var songsAlbum = Database.Query<ArtistEntity>().OrderBy(a => a.Dead).Where(a => a.Id != 0).ToList();
        }

        [TestMethod]
        public void OrderByDistinct()
        {
            var songsAlbum = Database.Query<ArtistEntity>().OrderBy(a => a.Dead).Distinct().ToList();
        }

        [TestMethod]
        public void OrderByGroupBy()
        {
            var songsAlbum = Database.Query<ArtistEntity>().OrderBy(a => a.Dead)
                .GroupBy(a => a.Sex, (s, gr) => new { Sex = s, Count = gr.Count() }).ToList();
        }


        [TestMethod]
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
                Database.Query<AlbumEntity>().Where(a => a.Songs.OrderBy(s => s.Name).Contains(null)).Select(a => a.Id).ToList();



            using (AsserNoQueryWith("ORDER"))
                Database.Query<AlbumEntity>().OrderBy(a => a.Name).Count();

            using (AsserNoQueryWith("ORDER"))
                Database.Query<AlbumEntity>().OrderBy(a => a.Name).Sum(s => s.Name.Length);

            using (AsserNoQueryWith("ORDER"))
                Database.Query<AlbumEntity>().OrderBy(a => a.Name).Any(s => s.Name.StartsWith("a"));

            using (AsserNoQueryWith("ORDER"))
                Database.Query<AlbumEntity>().OrderBy(a => a.Name).All(s => s.Name.StartsWith("a"));

            using (AsserNoQueryWith("ORDER"))
                Database.Query<AlbumEntity>().OrderBy(a => a.Name).Contains(null);
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

                Assert.IsTrue(!str.Contains(text));
            }); 
        }

       
    }
}
