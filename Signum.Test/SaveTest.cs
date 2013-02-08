using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signum.Utilities;
using Signum.Engine;
using Signum.Entities;
using Signum.Entities.Reflection;
using Signum.Engine.Operations;
using Signum.Test.Environment;

namespace Signum.Test
{
    [TestClass]
    public class SaveTest
    {
        [ClassInitialize()]
        public static void MyClassInitialize(TestContext testContext)
        {
            Starter.StartAndLoad();
        }

        [TestInitialize]
        public void Initialize()
        {
            Connector.CurrentLogger = new DebugTextWriter();
        }

        [TestMethod]
        public void SaveCycle()
        {
            using (Transaction tr = new Transaction())
            using (OperationLogic.AllowSave<ArtistDN>())
            {
                ArtistDN m = new ArtistDN() { Name = "Michael" };
                ArtistDN f = new ArtistDN() { Name = "Frank" };
                m.Friends.Add(f.ToLiteFat());
                f.Friends.Add(m.ToLiteFat());

                Database.SaveParams(m, f);

                var list = Database.Query<ArtistDN>().Where(a => a == m || a == f).ToList();

                Assert.IsTrue(list[0].Friends.Contains(list[1].ToLite()));
                Assert.IsTrue(list[1].Friends.Contains(list[0].ToLite()));

                //tr.Commit();
            }

        }

        [TestMethod]
        public void SaveSelfCycle()
        {
            using (Transaction tr = new Transaction())
            using (OperationLogic.AllowSave<ArtistDN>())
            {
                ArtistDN m = new ArtistDN() { Name = "Michael" };
                m.Friends.Add(m.ToLiteFat());

                m.Save();

                var m2 = m.ToLite().RetrieveAndForget();

                Assert.IsTrue(m2.Friends.Contains(m2.ToLite()));

                //tr.Commit();
            }

        }

        [TestMethod]
        public void SaveMany()
        {
            using (Transaction tr = new Transaction())
            using (OperationLogic.AllowSave<ArtistDN>())
            {
                var prev =  Database.Query<ArtistDN>().Count();

                Type[] types = typeof(int).Assembly.GetTypes().Where(a => a.Name.Length > 3 && a.Name.StartsWith("A")).ToArray();

                var list = types.Select(t => new ArtistDN() { Name = t.Name }).ToList();

                list.SaveList();

                Assert.AreEqual(prev + types.Length, Database.Query<ArtistDN>().Count());

                list.ForEach(a => a.Name += "Updated");

                list.SaveList();

                //tr.Commit();
            }
        }

        [TestMethod]
        public void SaveMList()
        {
            using (Transaction tr = new Transaction())
            using (OperationLogic.AllowSave<AlbumDN>())
            using (OperationLogic.AllowSave<ArtistDN>())
            {
                var prev = Database.MListQuery((AlbumDN a) => a.Songs).Count();

                Type[] types = typeof(int).Assembly.GetTypes().Where(a => a.Name.Length > 3 && a.Name.StartsWith("A")).ToArray();

                AlbumDN album = new AlbumDN()
                {
                    Name = "System Greatest hits",
                    Author = new ArtistDN { Name = ".Net Framework" },
                    Year = 2001,
                    Songs = types.Select(t => new SongDN() { Name = t.Name }).ToMList()
                }.Save();

                Assert2.AssertAll(GraphExplorer.FromRoot(album), a => a.SelfModified == false && a.Modified == null);

                Assert.AreEqual(prev + types.Length, Database.MListQuery((AlbumDN a) => a.Songs).Count());

                album.Name += "Updated";

                album.Save();

                album.Songs.ForEach(a => a.Name = "Updated");

                album.Save();

                //tr.Commit();
            }
        }

        [TestMethod]
        public void SaveManyMList()
        {
            using (Transaction tr = new Transaction())
            using (OperationLogic.AllowSave<AlbumDN>())
            using (OperationLogic.AllowSave<ArtistDN>())
            {
                var prev = Database.MListQuery((AlbumDN a) => a.Songs).Count();

                List<AlbumDN> albums = 0.To(16).Select(i => new AlbumDN()
                {
                    Name = "System Greatest hits {0}".Formato(i),
                    Author = new ArtistDN { Name = ".Net Framework" },
                    Year = 2001,
                    Songs =  { new SongDN { Name = "Compilation {0}".Formato(i) }}
                }).ToList();

                albums.SaveList();

                Assert2.AssertAll(GraphExplorer.FromRoots(albums), a => a.SelfModified == false && a.Modified == null);

                Assert.AreEqual(prev + 16, Database.MListQuery((AlbumDN a) => a.Songs).Count());

                albums.ForEach(a => a.Name += "Updated");

                albums.SaveList();

                albums.ForEach(a => a.Songs.ForEach(s => s.Name = "Updated"));

                albums.SaveList();

                //tr.Commit();
            }

        }
    }
}
