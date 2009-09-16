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

namespace Signum.Test.LinqProvider
{
    /// <summary>
    /// Summary description for LinqProvider
    /// </summary>
    [TestClass]
    public class UpdateDeleteTest
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

        [TestMethod]
        public void DeleteAll()
        {
            Starter.Dirty();

            int count = Database.UnsafeDelete<AlbumDN>(null);
        }

        [TestMethod]
        public void Delete()
        {
            Starter.Dirty();

            int count = Database.UnsafeDelete<AlbumDN>(a => a.Year == 1990);
        }

        [TestMethod]
        public void DeleteJoin()
        {
            Starter.Dirty();

            int count = Database.UnsafeDelete<AlbumDN>(a => ((ArtistDN)a.Author).Dead);
        }


        [TestMethod]
        public void UpdateValue()
        {
            Starter.Dirty();

            int count = Database.UnsafeUpdate<AlbumDN>(a => new AlbumDN { Year = a.Year * 2 }, null);
        }

        [TestMethod]
        public void UpdateConstant()
        {
            Starter.Dirty();

            int count = Database.UnsafeUpdate<AlbumDN>(a => new AlbumDN { Year = 1990 }, a => a.Year < 1990);
        }

        [TestMethod]
        public void UpdateFie()
        {
            Starter.Dirty();

            LabelDN label = Database.Query<LabelDN>().First();

            int count = Database.UnsafeUpdate<AlbumDN>(a => new AlbumDN { Label = label }, null);
        }

        [TestMethod]
        public void UpdateNewFie()
        {
            Starter.Dirty();

            LabelDN label = new LabelDN();

            int count = Database.UnsafeUpdate<AlbumDN>(a => new AlbumDN { Label = label }, null);
        }

        [TestMethod]
        public void UpdateFieIb()
        {
            Starter.Dirty();

            ArtistDN michael = Database.Query<ArtistDN>().Single(a => a.Dead);

            int count = Database.UnsafeUpdate<AlbumDN>(a => new AlbumDN { Author = michael }, null);
        }
    }
}
