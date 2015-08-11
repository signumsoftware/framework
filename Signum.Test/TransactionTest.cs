using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signum.Engine;
using Signum.Entities.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Utilities.Reflection;
using Signum.Utilities;
using System.Reflection;
using Signum.Engine.Linq;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using System.Data;

namespace Signum.Test
{
    [TestClass]
    public class TransactionTest
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

        void SetName(string newName)
        {
            Database.Query<ArtistEntity>().Where(a => a.Id == 1).UnsafeUpdate(a => new ArtistEntity { Name = newName });
        }

        string GetName()
        {
            return Database.Query<ArtistEntity>().Where(a => a.Id == 1).Select(a => a.Name).Single();
        }

        [TestMethod]
        public void NoTransaction()
        {
            SetName("Mickey");

            Assert.AreEqual("Mickey", GetName());

            SetName("Mouse");

            Assert.AreEqual("Mouse", GetName());

            Starter.Dirty();
        }

        [TestMethod]
        public void Commit()
        {
            using (Transaction tr = new Transaction())
            {
                SetName("Mouse");

                Assert.AreEqual("Mouse", GetName());
                tr.Commit();
            }

            Assert.AreEqual("Mouse", GetName());

            Starter.Dirty();
        }

        [TestMethod]
        public void Rollback()
        {
            SetName("Mickey");

            Assert.AreEqual("Mickey", GetName());

            using (Transaction tr = new Transaction())
            {
                SetName("Mouse");

                Assert.AreEqual("Mouse", GetName());
                //tr.Commit();
            }

            Assert.AreEqual("Mickey", GetName());
               
            Starter.Dirty();
        }

        [TestMethod]
        public void CommitNested()
        {
            SetName("Mickey");

            Assert.AreEqual("Mickey", GetName());

            using (Transaction tr = new Transaction())
            {
                using (Transaction tr2 = new Transaction())
                {
                    SetName("Mouse");

                    Assert.AreEqual("Mouse", GetName());
                    tr2.Commit();
                }
                tr.Commit();
            }

            Assert.AreEqual("Mouse", GetName());

            Starter.Dirty();
        }

        [TestMethod]
        public void RollbackNested()
        {
            SetName("Mickey");

            Assert.AreEqual("Mickey", GetName());

            using (Transaction tr = new Transaction())
            {
                SetName("Minie");

                Assert.AreEqual("Minie", GetName());

                using (Transaction tr2 = new Transaction())
                {
                    SetName("Mouse");

                    Assert.AreEqual("Mouse", GetName());
                    //tr.Commit();
                }
                Assert2.Throws<InvalidOperationException>(() => tr.Commit());
            }

            Assert.AreEqual("Mickey", GetName());

            Starter.Dirty();
        }

        [TestMethod]
        public void IndependantBlocking()
        {
            Administrator.SetSnapshotIsolation(false);
            
            using (new CommandTimeoutScope(3))
            {
                SetName("Mickey");

                Assert.AreEqual("Mickey", GetName());

                using (Transaction tr = new Transaction())
                {
                    SetName("Minie");

                    Assert.AreEqual("Minie", GetName());

                    using (Transaction tr2 = new Transaction(true))
                    {
                        Assert2.Throws<TimeoutException>(() => GetName()); //Timeout reading 

                        return;
                    }
                }
            }
        }


        [TestMethod]
        public void IndependantSnapshot()
        {
            try
            {
                Administrator.SetSnapshotIsolation(true);
                using (new CommandTimeoutScope(3))
                {
                    SetName("Mickey");

                    Assert.AreEqual("Mickey", GetName());

                    using (Transaction tr = new Transaction(true, IsolationLevel.Snapshot))
                    {
                        SetName("Minie");

                        Assert.AreEqual("Minie", GetName());

                        using (Transaction tr2 = new Transaction(true, IsolationLevel.Snapshot))
                        {
                            Assert.AreEqual("Mickey", GetName()); //Independant transaction

                            Assert2.Throws<TimeoutException>(() => SetName("Mouse")); //Timeout writing 

                            return;
                        }
                    }
                }
            }
            finally
            {
                Administrator.SetSnapshotIsolation(false);
            }
        }

        [TestMethod]
        public void NamedCommit()
        {
            using (Transaction tr = new Transaction())
            {
                SetName("Mickey");

                Assert.AreEqual("Mickey", GetName());

                using (Transaction tr2 = new Transaction("hola"))
                {
                    SetName("Minie");

                    Assert.AreEqual("Minie", GetName());

                    tr2.Commit(); 
                }

                tr.Commit();
            }

            Assert.AreEqual("Minie", GetName());
        }

