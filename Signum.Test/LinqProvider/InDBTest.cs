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
using Signum.Engine.Exceptions;
using Signum.Utilities.ExpressionTrees;
using Signum.Test.Environment;

namespace Signum.Test.LinqProvider
{
    /// <summary>
    /// Summary description for LinqProvider
    /// </summary>
    [TestClass]
    public class InDbTest
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

        private static ArtistDN GetFemale()
        {
            return Database.Query<ArtistDN>().Where(a => a.Sex == Sex.Female).Single();
        }

        [TestMethod]
        public void InDbTestSimple()
        {
            var female = GetFemale();

            Assert.AreEqual(Sex.Female, female.InDB().Select(a => a.Sex).Single());
            Assert.AreEqual(Sex.Female, female.ToLite().InDB().Select(a => a.Sex).Single());
        }

        [TestMethod]
        public void InDbTestSimpleList()
        {
            var female = GetFemale();

            var friends = female.InDB().Select(a => a.Friends.ToList()).Single();
            friends = female.ToLite().InDB().Select(a => a.Friends.ToList()).Single();
        }

        [TestMethod]
        public void InDbTestSelector()
        {
            var female = GetFemale();

            Assert.AreEqual(Sex.Female, female.InDBEntity(a => a.Sex));
            Assert.AreEqual(Sex.Female, female.ToLite().InDB(a => a.Sex));
        }

        [TestMethod]
        public void InDbTestSelectosList()
        {
            var female = GetFemale();

            var friends = female.InDBEntity(a => a.Friends.ToList());
            friends = female.ToLite().InDB(a => a.Friends.ToList());
        }



        [TestMethod]
        public void InDbQueryTestSimple()
        {
            var female = GetFemale();

            var list = Database.Query<ArtistDN>().Where(a=>a.Sex != female.InDB().Select(a2 => a2.Sex).Single()).ToList();
            Assert.IsTrue(list.Count > 0);
            list = Database.Query<ArtistDN>().Where(a => a.Sex != female.ToLite().InDB().Select(a2 => a2.Sex).Single()).ToList();
            Assert.IsTrue(list.Count > 0);
        }

        [TestMethod]
        public void InDbQueryTestSimpleList()
        {
            var female = GetFemale();

            var list = Database.Query<ArtistDN>().Where(a =>female.InDB().Select(a2 => a2.Friends).Single().Contains(a.ToLite())).ToList();
            Assert.IsTrue(list.Count > 0);
            list = Database.Query<ArtistDN>().Where(a => female.ToLite().InDB().Select(a2 => a2.Friends).Single().Contains(a.ToLite())).ToList();
            Assert.IsTrue(list.Count > 0);
        }

        [TestMethod]
        public void InDbQueryTestSimpleSelector()
        {
            var female = GetFemale();

            var list = Database.Query<ArtistDN>().Where(a => a.Sex != female.InDBEntity(a2 => a2.Sex)).ToList();
            Assert.IsTrue(list.Count > 0);
            list = Database.Query<ArtistDN>().Where(a => a.Sex != female.ToLite().InDB(a2 => a2.Sex)).ToList();
            Assert.IsTrue(list.Count > 0);
        }

        [TestMethod]
        public void InDbQueryTestSimpleListSelector()
        {
            var female = GetFemale();

            var list = Database.Query<ArtistDN>().Where(a => female.InDBEntity(a2 => a2.Friends).Contains(a.ToLite())).ToList();
            Assert.IsTrue(list.Count > 0);
            list = Database.Query<ArtistDN>().Where(a => female.ToLite().InDB(a2 => a2.Friends).Contains(a.ToLite())).ToList();
            Assert.IsTrue(list.Count > 0);
        }

        [TestMethod]
        public void SelectManyInDB()
        {
            var artistsInBands = (from b in Database.Query<BandDN>()
                                  from a in b.Members
                                  select new
                                  {
                                      MaxAlbum = a.InDBEntity(ar => ar.IsMale)
                                  }).ToList();
        }
    }
}
