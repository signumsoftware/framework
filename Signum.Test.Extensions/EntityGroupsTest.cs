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
using System.Xml.Linq;
using Signum.Engine.Exceptions;
using Signum.Engine.Operations;

namespace Signum.Test.Extensions
{
    [TestClass]
    public class TypeConditionTest
    {

        [ClassInitialize()]
        public static void MyClassInitialize(TestContext testContext)
        {
            Starter.StartAndLoad(UserConnections.Replace(Settings.Default.ConnectionString));
        }


        [TestInitialize]
        public void Initialize()
        {
            Connector.CurrentLogger = new DebugTextWriter();
        }

        const int JapLab = 2;
        const int AllLab = 7;

        const int JapAlb = 5;
        const int AllAlb = 12; 


        [TestMethod]
        public void TypeConditionAuthDisable()
        {
            using (AuthLogic.Disable())
            {
                Assert.AreEqual(AllLab, Database.Query<LabelDN>().Count());
                Assert.AreEqual(JapLab, Database.Query<LabelDN>().Count(r => r.InCondition(MusicGroups.JapanEntities)));

                Assert.AreEqual(AllLab, Database.RetrieveAll<LabelDN>().Count);
                Assert.AreEqual(AllLab, Database.RetrieveAllLite<LabelDN>().Count);

                Assert.AreEqual(AllLab, Database.Query<LabelDN>().WhereAllowed().Count());
            }
        }

        [TestMethod]
        public void TypeConditionQueryableAuthDisable()
        {
            using (AuthLogic.Disable())
            {
                Assert.AreEqual(AllAlb, Database.Query<AlbumDN>().Count());
                Assert.AreEqual(JapAlb, Database.Query<AlbumDN>().Count(r => r.InCondition(MusicGroups.JapanEntities)));

                Assert.AreEqual(AllAlb, Database.RetrieveAll<AlbumDN>().Count);
                Assert.AreEqual(AllAlb, Database.RetrieveAllLite<AlbumDN>().Count);

                Assert.AreEqual(AllAlb, Database.Query<AlbumDN>().WhereAllowed().Count());
            }
        }

        [TestMethod]
        public void TypeConditionExternal()
        {
            using (AuthLogic.UnsafeUserSession("external"))
            {
                Assert.AreEqual(JapLab, Database.Query<LabelDN>().Count());
                Assert.AreEqual(JapLab, Database.Query<LabelDN>().Count(r => r.InCondition(MusicGroups.JapanEntities)));

                Assert.AreEqual(JapLab, Database.RetrieveAll<LabelDN>().Count);
                Assert.AreEqual(JapLab, Database.RetrieveAllLite<LabelDN>().Count);

                using (TypeAuthLogic.DisableQueryFilter())
                {
                    Assert.AreEqual(AllLab, Database.Query<LabelDN>().Count());
                    Assert.AreEqual(AllLab, Database.RetrieveAllLite<LabelDN>().Count);
                    Assert.AreEqual(JapLab, Database.Query<LabelDN>().WhereAllowed().Count());
                }
            }
        }

        [TestMethod]
        public void TypeConditionExternalQueryable()
        {
            using (AuthLogic.UnsafeUserSession("external"))
            {
                Assert.AreEqual(JapAlb, Database.Query<AlbumDN>().Count());
                Assert.AreEqual(JapAlb, Database.Query<AlbumDN>().Count(r => r.InCondition(MusicGroups.JapanEntities)));

                Assert.AreEqual(JapAlb, Database.RetrieveAll<AlbumDN>().Count);
                Assert.AreEqual(JapAlb, Database.RetrieveAllLite<AlbumDN>().Count);

                using (TypeAuthLogic.DisableQueryFilter())
                {
                    Assert.AreEqual(AllAlb, Database.Query<AlbumDN>().Count());
                    Assert.AreEqual(AllAlb, Database.RetrieveAllLite<AlbumDN>().Count);
                    Assert.AreEqual(JapAlb, Database.Query<AlbumDN>().WhereAllowed().Count());
                }
            }
        }