        [TestMethod]
        public void NamedRollback()
        {
            using (Transaction tr = new Transaction())
            {
                SetName("Mickey");

                Assert.AreEqual("Mickey", GetName());

                using (Transaction tr2 = new Transaction("hola"))
                {
                    SetName("Minie");

                    Assert.AreEqual("Minie", GetName());

                    //tr2.Commit();
                }

                tr.Commit();
            }

            Assert.AreEqual("Mickey", GetName());
        }

        [TestMethod]
        public void NestedNamedCommit()
        {
            using (Transaction tr = new Transaction())
            {
                SetName("Mickey");

                Assert.AreEqual("Mickey", GetName());

                using (Transaction tr2 = new Transaction("hola"))
                {
                    SetName("Minie");

                    Assert.AreEqual("Minie", GetName());

                    using (Transaction tr3 = new Transaction())
                    {
                        SetName("Mouse");

                        Assert.AreEqual("Mouse", GetName());

                        tr3.Commit();
                    }

                    tr2.Commit();
                }

                tr.Commit();
            }

            Assert.AreEqual("Mouse", GetName());
        }

        [TestMethod]
        public void NestedNamedRollback()
        {
            using (Transaction tr = new Transaction())
            {
                SetName("Mickey");

                Assert.AreEqual("Mickey", GetName());

                using (Transaction tr2 = new Transaction("hola"))
                {
                    SetName("Minie");

                    Assert.AreEqual("Minie", GetName());

                    using (Transaction tr3 = new Transaction())
                    {
                        SetName("Mouse");

                        Assert.AreEqual("Mouse", GetName());

                        //tr3.Commit();
                    }

                    Assert2.Throws<InvalidOperationException>(() =>
                    {
                        using (Transaction tr3 = new Transaction())
                        {
                            SetName("Mouse");

                            Assert.AreEqual("Mouse", GetName());

                            tr3.Commit();
                        }

                        tr2.Commit();
                    });
                }

                tr.Commit();
            }

            Assert.AreEqual("Mickey", GetName());
        }

        [TestMethod]
        public void NamedNamedRollbackRollback()
        {
            using (Transaction tr = new Transaction())
            {
                SetName("Mickey");

                Assert.AreEqual("Mickey", GetName());

                using (Transaction tr2 = new Transaction("hola"))
                {
                    SetName("Minie");

                    Assert.AreEqual("Minie", GetName());

                    using (Transaction tr3 = new Transaction("dola"))
                    {
                        SetName("Mouse");

                        Assert.AreEqual("Mouse", GetName());

                        //tr3.Commit();
                    }

                    Assert.AreEqual("Minie", GetName());

                    //tr2.Commit();
                }

                tr.Commit();
            }

            Assert.AreEqual("Mickey", GetName());
        }

        [TestMethod]
        public void NamedNamedRollbackCommit()
        {
            using (Transaction tr = new Transaction())
            {
                SetName("Mickey");

                Assert.AreEqual("Mickey", GetName());

                using (Transaction tr2 = new Transaction("hola"))
                {
                    SetName("Minie");

                    Assert.AreEqual("Minie", GetName());

                    using (Transaction tr3 = new Transaction("dola"))
                    {
                        SetName("Mouse");

                        Assert.AreEqual("Mouse", GetName());

                        //tr3.Commit();
                    }

                    Assert.AreEqual("Minie", GetName());

                    tr2.Commit();
                }

                tr.Commit();
            }

            Assert.AreEqual("Minie", GetName());
        }

        [TestMethod]
        public void NamedNamedCommitCommit()
        {
            using (Transaction tr = new Transaction())
            {
                SetName("Mickey");

                Assert.AreEqual("Mickey", GetName());

                using (Transaction tr2 = new Transaction("hola"))
                {
                    SetName("Minie");

                    Assert.AreEqual("Minie", GetName());

                    using (Transaction tr3 = new Transaction("dola"))
                    {
                        SetName("Mouse");

                        Assert.AreEqual("Mouse", GetName());

                        tr3.Commit();
                    }

                    Assert.AreEqual("Mouse", GetName());

                    tr2.Commit();
                }

                tr.Commit();
            }

            Assert.AreEqual("Mouse", GetName());
        }
    }
}
