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
            MusicStarter.StartAndLoad();
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
            using (OperationLogic.AllowSave<ArtistEntity>())
            {
                ArtistEntity m = new ArtistEntity() { Name = "Michael" };
                ArtistEntity f = new ArtistEntity() { Name = "Frank" };
                m.Friends.Add(f.ToLiteFat());
                f.Friends.Add(m.ToLiteFat());

                Database.SaveParams(m, f);

                var list = Database.Query<ArtistEntity>().Where(a => a == m || a == f).ToList();

                Assert.IsTrue(list[0].Friends.Contains(list[1].ToLite()));
                Assert.IsTrue(list[1].Friends.Contains(list[0].ToLite()));

                //tr.Commit();
            }

        }

        [TestMethod]
        public void SaveSelfCycle()
        {
            using (Transaction tr = new Transaction())
            using (OperationLogic.AllowSave<ArtistEntity>())
            {
                ArtistEntity m = new ArtistEntity() { Name = "Michael" };
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
            using (OperationLogic.AllowSave<ArtistEntity>())
            {
                var prev =  Database.Query<ArtistEntity>().Count();

                Type[] types = typeof(int).Assembly.GetTypes().Where(a => a.Name.Length > 3 && a.Name.StartsWith("A")).ToArray();

                var list = types.Select(t => new ArtistEntity() { Name = t.Name }).ToList();

                list.SaveList();

                Assert.AreEqual(prev + types.Length, Database.Query<ArtistEntity>().Count());

                list.ForEach(a => a.Name += "Updated");

                list.SaveList();

                //tr.Commit();
            }
        }

        [TestMethod]
        public void SaveMList()
        {
            using (Transaction tr = new Transaction())
            using (OperationLogic.AllowSave<AlbumEntity>())
            using (OperationLogic.AllowSave<ArtistEntity>())
            {
                var prev = Database.MListQuery((AlbumEntity a) => a.Songs).Count();

                Type[] types = typeof(int).Assembly.GetTypes().Where(a => a.Name.Length > 3 && a.Name.StartsWith("A")).ToArray();

                AlbumEntity album = new AlbumEntity()
                {
                    Name = "System Greatest hits",
                    Author = new ArtistEntity { Name = ".Net Framework" },
                    Year = 2001,
                    Songs = types.Select(t => new SongEmbedded() { Name = t.Name }).ToMList(),
                    State = AlbumState.Saved
                }.Save();

                Assert2.AssertAll(GraphExplorer.FromRoot(album), a => !a.IsGraphModified);

                Assert.AreEqual(prev + types.Length, Database.MListQuery((AlbumEntity a) => a.Songs).Count());

                album.Name += "Updated";

                album.Save();

                album.Songs.ForEach(a => a.Name = "Updated");

                album.Save();

                //tr.Commit();
            }
        }


        [TestMethod]
        public void SmartSaveMList()
        {
            using (Transaction tr = new Transaction())
            using (OperationLogic.AllowSave<AlbumEntity>())
            using (OperationLogic.AllowSave<ArtistEntity>())
            {
                var maxRowId = Database.MListQuery((AlbumEntity a) => a.Songs).Max(a => a.RowId);

                var artist = Database.Query<ArtistEntity>().First();

                var album = new AlbumEntity
                {
                    Name = "Test album",
                    Author = artist,
                    Year = 2000,
                    Songs = { new SongEmbedded { Name = "Song 1" } },
                    State = AlbumState.Saved,
                };

                var innerList = ((IMListPrivate<SongEmbedded>)album.Songs).InnerList;

                Assert.IsNull(innerList[0].RowId);
                //Insert and row-id is set
                album.Save();
                Assert.IsNotNull(innerList[0].RowId);
                Assert.IsTrue(innerList[0].RowId > maxRowId); 


                album.Songs.Add(new SongEmbedded { Name = "Song 2" });

                Assert.IsNull(innerList[1].RowId);

                album.Save();
                //Insert and row-id is set
                Assert.IsNotNull(innerList[1].RowId);

                var song = innerList[0];

                album.Songs.Remove(song.Element);
                //Delete
                album.Save();

                {
                    var album2 = album.ToLite().Retrieve();

                    Assert.IsTrue(album.Songs.Count == album2.Songs.Count);
                    Assert.IsTrue(innerList[0].RowId == ((IMListPrivate<SongEmbedded>)album2.Songs).InnerList[0].RowId);
                    Assert.IsTrue(!album.MListElements(a => a.Songs).Any(mle => mle.RowId == song.RowId));
                }

                album.Songs[0].Name += "*";
                //Update
                album.Save();

                {
                    var album2 = album.ToLite().Retrieve();
                    
                    Assert.IsTrue(album.Songs.Count == album2.Songs.Count);
                    Assert.IsTrue(innerList[0].RowId == ((IMListPrivate<SongEmbedded>)album2.Songs).InnerList[0].RowId);
                    Assert.IsTrue(album.Songs[0].Name == album2.Songs[0].Name);
                    Assert.IsTrue(!album.MListElements(a => a.Songs).Any(mle => mle.RowId == song.RowId));
                }

                //tr.Commit();
            }
        }

        [TestMethod]
        public void SmartSaveMListOrder()
        {
            using (Transaction tr = new Transaction())
            using (OperationLogic.AllowSave<AlbumEntity>())
            using (OperationLogic.AllowSave<ArtistEntity>())
            {
                var artist = Database.Query<ArtistEntity>().First();

                var album = new AlbumEntity
                {
                    Name = "Test album",
                    Author = artist,
                    Year = 2000,
                    Songs = { new SongEmbedded { Name = "Song 0" }, new SongEmbedded { Name = "Song 1" }, new SongEmbedded { Name = "Song 2" }, },
                    State = AlbumState.Saved,
                };

                album.Save();

                AssertSequenceEquals(album.MListElements(a => a.Songs).OrderBy(a=>a.Order).Select(mle => KVP.Create(mle.Order, mle.Element.Name)),
                    new Dictionary<int, string> { { 0, "Song 0" }, { 1, "Song 1" }, { 2, "Song 2" } });

                var ids = album.MListElements(a => a.Songs).Select(a => a.RowId).ToHashSet();

                album.Songs.SortDescending(a => a.Name);

                album.Save();

                var ids2 = album.MListElements(a => a.Songs).Select(a => a.RowId).ToHashSet();

                AssertSequenceEquals(ids.OrderBy(), ids2.OrderBy());


                AssertSequenceEquals(album.MListElements(a => a.Songs).OrderBy(a => a.Order).Select(mle => KVP.Create(mle.Order, mle.Element.Name)),
                    new Dictionary<int, string> { { 0, "Song 2" }, { 1, "Song 1" }, { 2, "Song 0" } });


                var s3 = album.Songs[0];

                album.Songs.RemoveAt(0);

                album.Songs.Insert(1, s3);

                album.Save();

                AssertSequenceEquals(album.MListElements(a => a.Songs).OrderBy(a => a.Order).Select(mle => KVP.Create(mle.Order, mle.Element.Name)),
                    new Dictionary<int, string> { { 0, "Song 1" }, { 1, "Song 2" }, { 2, "Song 0" } });

                AssertSequenceEquals(album.ToLite().Retrieve().Songs.Select(a => a.Name), new[] { "Song 1", "Song 2", "Song 0" });

                 //tr.Commit();
            }
        }

        void AssertSequenceEquals<T>(IEnumerable<T> one, IEnumerable<T> two)
        {
            Assert.IsTrue(one.SequenceEqual(two));
        }

        [TestMethod]
        public void SaveManyMList()
        {
            using (Transaction tr = new Transaction())
            using (OperationLogic.AllowSave<AlbumEntity>())
            using (OperationLogic.AllowSave<ArtistEntity>())
            {
                var prev = Database.MListQuery((AlbumEntity a) => a.Songs).Count();

                var authors = 
                    Database.Query<BandEntity>().Take(6).ToList().Concat<IAuthorEntity>(
                    Database.Query<ArtistEntity>().Take(8).ToList()).ToList();

                List<AlbumEntity> albums = 0.To(16).Select(i => new AlbumEntity()
                {
                    Name = "System Greatest hits {0}".FormatWith(i),
                    Author = i < authors.Count ? authors[i] : new ArtistEntity { Name = ".Net Framework" },
                    Year = 2001,
                    Songs =  { new SongEmbedded { Name = "Compilation {0}".FormatWith(i) }},
                    State = AlbumState.Saved
                }).ToList();

                albums.SaveList();

                Assert2.AssertAll(GraphExplorer.FromRoots(albums), a => !a.IsGraphModified);

                Assert.AreEqual(prev + 16, Database.MListQuery((AlbumEntity a) => a.Songs).Count());

                albums.ForEach(a => a.Name += "Updated");

                albums.SaveList();

                albums.ForEach(a => a.Songs.ForEach(s => s.Name = "Updated"));

                albums.SaveList();

                //tr.Commit();
            }

        }
        
        [TestMethod]
        public void RetrieveSealed()
        {
            using (Transaction tr = new Transaction())
            using (new EntityCache(EntityCacheType.ForceNewSealed))
            {
                var albums = Database.Query<AlbumEntity>().ToList();

                Assert2.AssertAll(GraphExplorer.FromRoots(albums), a => a.Modified == ModifiedState.Sealed);

                Assert2.Throws<InvalidOperationException>("sealed", () => albums.First().Name = "New name");


                var notes = Database.Query<NoteWithDateEntity>().ToList();
                Assert2.AssertAll(GraphExplorer.FromRoots(notes), a => a.Modified == ModifiedState.Sealed);

                //tr.Commit();
            }
        }



        [TestMethod]
        public void BulkInsertWithMList()
        {
            using (Transaction tr = new Transaction())
            {
                var max = Database.Query<AlbumEntity>().Select(a => a.Id).ToList().Max();
                var count = Database.MListQuery<AlbumEntity, SongEmbedded>(a => a.Songs).Count();

                var list = Database.Query<AlbumEntity>().ToList().Select(a => new AlbumEntity
                {
                    Name = "Copy of " + a.Name,
                    Author = a.Author,
                    Label = a.Label,
                    State = a.State,
                    Year = a.Year,
                    Songs = a.Songs.Select(s => new SongEmbedded
                    {
                        Name = s.Name,
                        Seconds = s.Seconds,
                    }).ToMList()
                }).ToList();

                list.BulkInsertQueryIds(keySelector: a => a.Name, isNewPredicate: a => a.Id > max);

                Assert.AreNotEqual(count, Database.MListQuery<AlbumEntity, SongEmbedded>(a => a.Songs).Count());

                Database.Query<AlbumEntity>().Where(a => a.Id > max).UnsafeDelete();

                Assert.AreEqual(count, Database.MListQuery<AlbumEntity, SongEmbedded>(a => a.Songs).Count());

                tr.Commit();
            }
        }


        [TestMethod]
        public void BulkInsertTable()
        {
            using (Transaction tr = new Transaction())
            {
                var max = Database.Query<NoteWithDateEntity>().Select(a => a.Id).ToList().Max();

                var list = Database.Query<AlbumEntity>().Select(a => new NoteWithDateEntity
                {
                    CreationTime = DateTime.Now,
                    Text = "Nice album " + a.Name,
                    Target = a
                }).ToList();

                BulkInserter.BulkInsertTable(list);

                Database.Query<NoteWithDateEntity>().Where(a => a.Id > max).UnsafeDelete(); 

                tr.Commit();
            }

        }


        [TestMethod]
        public void BulkInsertMList()
        {
            using (Transaction tr = new Transaction())
            {
                var max = Database.MListQuery((AlbumEntity a) => a.Songs).Max(a => a.RowId);

                var list = Database.Query<AlbumEntity>().Select(a => new MListElement<AlbumEntity, SongEmbedded>
                {
                    Parent = a,
                    Element = new SongEmbedded { Duration = TimeSpan.FromMinutes(1), Name = "Bonus - " + a.Name },
                    Order = 100,
                }).ToList();

                list.BulkInsertMListTable( a => a.Songs);

                Database.MListQuery((AlbumEntity a) => a.Songs).Where(a => a.RowId > max).UnsafeDeleteMList();

                tr.Commit();
            }

        }
    }
}
