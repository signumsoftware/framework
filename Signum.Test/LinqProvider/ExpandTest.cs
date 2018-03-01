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
using Signum.Test.Environment;

namespace Signum.Test.LinqProvider
{
    /// <summary>
    /// Summary description for LinqProvider
    /// </summary>
    [TestClass]
    public class ExpandTest
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
        public void ExpandToStringNull()
        {
            Database.Query<CountryEntity>().Select(a => a.ToLite()).ExpandLite(a => a, ExpandLite.ToStringNull).ToList();
        }

        [TestMethod]
        public void ExpandToStringLazy()
        {
            Database.Query<CountryEntity>().Select(a => a.ToLite()).ExpandLite(a => a, ExpandLite.ToStringLazy).ToList();
        }

        [TestMethod]
        public void ExpandToStringEager()
        {
            Database.Query<CountryEntity>().Select(a => a.ToLite()).ExpandLite(a => a, ExpandLite.ToStringEager).ToList();
        }

        [TestMethod]
        public void ExpandEntityEager()
        {
            var list = Database.Query<CountryEntity>().Select(a => a.ToLite()).ExpandLite(a => a, ExpandLite.EntityEager).ToList();
        }


        [TestMethod]
        public void ExpandLazyEntity()
        {
            var list = Database.Query<CountryEntity>().ExpandEntity(a => a, ExpandEntity.LazyEntity).ToList();
        }

    }
}
