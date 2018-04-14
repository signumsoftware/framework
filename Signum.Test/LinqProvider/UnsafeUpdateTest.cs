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
using Signum.Engine.Maps;

namespace Signum.Test.LinqProviderUpdateDelete
{
    /// <summary>
    /// Summary description for LinqProvider
    /// </summary>
    [TestClass]
    public class UpdateUpdateTest
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
            Schema.Current.EntityEvents<LabelEntity>().PreUnsafeUpdate += (update, query) => null;
            Schema.Current.EntityEvents<AlbumEntity>().PreUnsafeUpdate += (update, query) => null;
            Schema.Current.EntityEvents<BandEntity>().PreUnsafeUpdate += (update, query) => null;
            Schema.Current.EntityEvents<ArtistEntity>().PreUnsafeUpdate += (update, query) => null;
        }

        [TestMethod]
        public void UpdateValue()
        {
            using (Transaction tr = new Transaction())
            {
                int count = Database.Query<AlbumEntity>().UnsafeUpdate().Set(a => a.Year, a => a.Year * 2).Execute();
                //tr.Commit();
            }

        }

        [TestMethod]
        public void UpdateValueSqlFunction()
        {
            using (Transaction tr = new Transaction())
            {
                int count = Database.Query<AlbumEntity>().UnsafeUpdate().Set(a => a.Name, a => a.Name.ToUpper()).Execute();
                //tr.Commit();
            }

        }

        [TestMethod]
        public void UpdateValueNull()
        {
            using (Transaction tr = new Transaction())
            {
                int count = Database.Query<NoteWithDateEntity>().UnsafeUpdate().Set(a => a.Text, a => null).Execute();
                //tr.Commit();
            }

        }

        [TestMethod]
        public void UpdateValueConstant()
        {
            using (Transaction tr = new Transaction())
            {
                int count = Database.Query<AlbumEntity>().Where(a => a.Year < 1990)
                    .UnsafeUpdate().Set(a => a.Year, a => 1990).Execute();
                //tr.Commit();
            }

        }

        [TestMethod]
        public void UpdateEnumConstant()
        {
            using (Transaction tr = new Transaction())
            {
                int count = Database.Query<ArtistEntity>().UnsafeUpdate()
                .Set(a => a.Sex, a => Sex.Male)
                .Execute();
                //tr.Commit();
            }
        }

        [TestMethod]
        public void UpdateEnum()
        {
            using (Transaction tr = new Transaction())
            {
                int count = Database.Query<ArtistEntity>().UnsafeUpdate()
                .Set(a => a.Sex, a => a.Sex == Sex.Female ? Sex.Male : Sex.Female)
                .Execute();
                //tr.Commit();
            }
        }

     

        [TestMethod]
        public void UpdateEfie()
        {
            using (Transaction tr = new Transaction())
            {
                SongEmbedded song = new SongEmbedded
                {
                    Name = "Mana Mana",
                    Duration = TimeSpan.FromSeconds(184),
                };


                int count = Database.Query<AlbumEntity>().UnsafeUpdate()
                .Set(a => a.BonusTrack, a => song)
                .Execute();

                Assert.IsFalse(Database.Query<AlbumEntity>().Any(a => a.BonusTrack == null));
                Assert.AreEqual(Database.Query<AlbumEntity>().Select(a => a.BonusTrack.Name).Distinct().SingleEx(), "Mana Mana");

                //tr.Commit();
            }

        }

        [TestMethod]
        public void UpdateEfieNull()
        {
            using (Transaction tr = new Transaction())
            {
                int count = Database.Query<AlbumEntity>().UnsafeUpdate()
                    .Set(a => a.BonusTrack, a => null)
                    .Execute();

                Assert.IsTrue(Database.Query<AlbumEntity>().All(a => a.BonusTrack == null));
                Assert.IsTrue(Database.Query<AlbumEntity>().All(a => a.BonusTrack.Name == null));
                //tr.Commit();
            }
        }

        [TestMethod]
        public void UpdateEfieConditional()
        {
            using (Transaction tr = new Transaction())
            {
                SongEmbedded song = new SongEmbedded
                {
                    Name = "Mana Mana",
                    Duration = TimeSpan.FromSeconds(184),
                };


                int count = Database.Query<AlbumEntity>().UnsafeUpdate()
                    .Set(a => a.BonusTrack, a => (int)a.Id % 2 == 0 ? song : null)
                .Execute();

                Assert.IsTrue(Database.Query<AlbumEntity>().All(a => (int)a.Id % 2 == 0 ? a.BonusTrack.Name == "Mana Mana" : a.BonusTrack.Name == null));

                //tr.Commit();
            }
        }


        [TestMethod]
        public void UpdateFie()
        {
            using (Transaction tr = new Transaction())
            {
                LabelEntity label = Database.Query<LabelEntity>().FirstEx();

                int count = Database.Query<AlbumEntity>().UnsafeUpdate()
                    .Set(a => a.Label, a => label)
                    .Execute();
                //tr.Commit();
            }

        }


        [TestMethod]
        public void UpdateFieConditional()
        {
            using (Transaction tr = new Transaction())
            {
                LabelEntity label = Database.Query<LabelEntity>().FirstEx();

                int count = Database.Query<AlbumEntity>().UnsafeUpdate()
                    .Set(a => a.Label, a => (int)a.Id % 2 == 0 ? label : null)
                    .Execute();
                //tr.Commit();
            }
        }

        [TestMethod]
        public void UpdateFieSetReadonly()
        {
            using (Transaction tr = new Transaction())
            {
                LabelEntity label = Database.Query<LabelEntity>().FirstEx();

                int count = Database.Query<AlbumEntity>().UnsafeUpdate()
                    .Set(a => a.Label, a => label)
                    .Execute();
                //tr.Commit();
            }
        }

        [TestMethod]
        public void UpdateMixin()
        {
            using (Transaction tr = new Transaction())
            {
                int count = Database.Query<NoteWithDateEntity>().UnsafeUpdate()
                    .Set(a => a.Mixin<CorruptMixin>().Corrupt, a => true)
                    .Execute();
                //tr.Commit();
            }
        }

        [TestMethod]
        public void UpdateFieToLite()
        {
            using (Transaction tr = new Transaction())
            {
                LabelEntity label = Database.Query<LabelEntity>().FirstEx();

                int count = Database.Query<LabelEntity>().UnsafeUpdate()
                    .Set(a => a.Owner, a => label.ToLite())
                    .Execute();
                //tr.Commit();
            }
        }


        [TestMethod, ExpectedException(typeof(InvalidOperationException), "The entity is New")]
        public void UpdateFieNew()
        {
            using (Transaction tr = new Transaction())
            {
                LabelEntity label = new LabelEntity();

                int count = Database.Query<AlbumEntity>().UnsafeUpdate()
                    .Set(a => a.Label, a => label)
                    .Execute();
                //tr.Commit();
            }

        }

        [TestMethod]
        public void UpdateFieNull()
        {
            using (Transaction tr = new Transaction())
            {
                int count = Database.Query<AlbumEntity>().UnsafeUpdate()
                    .Set(a => a.Label, a => null)
                    .Execute();
                //tr.Commit();
            }

        }

        [TestMethod]
        public void UpdateIbFie()
        {
            using (Transaction tr = new Transaction())
            {
                ArtistEntity michael = Database.Query<ArtistEntity>().SingleEx(a => a.Dead);

                int count = Database.Query<AlbumEntity>().UnsafeUpdate()
                    .Set(a => a.Author, a => michael)
                    .Execute();
                //tr.Commit();
            }

        }


        [TestMethod]
        public void UpdateIbFieConditional()
        {
            using (Transaction tr = new Transaction())
            {
                ArtistEntity michael = Database.Query<ArtistEntity>().SingleEx(a => a.Dead);

                int count = Database.Query<AlbumEntity>().UnsafeUpdate()
                    .Set(a => a.Author, a => a.Id > 1 ? michael : null)
                    .Execute();
                //tr.Commit();
            }
        }


        public void UpdateIbNull()
        {
            using (Transaction tr = new Transaction())
            {
                int count = Database.Query<AlbumEntity>().UnsafeUpdate()
                    .Set(a => a.Author, a => null)
                    .Execute();
                //tr.Commit();
            }

        }

        [TestMethod]
        public void UpdateIbaFie()
        {
            using (Transaction tr = new Transaction())
            {
                ArtistEntity michael = Database.Query<ArtistEntity>().SingleEx(a => a.Dead);

                int count = Database.Query<NoteWithDateEntity>().UnsafeUpdate()
                    .Set(a => a.Target, a => michael)
                    .Execute();
                //tr.Commit();
            }
        }

        [TestMethod]
        public void UpdateIbaLiteFie()
        {
            using (Transaction tr = new Transaction())
            {
                ArtistEntity michael = Database.Query<ArtistEntity>().SingleEx(a => a.Dead);

                int count = Database.Query<NoteWithDateEntity>().UnsafeUpdate()
                    .Set(a => a.OtherTarget, a => michael.ToLite())
                    .Execute();
                //tr.Commit();
            }
        }

        [TestMethod]
        public void UpdateIbaNull()
        {
            using (Transaction tr = new Transaction())
            {
                int count = Database.Query<NoteWithDateEntity>().UnsafeUpdate()
                    .Set(a => a.Target, a => null)
                    .Execute();
                //tr.Commit();
            }

        }

        [TestMethod]
        public void UpdateIbaLiteNull()
        {
            using (Transaction tr = new Transaction())
            {
                int count = Database.Query<NoteWithDateEntity>().UnsafeUpdate()
                    .Set(a => a.OtherTarget, a => null)
                    .Execute();
                //tr.Commit();
            }

        }

        [TestMethod]
        public void UpdateIbaConditional()
        {
            using (Transaction tr = new Transaction())
            {
                ArtistEntity michael = Database.Query<ArtistEntity>().SingleEx(a => a.Dead);

                int count = Database.Query<NoteWithDateEntity>().UnsafeUpdate()
                    .Set(a => a.Target, a => a.CreationTime > DateTime.Now ? michael : null)
                    .Execute();
                //tr.Commit();
            }
        }


        [TestMethod]
        public void UpdateIbaLiteConditional()
        {
            using (Transaction tr = new Transaction())
            {
                ArtistEntity michael = Database.Query<ArtistEntity>().SingleEx(a => a.Dead);

                int count = Database.Query<NoteWithDateEntity>().UnsafeUpdate()
                    .Set(a => a.OtherTarget, a => a.CreationTime > DateTime.Today ? michael.ToLite() : null)
                    .Execute();
                //tr.Commit();
            }
        }

        [TestMethod]
        public void UpdateIbaCoallesce()
        {
            using (Transaction tr = new Transaction())
            {
                ArtistEntity michael = Database.Query<ArtistEntity>().SingleEx(a => a.Dead);

                int count = Database.Query<NoteWithDateEntity>().UnsafeUpdate()
                    .Set(a => a.Target, a=>a.Target ??  michael)
                    .Execute();
                //tr.Commit();
            }
        }


        [TestMethod]
        public void UpdateIbaLiteCoallesce()
        {
            using (Transaction tr = new Transaction())
            {
                ArtistEntity michael = Database.Query<ArtistEntity>().SingleEx(a => a.Dead);

                int count = Database.Query<NoteWithDateEntity>().UnsafeUpdate()
                    .Set(a => a.OtherTarget, a => a.OtherTarget ?? michael.ToLite())
                    .Execute();
                //tr.Commit();
            }
        }

        [TestMethod]
        public void UpdateEmbeddedField()
        {
            using (Transaction tr = new Transaction())
            {
                int count = Database.Query<AlbumEntity>().UnsafeUpdate()
                    .Set(a => a.BonusTrack.Name, a => a.BonusTrack.Name + " - ")
                    .Execute();
                //tr.Commit();
            }

        }

        [TestMethod]
        public void UpdateEmbeddedNull()
        {
            using (Transaction tr = new Transaction())
            {
                int count = Database.Query<AlbumEntity>().UnsafeUpdate()
                    .Set(a => a.BonusTrack, a => null)
                    .Execute();
                //tr.Commit();
            }
        }


        [TestMethod]
        public void UnsafeUpdatePart()
        {
            using (Transaction tr = new Transaction())
            {
                int count = Database.Query<AlbumEntity>()
                    .Select(a => new { a.Label, Album = a })
                    .UnsafeUpdatePart(p => p.Label)
                    .Set(a => a.Name, p => p.Label.Name + "/" + p.Album.Id)
                    .Execute();

                var list = Database.Query<LabelEntity>().Select(a => a.Name);
                //tr.Commit();
            }

        }

        [TestMethod]
        public void UpdateMListLite()
        {
            using (Transaction tr = new Transaction())
            {
                ArtistEntity artist = Database.Query<ArtistEntity>().FirstEx();

                int count = Database.MListQuery((ArtistEntity a) => a.Friends).UnsafeUpdateMList()
                    .Set(mle => mle.Element, mle => artist.ToLite())
                    .Set(mle => mle.Parent, mle => artist)
                    .Execute();

                var list = Database.MListQuery((ArtistEntity a) => a.Friends);
                //tr.Commit();
            }

        }

        [TestMethod]
        public void UpdateMListEntity()
        {
            using (Transaction tr = new Transaction())
            {
                ArtistEntity artist = Database.Query<ArtistEntity>().FirstEx();

                int count = Database.MListQuery((BandEntity a) => a.Members).UnsafeUpdateMList()
                    .Set(mle => mle.Element, mle => artist)
                    .Execute();
                //tr.Commit();
            }

        }

        [TestMethod]
        public void UpdateMListEmbedded()
        {
            using (Transaction tr = new Transaction())
            {
                int count = Database.MListQuery((AlbumEntity a) => a.Songs).UnsafeUpdateMList()
                     .Set(mle => mle.Element.Seconds, mle => 3)
                    .Execute();

                var list = Database.MListQuery((AlbumEntity a) => a.Songs);
                //tr.Commit();
            }
        }

        [TestMethod]
        public void UpdateMListEmbeddedPart()
        {
            using (Transaction tr = new Transaction())
            {
                int count = (from a in Database.Query<AlbumEntity>()
                             from mle in a.MListElements(_ => _.Songs)
                             select new
                             {
                                 LabelId = a.Label.Id,
                                 mle
                             }).UnsafeUpdateMListPart(p => p.mle)
                            .Set(mle => mle.Element.Seconds, p => (int)p.LabelId)
                            .Execute();

                var list = Database.MListQuery((AlbumEntity a) => a.Songs);
                //tr.Commit();
            }
        }

        [TestMethod]
        public void UpdateExplicitInterfaceImplementedField()
        {
            using (Transaction tr = new Transaction())
            {
                 Database.Query<AlbumEntity>()
                     .UnsafeUpdate()
                     .Set(a=>((ISecretContainer)a).Secret, a=>"Hi")
                     .Execute();
            }
        }

        [TestMethod]
        public void UnsafeUpdatePartExpand()
        {
            using (Transaction tr = new Transaction())
            {
                Database.Query<LabelEntity>()
                    .UnsafeUpdatePart(lb => lb.Owner.Entity.Country)
                    .Set(ctr => ctr.Name, lb => lb.Name)
                    .Execute();
            }
        }


        [TestMethod]
        public void UnsafeUpdateNullableEmbeddedValue()
        {
            using (Transaction tr = new Transaction())
            {
                Database.Query<AlbumEntity>()
                    .UnsafeUpdate()
                    .Set(ctr => ctr.BonusTrack.Index, lb => 2)
                    .Execute();
            }
        }

        [TableName("#MyView")]
        class MyTempView : IView
        {
            [ViewPrimaryKey]
            public int MyId { get; set; }

            public bool Used { get; set; }
        }

        [TestMethod]
        public void UnsafeUpdateMyView()
        {
            using (Transaction tr = new Transaction())
            {
                Administrator.CreateTemporaryTable<MyTempView>();

                Database.Query<ArtistEntity>().UnsafeInsertView(a => new MyTempView { MyId = (int)a.Id, Used = false, });

                Database.View<MyTempView>().Where(a => a.MyId > 1).UnsafeUpdateView().Set(a => a.Used, a => true).Execute();

                tr.Commit();
            }

        }
    }
}
