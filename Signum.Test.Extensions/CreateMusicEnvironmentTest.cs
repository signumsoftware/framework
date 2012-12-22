using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signum.Entities.Authorization;
using Signum.Entities;
using Signum.Utilities.Reflection;
using Signum.Engine.Maps;
using Signum.Engine.Authorization;
using Signum.Engine.DynamicQuery;
using Signum.Engine;
using Signum.Services;
using Signum.Engine.Basics;
using Signum.Utilities;
using Signum.Test.Extensions.Properties;
using Signum.Engine.Cache;
using System.Threading;
using System.Threading.Tasks;
using Signum.Engine.Operations;

namespace Signum.Test.Extensions
{
    [TestClass]
    public class CreateMusicEnvironmentTest
    {
        [TestMethod]
        public void CreateMusicEnvironment()
        {
            Starter.StartAndLoad(UserConnections.Replace(Settings.Default.ConnectionString));
        }

        [TestMethod]
        public void CacheInvalidationTest()
        {
            Starter.Start(UserConnections.Replace(Settings.Default.ConnectionString));
            Schema.Current.Initialize();

            int invalidations = CacheLogic.Statistics().Single(t => t.Type == typeof(LabelDN)).Invalidations;

            Assert.IsTrue(Schema.Current.EntityEvents<LabelDN>().CacheController.Enabled);

            using(AuthLogic.Disable())
            using (OperationLogic.AllowSave<LabelDN>())
            using (Transaction tr = new Transaction())
            {
                Assert.IsTrue(Schema.Current.EntityEvents<LabelDN>().CacheController.Enabled);
                var label = Database.Retrieve<LabelDN>(1);

                label.Name += " - ";

                label.Save();

                Assert.AreEqual(invalidations, CacheLogic.Statistics().Single(t => t.Type == typeof(LabelDN)).Invalidations);
                Assert.IsFalse(Schema.Current.EntityEvents<LabelDN>().CacheController.Enabled);

                Task.Factory.StartNew(() =>
                {
                    Assert.IsTrue(Schema.Current.EntityEvents<LabelDN>().CacheController.Enabled);
                }).Wait();

                tr.Commit();
            }

            Assert.AreEqual(invalidations + 1, CacheLogic.Statistics().Single(t => t.Type == typeof(LabelDN)).Invalidations);
            Assert.IsTrue(Schema.Current.EntityEvents<LabelDN>().CacheController.Enabled);
        }
    }
}
