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
using Signum.Test.Extensions.Properties;
using Signum.Services;
using Signum.Engine.Basics;
using Signum.Utilities;

namespace Signum.Test.Extensions
{
    [TestClass]
    public class EntityGroupsTest
    {

        [ClassInitialize()]
        public static void MyClassInitialize(TestContext testContext)
        {
            Starter.StartAndLoad(UserConnections.Replace(Settings.Default.ConnectionString));
        }


        [TestInitialize]
        public void Initialize()
        {
            Connection.CurrentLog = new DebugTextWriter();
        }

        const int JapLab = 2;
        const int AllLab = 7;

        const int JapAlb = 5;
        const int AllAlb = 12; 


        [TestMethod]
        public void EntityGroupsAuthDisable()
        {
            using (AuthLogic.Disable())
            {
                Assert.AreEqual(AllLab, Database.Query<LabelDN>().Count());
                Assert.AreEqual(JapLab, Database.Query<LabelDN>().Count(r => r.IsInGroup(MusicGroups.JapanEntities)));

                Assert.AreEqual(AllLab, Database.RetrieveAll<LabelDN>().Count);
                Assert.AreEqual(AllLab, Database.RetrieveAllLite<LabelDN>().Count);

                Assert.AreEqual(AllLab, Database.Query<LabelDN>().WhereAllowed().Count());
            }
        }

        [TestMethod]
        public void EntityGroupsQueryableAuthDisable()
        {
            using (AuthLogic.Disable())
            {
                Assert.AreEqual(AllAlb, Database.Query<AlbumDN>().Count());
                Assert.AreEqual(JapAlb, Database.Query<AlbumDN>().Count(r => r.IsInGroup(MusicGroups.JapanEntities)));

                Assert.AreEqual(AllAlb, Database.RetrieveAll<AlbumDN>().Count);
                Assert.AreEqual(AllAlb, Database.RetrieveAllLite<AlbumDN>().Count);

                Assert.AreEqual(AllAlb, Database.Query<AlbumDN>().WhereAllowed().Count());
            }
        }

        [TestMethod]
        public void EntityGroupsBart()
        {
            using (AuthLogic.UnsafeUser("external"))
            {
                Assert.AreEqual(JapLab, Database.Query<LabelDN>().Count());
                Assert.AreEqual(JapLab, Database.Query<LabelDN>().Count(r => r.IsInGroup(MusicGroups.JapanEntities)));

                Assert.AreEqual(JapLab, Database.RetrieveAll<LabelDN>().Count);
                Assert.AreEqual(JapLab, Database.RetrieveAllLite<LabelDN>().Count);

                using (EntityGroupAuthLogic.DisableQueries())
                {
                    Assert.AreEqual(AllLab, Database.Query<LabelDN>().Count());
                    Assert.AreEqual(AllLab, Database.RetrieveAllLite<LabelDN>().Count);
                    Assert.AreEqual(JapLab, Database.Query<LabelDN>().WhereAllowed().Count());
                }
            }
        }

        [TestMethod]
        public void EntityGroupsBartQueryable()
        {
            using (AuthLogic.UnsafeUser("external"))
            {
                Assert.AreEqual(JapAlb, Database.Query<AlbumDN>().Count());
                Assert.AreEqual(JapAlb, Database.Query<AlbumDN>().Count(r => r.IsInGroup(MusicGroups.JapanEntities)));

                Assert.AreEqual(JapAlb, Database.RetrieveAll<AlbumDN>().Count);
                Assert.AreEqual(JapAlb, Database.RetrieveAllLite<AlbumDN>().Count);

                using (EntityGroupAuthLogic.DisableQueries())
                {
                    Assert.AreEqual(AllAlb, Database.Query<AlbumDN>().Count());
                    Assert.AreEqual(AllAlb, Database.RetrieveAllLite<AlbumDN>().Count);
                    Assert.AreEqual(JapAlb, Database.Query<AlbumDN>().WhereAllowed().Count());
                }
            }
        }

