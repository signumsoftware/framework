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
    public class DistinctTest
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
        public void DistinctString()
        {
            var authors = Database.Query<AlbumDN>().Select(a => a.Label.Name).Distinct().ToList();
        }

        [TestMethod]
        public void DistinctPair()
        {
            var authors = Database.Query<ArtistDN>().Select(a =>new {a.Sex, a.Dead}).Distinct().ToList();
        }

        [TestMethod]
        public void DistinctFie()
        {
            var authors = Database.Query<AlbumDN>().Select(a => a.Label).Distinct().ToList();
        }

        [TestMethod]
        public void DistinctFieExpanded()
        {
            var authors = Database.Query<AlbumDN>().Where(a => a.Year != 0).Select(a => a.Label).Distinct().ToList();
        }

        [TestMethod]
        public void DistinctIb()
        {
            var authors = Database.Query<AlbumDN>().Select(a => a.Author).Distinct().ToList();
        }
    }
}
