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
using Signum.Engine.Linq;

namespace Signum.Test.LinqProvider
{
    /// <summary>
    /// Summary description for LinqProvider
    /// </summary>
    [TestClass]
    public class SqlFunctionsTest
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
        public void StringFunctions()
        {
            var artists = Database.Query<ArtistDN>(); 
            Assert.IsTrue(artists.Any(a=>a.Name.IndexOf('M') == 0));
            Assert.IsTrue(artists.Any(a => a.Name.Contains("Jackson")));
            Assert.IsTrue(artists.Any(a => a.Name.StartsWith("Billy")));
            Assert.IsTrue(artists.Any(a => a.Name.EndsWith("Corgan")));
            Assert.IsTrue(artists.Any(a => a.Name.Like("%Michael%")));

            Debug.WriteLine(artists.Select(a => a.Name.Length).ToString(","));
            Debug.WriteLine(artists.Select(a=>a.Name.ToLower()).ToString(","));
            Debug.WriteLine(artists.Select(a=>a.Name.ToUpper()).ToString(","));
            Debug.WriteLine(artists.Select(a => a.Name.TrimStart()).ToString(","));
            Debug.WriteLine(artists.Select(a => a.Name.TrimEnd()).ToString(","));
            Debug.WriteLine(artists.Select(a => a.Name.Substring(2).InSql()).ToString(","));
            Debug.WriteLine(artists.Select(a => a.Name.Substring(2, 2).InSql()).ToString(","));

            Debug.WriteLine(artists.Select(a => a.Name.Left(2).InSql()).ToString(","));
            Debug.WriteLine(artists.Select(a => a.Name.Right(2).InSql()).ToString(","));
            Debug.WriteLine(artists.Select(a => a.Name.Reverse().InSql()).ToString(","));
            Debug.WriteLine(artists.Select(a => a.Name.Replicate(2).InSql()).ToString(","));
        }

        [TestMethod]
        public void DateTimeFunctions()
        {
            var artists = Database.Query<ArtistDN>();
            Assert.IsTrue(artists.Any(a => a.Name.IndexOf('M') == 0));
            Assert.IsTrue(artists.Any(a => a.Name.Contains("Jackson")));
            Assert.IsTrue(artists.Any(a => a.Name.StartsWith("Billy")));
            Assert.IsTrue(artists.Any(a => a.Name.EndsWith("Corgan")));
            Assert.IsTrue(artists.Any(a => a.Name.Like("%Michael%")));

            Debug.WriteLine(artists.Select(a => a.Name.Length).ToString(","));
            Debug.WriteLine(artists.Select(a => a.Name.ToLower()).ToString(","));
            Debug.WriteLine(artists.Select(a => a.Name.ToUpper()).ToString(","));
            Debug.WriteLine(artists.Select(a => a.Name.TrimStart()).ToString(","));
            Debug.WriteLine(artists.Select(a => a.Name.TrimEnd()).ToString(","));
            Debug.WriteLine(artists.Select(a => a.Name.Substring(2).InSql()).ToString(","));
            Debug.WriteLine(artists.Select(a => a.Name.Substring(2, 2).InSql()).ToString(","));

            Debug.WriteLine(artists.Select(a => a.Name.Left(2).InSql()).ToString(","));
            Debug.WriteLine(artists.Select(a => a.Name.Right(2).InSql()).ToString(","));
            Debug.WriteLine(artists.Select(a => a.Name.Reverse().InSql()).ToString(","));
            Debug.WriteLine(artists.Select(a => a.Name.Replicate(2).InSql()).ToString(","));
        }

        [TestMethod]
        public void DateDifFunctions()
        {
            var notes = Database.Query<NoteDN>();
            Debug.WriteLine(notes.Select(n => DateTime.Now.InSql()).ToString(","));
            Debug.WriteLine(notes.Select(n => n.CreationTime.Year).ToString(","));
            Debug.WriteLine(notes.Select(n => n.CreationTime.Month).ToString(","));
            Debug.WriteLine(notes.Select(n => n.CreationTime.Day).ToString(","));
            Debug.WriteLine(notes.Select(n => n.CreationTime.DayOfYear).ToString(","));
            Debug.WriteLine(notes.Select(n => n.CreationTime.Hour).ToString(","));
            Debug.WriteLine(notes.Select(n => n.CreationTime.Minute).ToString(","));
            Debug.WriteLine(notes.Select(n => n.CreationTime.Second).ToString(","));
            Debug.WriteLine(notes.Select(n => n.CreationTime.Millisecond).ToString(","));
            Debug.WriteLine(notes.Select(n => n.CreationTime.Date).ToString(","));

            Debug.WriteLine(notes.Select(n => (DateTime.Today - n.CreationTime).TotalDays.InSql()).ToString(","));
            Debug.WriteLine(notes.Select(n => (DateTime.Today - n.CreationTime).TotalHours.InSql()).ToString(","));
            Debug.WriteLine(notes.Select(n => (DateTime.Today - n.CreationTime).TotalMinutes.InSql()).ToString(","));
            Debug.WriteLine(notes.Select(n => (DateTime.Today - n.CreationTime).TotalSeconds.InSql()).ToString(","));
            Debug.WriteLine(notes.Select(n => (n.CreationTime.AddDays(1) - n.CreationTime).TotalMilliseconds.InSql()).ToString(","));
        }

        [TestMethod]
        public void MathFunctions()
        {
            var album = Database.Query<AlbumDN>();
            Debug.WriteLine(album.Select(a => Math.Sign(a.Year)).ToString(","));
            Debug.WriteLine(album.Select(a => Math.Abs(a.Year)).ToString(","));
            Debug.WriteLine(album.Select(a => Math.Sin(a.Year)).ToString(","));
            Debug.WriteLine(album.Select(a => Math.Asin(Math.Sin(a.Year))).ToString(","));
            Debug.WriteLine(album.Select(a => Math.Cos(a.Year)).ToString(","));
            Debug.WriteLine(album.Select(a => Math.Acos(Math.Cos(a.Year))).ToString(","));
            Debug.WriteLine(album.Select(a => Math.Tan(a.Year)).ToString(","));
            Debug.WriteLine(album.Select(a => Math.Atan(Math.Tan(a.Year))).ToString(","));
            Debug.WriteLine(album.Select(a => Math.Atan2(1,1).InSql()).ToString(","));
            Debug.WriteLine(album.Select(a => Math.Pow(a.Year, 2).InSql()).ToString(","));
            Debug.WriteLine(album.Select(a => Math.Sqrt(a.Year)).ToString(","));
            Debug.WriteLine(album.Select(a => Math.Exp(Math.Log(a.Year))).ToString(","));
            Debug.WriteLine(album.Select(a => Math.Floor(a.Year + 0.5).InSql()).ToString(","));
            Debug.WriteLine(album.Select(a => Math.Log10(a.Year)).ToString(","));
            Debug.WriteLine(album.Select(a => Math.Ceiling(a.Year + 0.5).InSql()).ToString(","));
            Debug.WriteLine(album.Select(a => Math.Round(a.Year + 0.5).InSql()).ToString(","));
        }
    }
}
