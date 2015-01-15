﻿using System;
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
                    Songs = types.Select(t => new SongDN() { Name = t.Name }).ToMList(),
                    State = AlbumState.Saved
                }.Save();

                Assert2.AssertAll(GraphExplorer.FromRoot(album), a => !a.IsGraphModified);

                Assert.AreEqual(prev + types.Length, Database.MListQuery((AlbumDN a) => a.Songs).Count());

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
            using (OperationLogic.AllowSave<AlbumDN>())
            using (OperationLogic.AllowSave<ArtistDN>())
            {
                var maxRowId = Database.MListQuery((AlbumDN a) => a.Songs).Max(a => a.RowId);

                var artist = Database.Query<ArtistDN>().First();

                var album = new AlbumDN
                {
                    Name = "Test album",
                    Author = artist,
                    Year = 2000,
                    Songs = { new SongDN { Name = "Song 1" } },
                    State = AlbumState.Saved,
                };

                var innerList = ((IMListPrivate<SongDN>)album.Songs).InnerList;

                Assert.IsNull(innerList[0].RowId);
                //Insert and row-id is set
                album.Save();
                Assert.IsNotNull(innerList[0].RowId);
                Assert.IsTrue(innerList[0].RowId > maxRowId); 


                album.Songs.Add(new SongDN { Name = "Song 2" });

                Assert.IsNull(innerList[1].RowId);

                album.Save();
                //Insert and row-id is set
                Assert.IsNotNull(innerList[1].RowId);

                var song = innerList[0];

                album.Songs.Remove(song.Value);
                //Delete
                album.Save();

                {
                    var album2 = album.ToLite().Retrieve();

                    Assert.IsTrue(album.Songs.Count == album2.Songs.Count);
                    Assert.IsTrue(innerList[0].RowId == ((IMListPrivate<SongDN>)album2.Songs).InnerList[0].RowId);
                    Assert.IsTrue(!album.MListElements(a => a.Songs).Any(mle => mle.RowId == song.RowId));
                }

                album.Songs[0].Name += "*";
                //Update
                album.Save();

                {
                    var album2 = album.ToLite().Retrieve();
                    
                    Assert.IsTrue(album.Songs.Count == album2.Songs.Count);
                    Assert.IsTrue(innerList[0].RowId == ((IMListPrivate<SongDN>)album2.Songs).InnerList[0].RowId);
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
            using (OperationLogic.AllowSave<AlbumDN>())
            using (OperationLogic.AllowSave<ArtistDN>())
            {
                var artist = Database.Query<ArtistDN>().First();

                var album = new AlbumDN
                {
                    Name = "Test album",
                    Author = artist,
                    Year = 2000,
                    Songs = { new SongDN { Name = "Song 0" }, new SongDN { Name = "Song 1" }, new SongDN { Name = "Song 2" }, },
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
            using (OperationLogic.AllowSave<AlbumDN>())
            using (OperationLogic.AllowSave<ArtistDN>())
            {
                var prev = Database.MListQuery((AlbumDN a) => a.Songs).Count();

                var authors = 
                    Database.Query<BandDN>().Take(6).ToList().Concat<IAuthorDN>(
                    Database.Query<ArtistDN>().Take(8).ToList()).ToList();

                List<AlbumDN> albums = 0.To(16).Select(i => new AlbumDN()
                {
                    Name = "System Greatest hits {0}".Formato(i),
                    Author = i < authors.Count ? authors[i] : new ArtistDN { Name = ".Net Framework" },
                    Year = 2001,
                    Songs =  { new SongDN { Name = "Compilation {0}".Formato(i) }},
                    State = AlbumState.Saved
                }).ToList();

                albums.SaveList();

                Assert2.AssertAll(GraphExplorer.FromRoots(albums), a => !a.IsGraphModified);

                Assert.AreEqual(prev + 16, Database.MListQuery((AlbumDN a) => a.Songs).Count());

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
                var albums = Database.Query<AlbumDN>().ToList();

                Assert2.AssertAll(GraphExplorer.FromRoots(albums), a => a.Modified == ModifiedState.Sealed);

                Assert2.Throws<InvalidOperationException>("sealed", () => albums.First().Name = "New name");


                var notes = Database.Query<NoteWithDateDN>().ToList();
                Assert2.AssertAll(GraphExplorer.FromRoots(notes), a => a.Modified == ModifiedState.Sealed);

                //tr.Commit();
            }
        }

        [TestMethod]
        public void BulkInsert()
        {
            using (Transaction tr = new Transaction())
            {
                var max = Database.Query<NoteWithDateDN>().Select(a => a.Id).ToList().Max();

                var list = Database.Query<AlbumDN>().Select(a => new NoteWithDateDN
                {
                    CreationTime = DateTime.Now,
                    Text = "Nice album " + a.Name,
                    Target = a
                }).ToList();

                Administrator.BulkInsert(list);

                Database.Query<NoteWithDateDN>().Where(a => a.Id > max).UnsafeDelete(); 

                tr.Commit();
            }

        }


        [TestMethod]
        public void BulkInsertMList()
        {
            using (Transaction tr = new Transaction())
            {
                var max = Database.MListQuery((AlbumDN a) => a.Songs).Max(a => a.RowId);

                var list = Database.Query<AlbumDN>().Select(a => new MListElement<AlbumDN, SongDN>
                {
                    Order = 100,
                    Element = new SongDN { Duration = TimeSpan.FromMinutes(1), Name = "Bonus - " + a.Name },
                    Parent = a,
                }).ToList();

                Administrator.BulkInsertMList((AlbumDN a) => a.Songs, list);

                Database.MListQuery((AlbumDN a) => a.Songs).Where(a => a.RowId > max).UnsafeDeleteMList();

                tr.Commit();
            }

        }
    }
}
