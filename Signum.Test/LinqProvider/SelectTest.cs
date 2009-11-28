using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signum.Engine;
using Signum.Entities;
using System.Diagnostics;
using System.IO;
using Signum.Engine.Linq;
using Signum.Utilities;
using System.Linq.Expressions;

namespace Signum.Test.LinqProvider
{
    /// <summary>
    /// Summary description for LinqProvider
    /// </summary>
    [TestClass]
    public class SelectTest
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
        public void Select()
        {
            var list = Database.Query<AlbumDN>().Select(a => a.Name).ToList();
        }

        [TestMethod]
        public void SelectExpansion()
        {
            var list = Database.Query<AlbumDN>().Select(a => a.Label.Name).ToList();
        }

        [TestMethod]
        public void SelectWhereExpansion()
        {
            var list = Database.Query<AlbumDN>().Where(a=>a.Label != null).Select(a => a.Label.Name).ToList();
        }

        [TestMethod]
        public void SelectAnonymous()
        {
            var list = Database.Query<AlbumDN>().Select(a => new { a.Name, a.Year }).ToList();
        }

        [TestMethod]
        public void SelectNoColumns()
        {
            var list = Database.Query<AlbumDN>().Select(a => new { DateTime.Now, Album = (AlbumDN)null, Artist = (Lite<ArtistDN>)null }).ToList();
        }

        [TestMethod]
        public void SelectCount()
        {
            var list = Database.Query<AlbumDN>().Select(a => (int?)a.Songs.Count).ToList();
        }

        [TestMethod]
        public void SelectLite()
        {
            var list = Database.Query<AlbumDN>().Select(a => a.ToLite()).ToList();
        }

        [TestMethod]
        public void SelectLiteToStr()
        {
            var list = Database.Query<AlbumDN>().Select(a => a.ToLite(a.Label.Name)).ToList();
        }

        [TestMethod]
        public void SelectLiteIB()
        {
            var list = Database.Query<AlbumDN>()
                .Select(a => a.Author.ToLite())
                .Where(a => a.ToStr.StartsWith("Michael")).ToList();
        }

        [TestMethod]
        public void SelectLiteIBA()
        {
            var list = Database.Query<NoteDN>().Select(a => a.Target.ToLite()).ToList();
        }

        [TestMethod]
        public void SelectEntity()
        {
            var list = Database.Query<AlbumDN>().ToList();
        }

        [TestMethod]
        public void SelectEntitySelect()
        {
            var list = Database.Query<AlbumDN>().Select(a => a).ToList();
        }

        [TestMethod]
        public void SelectEntityIB()
        {
            var list = Database.Query<AlbumDN>().Select(a => a.Author).ToList();
        }

        [TestMethod]
        public void SelectEntityIBA()
        {
            var list = Database.Query<NoteDN>().Select(a => a.Target).ToList();
        }

        [TestMethod]
        public void SelectBool()
        {
            var list = Database.Query<ArtistDN>().Select(a => a.Dead).ToList();
        }

        [TestMethod]
        public void SelectConditionToBool()
        {
            var list = Database.Query<AlbumDN>().Select(a => a.Year < 1990).ToList();
        }

        [TestMethod]
        public void SelectCastIB()
        {
            var list = (from a in Database.Query<AlbumDN>()
                       select ((ArtistDN)a.Author).Name ?? 
                              ((BandDN)a.Author).Name).ToList();
        }

        [TestMethod]
        public void SelectCastIBPolymorphic()
        {
            var list = (from a in Database.Query<AlbumDN>()
                        select a.Author.Name).ToList();
        }

        [TestMethod]
        public void SelectCastIBPolymorphicIB()
        {
            var list = (from a in Database.Query<AlbumDN>()
                        select a.Author.LastAward.ToLite()).ToList();
        }

        [TestMethod]
        public void SelectCastIBA()
        {
            var list = (from n in Database.Query<NoteDN>()
                        select 
                        ((ArtistDN)n.Target).Name ??
                        ((AlbumDN)n.Target).Name ??
                        ((BandDN)n.Target).Name).ToList();               
        }

        [TestMethod]
        public void SelectCastIsIB()
        {
            var list = (from a in Database.Query<AlbumDN>()
                        select (a.Author is ArtistDN ? ((ArtistDN)a.Author).Name :
                                                        ((BandDN)a.Author).Name)).ToList(); //Just to full-nominate
        }

        [TestMethod]
        public void SelectCastIsIBA()
        {
            var list = (from n in Database.Query<NoteDN>()
                        select n.Target is ArtistDN ?
                        ((ArtistDN)n.Target).Name :
                        ((BandDN)n.Target).Name).ToList(); //Just to full-nominate
        }

        [TestMethod]
        public void SelectEntityEquals()
        {
            ArtistDN michael = Database.Query<ArtistDN>().Single(a => a.Dead);

            var list = Database.Query<AlbumDN>().Select(a => a.Author == michael).ToList(); 
        }

        [TestMethod]
        public void SelectMList()
        {
            ArtistDN michael = Database.Query<ArtistDN>().Single(a => a.Dead);

            var list = Database.Query<AlbumDN>().Select(a => a.Author == michael).ToList();
        }

        [TestMethod]
        public void SelectExpressionProperty()
        {
            var list = Database.Query<ArtistDN>().Where(a => a.IsMale).ToArray();
        }

        [TestMethod]
        public void SelectExpressionMethod()
        {
            var list = Database.Query<ArtistDN>().Select(a => new { a.Name, Count = a.AlbumCount() }).ToArray();
        }
    }

    public static class AuthorExtensions
    {
        static Expression<Func<IAuthorDN, int>> AlbumCountExpression = auth => Database.Query<AlbumDN>().Count(a => a.Author == auth);
        public static int AlbumCount(this IAuthorDN author)
        {
            return Database.Query<AlbumDN>().Count(a => a.Author == author);
        }
    }
}
