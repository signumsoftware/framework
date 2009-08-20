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

namespace Signum.Test.LinqProvider
{
    /// <summary>
    /// Summary description for LinqProvider
    /// </summary>
    [TestClass]
    public class SetOperationTest
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
        public void Concat()
        {
            var sexes =
                Database.Query<ArtistDN>().Where(a => a.Sex == Sex.Male).Concat(
                Database.Query<ArtistDN>().Where(a => a.Sex == Sex.Female)).Select(a => a.Sex).ToList();
            Assert.IsTrue(sexes.Count > 2); 
        }

        [TestMethod]
        public void Union()
        {
            var sexes =
                Database.Query<ArtistDN>().Where(a => a.Sex == Sex.Male).Union(
                Database.Query<ArtistDN>().Where(a => a.Sex == Sex.Female)).Select(a => a.Sex).ToList();
            Assert.IsTrue(sexes.Count > 2); 
        }

        [TestMethod]
        public void Union2()
        {
            var sexes =
                Database.Query<ArtistDN>().Select(a => a.Sex).Where(s => s == Sex.Male).Union(
                Database.Query<ArtistDN>().Select(a => a.Sex).Where(s => s == Sex.Female)).ToList();
            Assert.IsTrue(sexes.Count == 2);
        }

        [TestMethod]
        public void Distinct()
        {
            var sexes = Database.Query<ArtistDN>().Select(a => a.Sex).Distinct().ToList();
            Assert.IsTrue(sexes.Count == 2);
        }

        [TestMethod]
        public void Except()
        {
            var wretzky = 
                Database.Query<ArtistDN>().Except(
                Database.Query<ArtistDN>().Where(a => a.Sex == Sex.Male)).Single();
        }

        [TestMethod]
        public void Intersect()
        {
            var albums90 =
                Database.Query<AlbumDN>().Where(a=>a.Year < 2000).Intersect(
                Database.Query<AlbumDN>().Where(a=>1990 <= a.Year )).ToList();
        }
    }
}
