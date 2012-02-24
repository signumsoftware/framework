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


        [TestMethod]
        public void UpdateValue()
        {
            using (Transaction tr = new Transaction())
            {
                int count = Database.Query<AlbumDN>().UnsafeUpdate(a => new AlbumDN { Year = a.Year * 2 });
                //tr.Commit();
            }

        }

        [TestMethod]
        public void UpdateValueSqlFunction()
        {
            using (Transaction tr = new Transaction())
            {
                int count = Database.Query<AlbumDN>().UnsafeUpdate(a => new AlbumDN { Name = a.Name.ToUpper() });
                //tr.Commit();
            }

        }

        [TestMethod]
        public void UpdateValueNull()
        {
            using (Transaction tr = new Transaction())
            {
                int count = Database.Query<NoteWithDateDN>().UnsafeUpdate(a => new NoteWithDateDN { Text = null });
                //tr.Commit();
            }

        }

        [TestMethod]
        public void UpdateValueConstant()
        {
            using (Transaction tr = new Transaction())
            {
                int count = Database.Query<AlbumDN>().Where(a => a.Year < 1990).UnsafeUpdate(a => new AlbumDN { Year = 1990 });
                //tr.Commit();
            }

        }

        [TestMethod]
        public void UpdateEnum()
        {
            using (Transaction tr = new Transaction())
            {
                int count = Database.Query<ArtistDN>().UnsafeUpdate(a => new ArtistDN { Sex = a.Sex == Sex.Female ? Sex.Male : Sex.Female });
                //tr.Commit();
            }

        }

        [TestMethod]
        public void UpdateEnumConstant()
        {
            using (Transaction tr = new Transaction())
            {
                int count = Database.Query<ArtistDN>().UnsafeUpdate(a => new ArtistDN { Sex = Sex.Male });
                //tr.Commit();
            }

        }

        [TestMethod]
        public void UpdateEfie()
        {
            using (Transaction tr = new Transaction())
            {
                SongDN song = new SongDN
                {
                    Name = "Mana Mana",
                    Duration = TimeSpan.FromSeconds(184),
                };


                int count = Database.Query<AlbumDN>().UnsafeUpdate(a => new AlbumDN { BonusTrack = song });

                Assert.IsFalse(Database.Query<AlbumDN>().Any(a => a.BonusTrack == null));
                Assert.AreEqual(Database.Query<AlbumDN>().Select(a => a.BonusTrack.Name).Distinct().SingleEx(), "Mana Mana");

                //tr.Commit();
            }

        }

        [TestMethod]
        public void UpdateEfieNull()
        {
            using (Transaction tr = new Transaction())
            {
                int count = Database.Query<AlbumDN>().UnsafeUpdate(a => new AlbumDN { BonusTrack = null });

                Assert.IsTrue(Database.Query<AlbumDN>().All(a => a.BonusTrack == null));
                Assert.IsTrue(Database.Query<AlbumDN>().All(a => a.BonusTrack.Name == null));
                //tr.Commit();
            }

        }


        [TestMethod]
        public void UpdateFie()
        {
            using (Transaction tr = new Transaction())
            {
                LabelDN label = Database.Query<LabelDN>().FirstEx();

                int count = Database.Query<AlbumDN>().UnsafeUpdate(a => new AlbumDN { Label = label });
                //tr.Commit();
            }

        }

        [TestMethod]
        public void UpdateFieToLite()
        {
            using (Transaction tr = new Transaction())
            {
                LabelDN label = Database.Query<LabelDN>().FirstEx();

                int count = Database.Query<LabelDN>().UnsafeUpdate(a => new LabelDN { Owner = label.ToLite() });
                //tr.Commit();
            }

        }


        [TestMethod, ExpectedException(typeof(InvalidOperationException), "The entity is New")]
        public void UpdateFieNew()
        {
            using (Transaction tr = new Transaction())
            {
                LabelDN label = new LabelDN();

                int count = Database.Query<AlbumDN>().UnsafeUpdate(a => new AlbumDN { Label = label });
                //tr.Commit();
            }

        }

        [TestMethod]
        public void UpdateFieNull()
        {
            using (Transaction tr = new Transaction())
            {
                int count = Database.Query<AlbumDN>().UnsafeUpdate(a => new AlbumDN { Label = null });
                //tr.Commit();
            }

        }

        [TestMethod]
        public void UpdateIbFie()
        {
            using (Transaction tr = new Transaction())
            {
                ArtistDN michael = Database.Query<ArtistDN>().SingleEx(a => a.Dead);

                int count = Database.Query<AlbumDN>().UnsafeUpdate(a => new AlbumDN { Author = michael });
                //tr.Commit();
            }

        }


        public void UpdateIbNull()
        {
            using (Transaction tr = new Transaction())
            {
                int count = Database.Query<AlbumDN>().UnsafeUpdate(a => new AlbumDN { Author = null });
                //tr.Commit();
            }

        }

        [TestMethod]
        public void UpdateIbaFie()
        {
            using (Transaction tr = new Transaction())
            {
                ArtistDN michael = Database.Query<ArtistDN>().SingleEx(a => a.Dead);

                int count = Database.Query<NoteWithDateDN>().UnsafeUpdate(n => new NoteWithDateDN { Target = michael });
                //tr.Commit();
            }

        }

        [TestMethod]
        public void UpdateIbaNull()
        {
            using (Transaction tr = new Transaction())
            {
                int count = Database.Query<NoteWithDateDN>().UnsafeUpdate(n => new NoteWithDateDN { Target = null });
                //tr.Commit();
            }

        }

        [TestMethod]
        public void UpdateEmbeddedField()
        {
            using (Transaction tr = new Transaction())
            {
                int count = Database.Query<AlbumDN>().UnsafeUpdate(a => new AlbumDN { BonusTrack = { Name = a.BonusTrack.Name + " - " } });
                //tr.Commit();
            }

        }

        [TestMethod]
        public void UpdateEmbeddedNull()
        {
            using (Transaction tr = new Transaction())
            {
                int count = Database.Query<AlbumDN>().UnsafeUpdate(a => new AlbumDN { BonusTrack = null });
                //tr.Commit();
            }

        }


        [TestMethod]
        public void UpdateMListLite()
        {
            using (Transaction tr = new Transaction())
            {
                ArtistDN artist = Database.Query<ArtistDN>().FirstEx();

                int count = Database.MListQuery((ArtistDN a) => a.Friends).UnsafeUpdate(a => new MListElement<ArtistDN, Lite<ArtistDN>>
                {
                    Element = artist.ToLite(),
                    Parent = artist
                });

                var list = Database.MListQuery((ArtistDN a) => a.Friends);
                //tr.Commit();
            }

        }

        [TestMethod]
        public void UpdateMListEntity()
        {
            using (Transaction tr = new Transaction())
            {
                ArtistDN artist = Database.Query<ArtistDN>().FirstEx();

                int count = Database.MListQuery((BandDN a) => a.Members).UnsafeUpdate(b => new MListElement<BandDN, ArtistDN>
                {
                    Element = artist
                });
                //tr.Commit();
            }

        }

        [TestMethod]
        public void UpdateMListEmbedded()
        {
            using (Transaction tr = new Transaction())
            {
                int count = Database.MListQuery((AlbumDN a) => a.Songs).UnsafeUpdate(b => new MListElement<AlbumDN, SongDN>
                {
                    Element = { Seconds = 3 }
                });

                var list = Database.MListQuery((AlbumDN a) => a.Songs);
                //tr.Commit();
            }
        }
    }
}
