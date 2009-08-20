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
        public void SelectAnonymous()
        {
            var list = Database.Query<AlbumDN>().Select(a => new { a.Name, a.Year }).ToList();
        }

        [TestMethod]
        public void SelectNoColumns()
        {
            var list = Database.Query<AlbumDN>().Select(a => new { DateTime.Now, Album = (AlbumDN)null, Artist = (Lazy<ArtistDN>)null }).ToList();
        }

        [TestMethod]
        public void SelectCount()
        {
            var list = Database.Query<AlbumDN>().Select(a => a.Song.Count).ToList();
        }

        [TestMethod]
        public void SelectLazy()
        {
            var list = Database.Query<AlbumDN>().Select(a => a.ToLazy()).ToList();
        }

        [TestMethod]
        public void SelectLazyIB()
        {
            var list = Database.Query<AlbumDN>().Select(a => a.Author.ToLazy()).ToList();
        }

        [TestMethod]
        public void SelectLazyIBA()
        {
            var list = Database.Query<NoteDN>().Select(a => a.Target.ToLazy()).ToList();
        }

        [TestMethod]
        public void SelectEntity()
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
                        select (a.Author is ArtistDN ?
                        ((ArtistDN)a.Author).Name :
                        ((BandDN)a.Author).Name).InSql()).ToList(); //Just to full-nominate
        }

        [TestMethod]
        public void SelectCastIsIBA()
        {
            var list = (from n in Database.Query<NoteDN>()
                        select (n.Target is ArtistDN ?
                        ((ArtistDN)n.Target).Name :
                        ((BandDN)n.Target).Name).InSql()).ToList(); //Just to full-nominate
        }

        [TestMethod]
        public void SelectEntityEquals()
        {
            ArtistDN michael = Database.Query<ArtistDN>().Single(a => a.Dead);

            var list = Database.Query<AlbumDN>().Select(a => a.Author == michael).ToList(); 
        }   
    }
}
