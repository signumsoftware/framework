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
using System.Linq.Expressions;
using Signum.Utilities.ExpressionTrees;

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
            Connection.CurrentLogger = new DebugTextWriter();
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

            Dump((ArtistDN a)=> a.Name.Length);
            Dump((ArtistDN a)=>a.Name.ToLower());
            Dump((ArtistDN a)=>a.Name.ToUpper());
            Dump((ArtistDN a) => a.Name.TrimStart());
            Dump((ArtistDN a) => a.Name.TrimEnd());
            Dump((ArtistDN a) => a.Name.Substring(2).InSql());
            Dump((ArtistDN a) => a.Name.Substring(2, 2).InSql());

            Dump((ArtistDN a) => a.Name.Left(2).InSql());
            Dump((ArtistDN a) => a.Name.Right(2).InSql());
            Dump((ArtistDN a) => a.Name.Reverse().InSql());
            Dump((ArtistDN a) => a.Name.Replicate(2).InSql());
        }

        [TestMethod]
        public void DateTimeFunctions()
        {
            var artists = Database.Query<ArtistDN>();
            Assert.IsTrue(artists.Any(a => a.Name.IndexOf('M') == 0));
            Assert.IsTrue(artists.Any(a => a.Name.IndexOf("Mi") == 0));
            Assert.IsTrue(artists.Any(a => a.Name.Contains("Jackson")));
            Assert.IsTrue(artists.Any(a => a.Name.StartsWith("Billy")));
            Assert.IsTrue(artists.Any(a => a.Name.EndsWith("Corgan")));
            Assert.IsTrue(artists.Any(a => a.Name.Like("%Michael%")));

            Dump((ArtistDN a) => a.Name.Length);
            Dump((ArtistDN a) => a.Name.ToLower());
            Dump((ArtistDN a) => a.Name.ToUpper());
            Dump((ArtistDN a) => a.Name.TrimStart());
            Dump((ArtistDN a) => a.Name.TrimEnd());
            Dump((ArtistDN a) => a.Name.Substring(2).InSql());
            Dump((ArtistDN a) => a.Name.Substring(2, 2).InSql());

            Dump((ArtistDN a) => a.Name.Left(2).InSql());
            Dump((ArtistDN a) => a.Name.Right(2).InSql());
            Dump((ArtistDN a) => a.Name.Reverse().InSql());
            Dump((ArtistDN a) => a.Name.Replicate(2).InSql());
        }

        [TestMethod]
        public void DateDifFunctions()
        {
            Dump((NoteWithDateDN n) => n.CreationTime.Year);
            Dump((NoteWithDateDN n) => n.CreationTime.Month);
            Dump((NoteWithDateDN n) => n.CreationTime.Day);
            Dump((NoteWithDateDN n) => n.CreationTime.DayOfYear);
            Dump((NoteWithDateDN n) => n.CreationTime.Hour);
            Dump((NoteWithDateDN n) => n.CreationTime.Minute);
            Dump((NoteWithDateDN n) => n.CreationTime.Second);
            Dump((NoteWithDateDN n) => n.CreationTime.Millisecond);
           

            Dump((NoteWithDateDN n) => (n.CreationTime - n.CreationTime).TotalDays.InSql());
            Dump((NoteWithDateDN n) => (n.CreationTime - n.CreationTime).TotalHours.InSql());
            Dump((NoteWithDateDN n) => (n.CreationTime - n.CreationTime).TotalMinutes.InSql());
            Dump((NoteWithDateDN n) => (n.CreationTime - n.CreationTime).TotalSeconds.InSql());
            Dump((NoteWithDateDN n) => (n.CreationTime.AddDays(1) - n.CreationTime).TotalMilliseconds.InSql());
        }

        [TestMethod]
        public void DateFunctions()
        {
            Dump((NoteWithDateDN n) => n.CreationTime.Date);
        }

        [TestMethod]
        public void DayOfWeekFunction()
        {
            var list = Database.Query<NoteWithDateDN>().Where(n => n.CreationTime.DayOfWeek != DayOfWeek.Sunday)
                .Select(n => n.CreationTime.DayOfWeek).ToList();
        }

        [TestMethod]
        public void MathFunctions()
        {
            Dump((AlbumDN a) => Math.Sign(a.Year));
            Dump((AlbumDN a) => Math.Abs(a.Year));
            Dump((AlbumDN a) => Math.Sin(a.Year));
            Dump((AlbumDN a) => Math.Asin(Math.Sin(a.Year)));
            Dump((AlbumDN a) => Math.Cos(a.Year));
            Dump((AlbumDN a) => Math.Acos(Math.Cos(a.Year)));
            Dump((AlbumDN a) => Math.Tan(a.Year));
            Dump((AlbumDN a) => Math.Atan(Math.Tan(a.Year)));
            Dump((AlbumDN a) => Math.Atan2(1,1).InSql());
            Dump((AlbumDN a) => Math.Pow(a.Year, 2).InSql());
            Dump((AlbumDN a) => Math.Sqrt(a.Year));
            Dump((AlbumDN a) => Math.Exp(Math.Log(a.Year)));
            Dump((AlbumDN a) => Math.Floor(a.Year + 0.5).InSql());
            Dump((AlbumDN a) => Math.Log10(a.Year));
            Dump((AlbumDN a) => Math.Ceiling(a.Year + 0.5).InSql());
            Dump((AlbumDN a) => Math.Round(a.Year + 0.5).InSql());
            Dump((AlbumDN a) => Math.Truncate(a.Year + 0.5).InSql());
        }

        public void Dump<T,S>(Expression<Func<T, S>> bla)
            where T:IdentifiableEntity
        {
            Debug.WriteLine(Database.Query<T>().Select(a => bla.Invoke(a).InSql()).ToString(","));
        }
    }
}
