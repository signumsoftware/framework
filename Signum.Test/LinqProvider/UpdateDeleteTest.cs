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
            Starter.Dirty();

            Connection.CurrentLog = new DebugTextWriter();
        }

        [TestMethod]
        public void DeleteAll()
        {
            int count = Database.Query<AlbumDN>().UnsafeDelete();
        }

        [TestMethod]
        public void Delete()
        {
            int count = Database.Query<AlbumDN>().Where(a => a.Year < 1990).UnsafeDelete();
        }

        [TestMethod]
        public void DeleteJoin()
        {
            int count = Database.Query<AlbumDN>().Where(a => ((ArtistDN)a.Author).Dead).UnsafeDelete();
        }


        [TestMethod]
        public void DeleteMListLite()
        {
            int count = Database.MListQuery((ArtistDN a) => a.Friends).UnsafeDelete(); 
        }

        [TestMethod]
        public void DeleteMListEntity()
        {
            int count = Database.MListQuery((BandDN a) => a.Members).UnsafeDelete();
        }

        [TestMethod]
        public void DeleteMListEmbedded()
        {
            int count = Database.MListQuery((AlbumDN a) => a.Songs).UnsafeDelete();
        }


        [TestMethod]
        public void DeleteManual()
        {
            var list = Database.Query<AlbumDN>().Where(a => ((ArtistDN)a.Author).Dead).Select(a => a.ToLite()).ToList();

            Database.DeleteList(list);
        }


        [TestMethod]
        public void UpdateValue()
        {
            int count = Database.Query<AlbumDN>().UnsafeUpdate(a => new AlbumDN { Year = a.Year * 2 });
        }

        [TestMethod]
        public void UpdateValueSqlFunction()
        {
            int count = Database.Query<AlbumDN>().UnsafeUpdate(a => new AlbumDN { Name = a.Name.ToUpper() });
        }

        [TestMethod]
        public void UpdateValueNull()
        {
            int count = Database.Query<NoteWithDateDN>().UnsafeUpdate(a => new NoteWithDateDN { Text = null });
        }

        [TestMethod]
        public void UpdateValueConstant()
        {
            int count = Database.Query<AlbumDN>().Where(a => a.Year < 1990).UnsafeUpdate(a => new AlbumDN { Year = 1990 });
        }

        [TestMethod]
        public void UpdateEnum()
        {
            int count = Database.Query<ArtistDN>().UnsafeUpdate(a => new ArtistDN { Sex = a.Sex == Sex.Female ? Sex.Male : Sex.Female });
        }

        [TestMethod]
        public void UpdateEnumConstant()
        {
            int count = Database.Query<ArtistDN>().UnsafeUpdate(a => new ArtistDN { Sex = Sex.Male });
        }

        [TestMethod]
        public void UpdateEfie()
        {
            SongDN song = new SongDN
            {
                 Name = "Mana Mana",
                 Duration = 184,
            };

            using (Transaction tr = new Transaction())
            {
                int count = Database.Query<AlbumDN>().UnsafeUpdate(a => new AlbumDN { BonusTrack = song });

                Assert.IsFalse(Database.Query<AlbumDN>().Any(a => a.BonusTrack == null));
                Assert.AreEqual(Database.Query<AlbumDN>().Select(a => a.BonusTrack.Name).Distinct().Single(), "Mana Mana");

                tr.Commit();
            }

        }

        [TestMethod]
        public void UpdateEfieNull()
        {
            int count = Database.Query<AlbumDN>().UnsafeUpdate(a => new AlbumDN { BonusTrack = null });

            Assert.IsTrue(Database.Query<AlbumDN>().All(a => a.BonusTrack == null));
            Assert.IsTrue(Database.Query<AlbumDN>().All(a => a.BonusTrack.Name == null));
        }


        [TestMethod]
        public void UpdateFie()
        {
            LabelDN label = Database.Query<LabelDN>().First();

            int count = Database.Query<AlbumDN>().UnsafeUpdate(a => new AlbumDN { Label = label });
        }

        [TestMethod]
        public void UpdateFieToLite()
        {
            LabelDN label = Database.Query<LabelDN>().First();

            int count = Database.Query<LabelDN>().UnsafeUpdate(a => new LabelDN { Owner = label.ToLite() });
        }


        [TestMethod, ExpectedException(typeof(InvalidOperationException), "The entity is New")]
        public void UpdateFieNew()
        {
            LabelDN label = new LabelDN();

            int count = Database.Query<AlbumDN>().UnsafeUpdate(a => new AlbumDN { Label = label });
        }

        [TestMethod]
        public void UpdateFieNull()
        {
            int count = Database.Query<AlbumDN>().UnsafeUpdate(a => new AlbumDN { Label = null });
        }

        [TestMethod]
        public void UpdateIbFie()
        {
            ArtistDN michael = Database.Query<ArtistDN>().Single(a => a.Dead);

            int count = Database.Query<AlbumDN>().UnsafeUpdate(a => new AlbumDN { Author = michael });
        }


        public void UpdateIbNull()
        {
            int count = Database.Query<AlbumDN>().UnsafeUpdate(a => new AlbumDN { Author = null });
        }

        [TestMethod]
        public void UpdateIbaFie()
        {
            ArtistDN michael = Database.Query<ArtistDN>().Single(a => a.Dead);

            int count = Database.Query<NoteWithDateDN>().UnsafeUpdate(n => new NoteWithDateDN {  Target = michael });
        }

        [TestMethod]
        public void UpdateIbaNull()
        {
            int count = Database.Query<NoteWithDateDN>().UnsafeUpdate(n => new NoteWithDateDN { Target = null });
        }

        [TestMethod]
        public void UpdateEmbeddedField()
        {
            int count = Database.Query<AlbumDN>().UnsafeUpdate(a => new AlbumDN { BonusTrack = { Name = a.BonusTrack.Name + " - " } });
        }

        [TestMethod]
        public void UpdateEmbeddedNull()
        {
            int count = Database.Query<AlbumDN>().UnsafeUpdate(a => new AlbumDN { BonusTrack = null });
        }


        [TestMethod]
        public void UpdateMListLite()
        {
            ArtistDN artist = Database.Query<ArtistDN>().First();

            int count = Database.MListQuery((ArtistDN a) => a.Friends).UnsafeUpdate(a => new MListElement<ArtistDN, Lite<ArtistDN>>
            {
                Element = artist.ToLite(),
                Parent = artist
            });

            var list = Database.MListQuery((ArtistDN a) => a.Friends); 
        }

        [TestMethod]
        public void UpdateMListEntity()
        {
            ArtistDN artist = Database.Query<ArtistDN>().First();

            int count = Database.MListQuery((BandDN a) => a.Members).UnsafeUpdate(b => new MListElement<BandDN, ArtistDN>
            {
                Element = artist
            });
        }

        [TestMethod]
        public void UpdateMListEmbedded()
        {
            int count = Database.MListQuery((AlbumDN a) => a.Songs).UnsafeUpdate(b => new MListElement<AlbumDN, SongDN>
            {
                Element = { Duration = 3 }
            });

            var list = Database.MListQuery((AlbumDN a) => a.Songs);
        }

    }
}