        [TestMethod]
        public void TypeConditionRetrieve()
        {
            using (AuthLogic.UnsafeUserSession("external"))
            {
                Assert2.Throws<EntityNotFoundException>(() => Database.Retrieve<LabelDN>(1));
                using (TypeAuthLogic.DisableQueryFilter())
                {
                    Database.Query<LabelDN>().SingleEx(r => r.Name == "Virgin");
                }
            }
        }


        [TestMethod]
        public void TypeConditionUpdate()
        {
            using (AuthLogic.UnsafeUserSession("external"))
            using (Transaction tr = new Transaction())
            {
                Assert.AreEqual(JapLab, Database.Query<LabelDN>().UnsafeUpdate(r => new LabelDN { Name = r.Name + r.Name }));
                Assert.AreEqual(JapAlb, Database.Query<AlbumDN>().UnsafeUpdate(r => new AlbumDN { Name = r.Name + r.Name }));

                //tr.Commit();
            }
        }

        [TestMethod]
        public void TypeConditionDelete()
        {
            using (AuthLogic.UnsafeUserSession("external"))
            using (Transaction tr = new Transaction())
            {
                Assert.AreEqual(JapAlb, Database.Query<AlbumDN>().UnsafeDelete());
                Assert.AreEqual(JapLab, Database.Query<LabelDN>().UnsafeDelete());
                
                //tr.Commit();
            }
        }

        [TestMethod]
        public void TypeConditionJoin()
        {
            using (AuthLogic.UnsafeUserSession("external"))
            {
                int coutFast = Database.Query<AlbumDN>().Count();
                int coutSlow = (from lab in Database.Query<LabelDN>()
                                join alb in Database.Query<AlbumDN>() on lab equals alb.Label
                                select lab).Count();
                Assert.AreEqual(coutFast, coutSlow);
            }
        }

        [TestMethod]
        public void TypeConditionSaveOut()
        {
            using (AuthLogic.UnsafeUserSession("external"))
            using (OperationLogic.AllowSave<LabelDN>())
            using (TypeAuthLogic.DisableQueryFilter())
            {
                //Because of target
                {
                    var label = Database.Query<LabelDN>().SingleEx(l => l.Name == "MJJ");
                    label.Owner.Retrieve().Country.Name = "Spain";
                    Assert2.Throws<UnauthorizedAccessException>(() => label.Save());
                }

                {
                    var label = Database.Query<LabelDN>().SingleEx(l => l.Name == "MJJ");
                    label.Owner = Database.Query<LabelDN>().Where(l => l.Name == "Virgin").Select(a => a.ToLite()).SingleEx();
                    Assert2.Throws<UnauthorizedAccessException>(() => label.Save());
                }


                //Because of origin
                {
                    var label = Database.Query<LabelDN>().SingleEx(l => l.Name == "Virgin");
                    label.Country.Name = "Japan Empire";
                    Assert2.Throws<UnauthorizedAccessException>(() => label.Save());
                }

                {
                    var label = Database.Query<LabelDN>().SingleEx(l => l.Name == "WEA International");
                    label.Owner.Retrieve().Name = "Japan Empire";
                    Assert2.Throws<UnauthorizedAccessException>(() => label.Save());
                }

                {
                    var label = Database.Query<LabelDN>().SingleEx(l => l.Name == "WEA International");
                    label.Owner = Database.Query<LabelDN>().Where(l => l.Name == "Sony").Select(a => a.ToLite()).SingleEx();
                    Assert2.Throws<UnauthorizedAccessException>(() => label.Save());
                }
            }
        }

        //[TestMethod]
        //public void ImportAuthRules()
        //{
        //    AuthLogic.GloballyEnabled = false;
        //    var rules = AuthLogic.ImportRulesScript(XDocument.Load(@"C:\Users\olmo.SIGNUMS\Desktop\AuthRules.xml")); 
        //}

    }
}
