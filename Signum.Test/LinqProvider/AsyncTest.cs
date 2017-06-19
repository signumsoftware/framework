using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signum.Engine;
using Signum.Entities;
using Signum.Utilities;
using Signum.Entities.Basics;
using Signum.Test.Environment;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Test.LinqProvider
{
    /// <summary>
    /// Summary description for SelectManyTest
    /// </summary>
    [TestClass]
    public class AsyncTest
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
        public void ToListAsync()
        {
            var artistsInBands = Database.Query<BandEntity>().ToListAsync().Result;
        }

        [TestMethod]
        public void ToArrayAsync()
        {
            var artistsInBands = Database.Query<BandEntity>().ToArrayAsync().Result;
        }

        [TestMethod]
        public void AverageAsync()
        {
            var artistsInBands = Database.Query<BandEntity>().AverageAsync(a=>a.Members.Count).Result;
        }


        [TestMethod]
        public void MinAsync()
        {
            var artistsInBands = Database.Query<BandEntity>().MinAsync(a => a.Members.Count).Result;
        }
    }
}
