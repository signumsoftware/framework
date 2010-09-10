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

namespace Signum.Test.LinqProviderUpdateDelete
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

            int count = Database.Query<AlbumDN>().UnsafeDelete();
        }

        [TestMethod]
        public void Delete()
        {
            Starter.Dirty();

            int count = Database.Query<AlbumDN>().Where(a => a.Year < 1990).UnsafeDelete();
        }

        [TestMethod]
        public void DeleteJoin()
        {
            Starter.Dirty();

            int count = Database.Query<AlbumDN>().Where(a => ((ArtistDN)a.Author).Dead).UnsafeDelete();
        }


        [TestMethod]
        public void UpdateValue()
        {
            Starter.Dirty();

            int count = Database.Query<AlbumDN>().UnsafeUpdate(a => new AlbumDN { Year = a.Year * 2 });
        }

        [TestMethod]
        public void UpdateValueSqlFunction()
        {
            Starter.Dirty();

            int count = Database.Query<AlbumDN>().UnsafeUpdate(a => new AlbumDN { Name = a.Name.ToUpper() });
        }

        [TestMethod]
        public void UpdateValueNull()
        {
            Starter.Dirty();

            int count = Database.Query<NoteDN>().UnsafeUpdate(a => new NoteDN { Text = null });
        }

        [TestMethod]
        public void UpdateValueConstant()
        {
            Starter.Dirty();

            int count = Database.Query<AlbumDN>().Where(a => a.Year < 1990).UnsafeUpdate(a => new AlbumDN { Year = 1990 });
        }

        [TestMethod]
        public void UpdateEnum()
        {
            Starter.Dirty();

            int count = Database.Query<ArtistDN>().UnsafeUpdate(a => new ArtistDN { Sex = a.Sex == Sex.Female ? Sex.Male : Sex.Female });
        }

        [TestMethod]
        public void UpdateEnumConstant()
        {
            Starter.Dirty();

            int count = Database.Query<ArtistDN>().UnsafeUpdate(a => new ArtistDN { Sex = Sex.Male });
        }

        [TestMethod]
        public void UpdateEfie()
        {
            Starter.Dirty();

            SongDN song = new SongDN
            {
                 Name = "Mana Mana",
                 Duration = 184,
            };

            int count = Database.Query<AlbumDN>().UnsafeUpdate(a => new AlbumDN { BonusTrack = song });

            Assert.IsFalse(Database.Query<AlbumDN>().Any(a => a.BonusTrack == null));
            Assert.AreEqual(Database.Query<AlbumDN>().Select(a => a.BonusTrack.Name).Distinct().Single(), "Mana Mana");
        }

        [TestMethod]
        public void UpdateEfieNull()
        {
            Starter.Dirty();

            int count = Database.Query<AlbumDN>().UnsafeUpdate(a => new AlbumDN { BonusTrack = null });

            Assert.IsTrue(Database.Query<AlbumDN>().All(a => a.BonusTrack == null));
            Assert.IsTrue(Database.Query<AlbumDN>().All(a => a.BonusTrack.Name == null));
        }


        [TestMethod]
        public void UpdateFie()
        {
            Starter.Dirty();

            LabelDN label = Database.Query<LabelDN>().First();

            int count = Database.Query<AlbumDN>().UnsafeUpdate(a => new AlbumDN { Label = label });
        }

        [TestMethod]
        public void UpdateFieToLite()
        {
            Starter.Dirty();

            LabelDN label = Database.Query<LabelDN>().First();

            int count = Database.Query<LabelDN>().UnsafeUpdate(a => new LabelDN { Owner = label.ToLite() });
        }


        [TestMethod, ExpectedException(typeof(InvalidOperationException), "The entity is New")]
        public void UpdateFieNew()
        {
            Starter.Dirty();

            LabelDN label = new LabelDN();

            int count = Database.Query<AlbumDN>().UnsafeUpdate(a => new AlbumDN { Label = label });
        }

        [TestMethod]
        public void UpdateFieNull()
        {
            Starter.Dirty();

            int count = Database.Query<AlbumDN>().UnsafeUpdate(a => new AlbumDN { Label = null });
        }

        [TestMethod]
        public void UpdateIbFie()
        {
            Starter.Dirty();

            ArtistDN michael = Database.Query<ArtistDN>().Single(a => a.Dead);

            int count = Database.Query<AlbumDN>().UnsafeUpdate(a => new AlbumDN { Author = michael });
        }


        public void UpdateIbNull()
        {
            Starter.Dirty();

            int count = Database.Query<AlbumDN>().UnsafeUpdate(a => new AlbumDN { Author = null });
        }

        [TestMethod]
        public void UpdateIbaFie()
        {
            Starter.Dirty();

            ArtistDN michael = Database.Query<ArtistDN>().Single(a => a.Dead);

            int count = Database.Query<NoteDN>().UnsafeUpdate(n => new NoteDN {  Target = michael });
        }

        [TestMethod]
        public void UpdateIbaNull()
        {
            Starter.Dirty();

            int count = Database.Query<NoteDN>().UnsafeUpdate(n => new NoteDN { Target = null });
        }

        [TestMethod]
        public void UpdateEmbeddedField()
        {
            Starter.Dirty();

            int count = Database.Query<AlbumDN>().UnsafeUpdate(a => new AlbumDN { BonusTrack = { Name = a.BonusTrack.Name + " - " } });
        }

        [TestMethod]
        public void UpdateEmbeddedNull()
        {
            Starter.Dirty();

            int count = Database.Query<AlbumDN>().UnsafeUpdate(a => new AlbumDN { BonusTrack = null });
        }
    }
}