        [TestMethod]
        public void EntityGroupRetrieve()
        {
            using (AuthLogic.UnsafeUser("external"))
            {
                Assert2.Throws<UnauthorizedAccessException>(() => Database.Retrieve<LabelDN>(1));
                using (EntityGroupAuthLogic.DisableQueries())
                {
                    Assert2.Throws<UnauthorizedAccessException>(() => Database.Query<LabelDN>().Single(r => r.Name == "Virgin"));
                }
            }
        }

        [TestMethod]
        public void EntityGroupRetrieveQueryable()
        {
            using (AuthLogic.UnsafeUser("external"))
            {
                Assert2.Throws<UnauthorizedAccessException>(() => Database.Retrieve<AlbumDN>(1)); 
                using (EntityGroupAuthLogic.DisableQueries())
                {
                    Assert2.Throws<UnauthorizedAccessException>(() => Database.Query<AlbumDN>().Single(r => r.Name == "Siamese Dream"));
                }
            }
        }

        [TestMethod]
        public void EntityGroupUpdate()
        {
            using (AuthLogic.UnsafeUser("external"))
            using (Transaction tr = new Transaction())
            {
                Assert.AreEqual(JapLab, Database.Query<LabelDN>().UnsafeUpdate(r => new LabelDN { Name = r.Name + r.Name }));
                Assert.AreEqual(JapAlb, Database.Query<AlbumDN>().UnsafeUpdate(r => new AlbumDN { Name = r.Name + r.Name }));

                //tr.Commit();
            }
        }

        [TestMethod]
        public void EntityGroupDelete()
        {
            using (AuthLogic.UnsafeUser("external"))
            using (Transaction tr = new Transaction())
            {
                Assert.AreEqual(JapAlb, Database.Query<AlbumDN>().UnsafeDelete());
                Assert.AreEqual(JapLab, Database.Query<LabelDN>().UnsafeDelete());
                
                //tr.Commit();
            }
        }

        [TestMethod]
        public void EntityGroupJoin()
        {
            using (AuthLogic.UnsafeUser("external"))
            {
                int coutFast = Database.Query<AlbumDN>().Count();
                int coutSlow = (from lab in Database.Query<LabelDN>()
                                join alb in Database.Query<AlbumDN>() on lab equals alb.Label
                                select lab).Count();
                Assert.AreEqual(coutFast, coutSlow);
            }
        }

        [TestMethod]
        public void EntityGroupSaveOut()
        {
            using (AuthLogic.UnsafeUser("external"))
            using (EntityGroupAuthLogic.DisableQueries())
            using (EntityGroupAuthLogic.DisableRetrieve())
            {
                //Because of target
                {
                    var label = Database.Query<LabelDN>().Single(l => l.Name == "MJJ");
                    label.Owner.Retrieve().Country.Name = "Spain";
                    Assert2.Throws<UnauthorizedAccessException>(() => label.Save());
                }

                {
                    var label = Database.Query<LabelDN>().Single(l => l.Name == "MJJ");
                    label.Owner = Database.Query<LabelDN>().Where(l => l.Name == "Virgin").Select(a => a.ToLite()).Single();
                    Assert2.Throws<UnauthorizedAccessException>(() => label.Save());
                }


                //Because of origin
                {
                    var label = Database.Query<LabelDN>().Single(l => l.Name == "Virgin");
                    label.Country.Name = "Japan";
                    Assert2.Throws<UnauthorizedAccessException>(() => label.Save());
                }

                {
                    var label = Database.Query<LabelDN>().Single(l => l.Name == "WEA International");
                    label.Owner.Retrieve().Name = "Japan";
                    Assert2.Throws<UnauthorizedAccessException>(() => label.Save());
                }

                {
                    var label = Database.Query<LabelDN>().Single(l => l.Name == "WEA International");
                    label.Owner = Database.Query<LabelDN>().Where(l => l.Name == "Sony").Select(a => a.ToLite()).Single();
                    Assert2.Throws<UnauthorizedAccessException>(() => label.Save());
                }
            }
        }
    }
}
