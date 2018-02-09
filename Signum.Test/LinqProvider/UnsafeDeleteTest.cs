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
using Signum.Test.Environment;
using System.Data.SqlClient;

namespace Signum.Test.LinqProviderUpdateDelete
{
    /// <summary>
    /// Summary description for LinqProvider
    /// </summary>
    [TestClass]
    public class UnsafeDeleteTest
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
        public void DeleteAll()
        {
            using (Transaction tr = new Transaction())
            {
                int count = Database.Query<AlbumEntity>().UnsafeDelete();

                //tr.Commit();
            }

        }

        [TestMethod]
        public void Delete()
        {
            using (Transaction tr = new Transaction())
            {
                int count = Database.Query<AlbumEntity>().Where(a => a.Year < 1990).UnsafeDelete();

                //tr.Commit();
            }
        }

        [TestMethod]
        public void DeleteChunks()
        {
            using (Transaction tr = new Transaction())
            {
                int count = Database.Query<AlbumEntity>().UnsafeDeleteChunks(2);

                //tr.Commit();
            }
        }

        [TestMethod]
        public void DeleteJoin()
        {
            using (Transaction tr = new Transaction())
            {
                int count = Database.Query<AlbumEntity>().Where(a => ((ArtistEntity)a.Author).Dead).UnsafeDelete();
                //tr.Commit();
            }
        }


        [TestMethod]
        public void DeleteMListLite()
        {
            using (Transaction tr = new Transaction())
            {
                int count = Database.MListQuery((ArtistEntity a) => a.Friends).UnsafeDeleteMList();
                //tr.Commit();
            }
        }

        [TestMethod]
        public void DeleteMListEntity()
        {
            using (Transaction tr = new Transaction())
            {
                int count = Database.MListQuery((BandEntity a) => a.Members).UnsafeDeleteMList();

                //tr.Commit();
            }
        }

        [TestMethod]
        public void DeleteMListEmbedded()
        {
            using (Transaction tr = new Transaction())
            {
                int count = Database.MListQuery((AlbumEntity a) => a.Songs).UnsafeDeleteMList();

                //tr.Commit();
            }
        }


        [TestMethod]
        public void DeleteManual()
        {
            using (Transaction tr = new Transaction())
            {
                var list = Database.Query<AlbumEntity>().Where(a => ((ArtistEntity)a.Author).Dead).Select(a => a.ToLite()).ToList();

                Database.DeleteList(list);
                //tr.Commit();
            }
        }

        [TableName("#MyView")]
        class MyTempView : IView
        {
            [ViewPrimaryKey]
            public int MyId { get; set; }
        }

        [TestMethod]
        public void UnsafeDeleteMyView()
        {
            using (Transaction tr = new Transaction())
            {
                Administrator.CreateTemporaryTable<MyTempView>();

                Database.Query<ArtistEntity>().UnsafeInsertView(a => new MyTempView { MyId = (int)a.Id });

                Database.View<MyTempView>().Where(a=>a.MyId > 1).UnsafeDeleteView();

                tr.Commit();
            }

        }
    }
}
