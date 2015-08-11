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
using System.Data.SqlTypes;
using System.Reflection;
using Signum.Utilities.ExpressionTrees;
using Signum.Entities.Reflection;
using Signum.Test.Environment;

namespace Signum.Test.LinqProvider
{
    /// <summary>
    /// Summary description for LinqProvider
    /// </summary>
    [TestClass]
    public class RetrieverTest
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
        public void RetrieveSimple()
        {
            var list = Database.Query<CountryEntity>().ToList();

            AssertRetrieved(list);
        }

        [TestMethod]
        public void RetrieveWithEnum()
        {
            var list = Database.Query<GrammyAwardEntity>().ToList();

            AssertRetrieved(list);
        }


        [TestMethod]
        public void RetrieveWithRelatedEntityAndLite()
        {
            var list = Database.Query<LabelEntity>().ToList();

            AssertRetrieved(list);
        }

        [TestMethod]
        public void RetrieveWithIBA()
        {
            var list = Database.Query<NoteWithDateEntity>().ToList();

            AssertRetrieved(list);
        }

        [TestMethod]
        public void RetrieveWithMList()
        {
            var list = Database.Query<ArtistEntity>().ToList();

            AssertRetrieved(list);
        }

        [TestMethod]
        public void RetrieveWithMListEmbedded()
        {
            var list = Database.Query<AlbumEntity>().ToList();

            AssertRetrieved(list);
        }

        private void AssertRetrieved<T>(List<T> list) where T:Modifiable
        {
            var graph = GraphExplorer.FromRoots(list);

            var problematic = graph.Where(a =>
                a.IsGraphModified &&
                a is Entity && (((Entity)a).IdOrNull == null || ((Entity)a).IsNew));

            if (problematic.Any())
                throw new AssertFailedException("Some non-retrived elements: {0}".FormatWith(problematic.ToString(", ")));  
        }


        [TestMethod]
        public void RetrieveWithMListCount()
        {
            var artist = Database.Query<ArtistEntity>().OrderBy(a => a.Name).First();

            Assert.AreEqual(artist.ToLite().Retrieve().Friends.Count, artist.Friends.Count);
        }
    }
}
