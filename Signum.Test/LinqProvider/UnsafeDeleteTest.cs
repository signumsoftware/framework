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
            Starter.StartAndLoad();
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
                int count = Database.Query<AlbumDN>().UnsafeDelete();

                //tr.Commit();
            }

        }

        [TestMethod]
        public void Delete()
        {
            using (Transaction tr = new Transaction())
            {
                int count = Database.Query<AlbumDN>().Where(a => a.Year < 1990).UnsafeDelete();

                //tr.Commit();
            }
        }

        [TestMethod]
        public void DeleteJoin()
        {
            using (Transaction tr = new Transaction())
            {
                int count = Database.Query<AlbumDN>().Where(a => ((ArtistDN)a.Author).Dead).UnsafeDelete();
                //tr.Commit();
            }
        }


        [TestMethod]
        public void DeleteMListLite()
        {
            using (Transaction tr = new Transaction())
            {
                int count = Database.MListQuery((ArtistDN a) => a.Friends).UnsafeDelete();
                //tr.Commit();
            }
        }

        [TestMethod]
        public void DeleteMListEntity()
        {
            using (Transaction tr = new Transaction())
            {
                int count = Database.MListQuery((BandDN a) => a.Members).UnsafeDelete();

                //tr.Commit();
            }
        }

        [TestMethod]
        public void DeleteMListEmbedded()
        {
            using (Transaction tr = new Transaction())
            {
                int count = Database.MListQuery((AlbumDN a) => a.Songs).UnsafeDelete();

                //tr.Commit();
            }
        }


        [TestMethod]
        public void DeleteManual()
        {
            using (Transaction tr = new Transaction())
            {
                var list = Database.Query<AlbumDN>().Where(a => ((ArtistDN)a.Author).Dead).Select(a => a.ToLite()).ToList();

                Database.DeleteList(list);
                //tr.Commit();
            }

        }
    }
}
