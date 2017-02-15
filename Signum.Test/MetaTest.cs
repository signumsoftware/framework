using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signum.Engine;
using Signum.Entities.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Utilities.Reflection;
using Signum.Utilities;
using System.Reflection;
using Signum.Engine.Linq;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Test.Environment;
using System.Linq.Expressions;

namespace Signum.Test
{
    [TestClass]
    public class MetaTest
    {
        [ClassInitialize()]
        public static void MyClassInitialize(TestContext testContext)
        {
            MusicStarter.StartAndLoad();
        }


        [TestMethod]
        public void MetaNoMetadata()
        {
            Assert.IsNull(DynamicQueryCore.QueryMetadata(Database.Query<NoteWithDateEntity>().Select(a => a.Target)));
        }

        [TestMethod]
        public void MetaRawEntity()
        {
            var dic = DynamicQueryCore.QueryMetadata(Database.Query<NoteWithDateEntity>());
            Assert.IsNotNull(dic);
        }

        [TestMethod]
        public void MetaAnonymousType()
        {
            var dic = DynamicQueryCore.QueryMetadata(Database.Query<NoteWithDateEntity>().Select(a => new { a.Target, a.Text, a.ToString().Length, Sum = a.ToString() + a.ToString() }));
            Assert.IsInstanceOfType(dic["Target"], typeof(CleanMeta));
            Assert.IsInstanceOfType(dic["Text"], typeof(CleanMeta));
            Assert.IsInstanceOfType(dic["Length"], typeof(DirtyMeta));
            Assert.IsInstanceOfType(dic["Sum"], typeof(DirtyMeta));
        }

        public class Bla
        {
            public string ToStr { get; set; }
            public int Length { get; set; }
        }

        [TestMethod]
        public void MetaNamedType()
        {
            var dic = DynamicQueryCore.QueryMetadata(Database.Query<NoteWithDateEntity>().Select(a => new Bla { ToStr = a.ToString(), Length = a.ToString().Length }));
            Assert.IsInstanceOfType(dic["ToStr"], typeof(CleanMeta));
            Assert.IsInstanceOfType(dic["Length"], typeof(DirtyMeta));
        }

        [TestMethod]
        public void MetaComplexJoin()
        {
            var dic = DynamicQueryCore.QueryMetadata(
                    from l in Database.Query<LabelEntity>()
                    join a in Database.Query<AlbumEntity>() on l equals a.Label
                    select new { Label = l.Name, Name = a.Name, Sum = l.Name.Length + a.Name });

            Assert.IsInstanceOfType(dic["Label"], typeof(CleanMeta));
            Assert.IsInstanceOfType(dic["Name"], typeof(CleanMeta));
            Assert.IsInstanceOfType(dic["Sum"], typeof(DirtyMeta));

            var metas = ((DirtyMeta)dic["Sum"]).CleanMetas;
            Assert.AreEqual(metas.SelectMany(cm => cm.PropertyRoutes).Distinct().ToString(","), "(Album).Name,(Label).Name");
        }

        [TestMethod]
        public void MetaComplexJoinGroup()
        {
            var dic = DynamicQueryCore.QueryMetadata(
                      from l in Database.Query<LabelEntity>()
                      join a in Database.Query<AlbumEntity>() on l equals a.Label into g
                      select new { l.Name, Num = g.Count() });

            Assert.IsInstanceOfType(dic["Name"], typeof(CleanMeta));
            Assert.IsInstanceOfType(dic["Num"], typeof(DirtyMeta));

            Assert.IsTrue(((DirtyMeta)dic["Num"]).CleanMetas.Count == 0);
        }

        [TestMethod]
        public void MetaComplexGroup()
        {
            var dic = DynamicQueryCore.QueryMetadata(
                    from a in Database.Query<AlbumEntity>()
                    group a by a.Label into g
                    select new { g.Key, Num = g.Count() });

            Assert.IsInstanceOfType(dic["Key"], typeof(CleanMeta));
            Assert.IsInstanceOfType(dic["Num"], typeof(DirtyMeta));

            Assert.IsTrue(((DirtyMeta)dic["Num"]).CleanMetas.Count == 0);
        }

        [TestMethod]
        public void MetaSelectMany()
        {
            var dic = DynamicQueryCore.QueryMetadata(
                    from a in Database.Query<AlbumEntity>()
                    from s in a.Songs
                    select new { a.Name, Song = s.Name }
                    );

            Assert.IsInstanceOfType(dic["Name"], typeof(CleanMeta));
            Assert.IsInstanceOfType(dic["Song"], typeof(CleanMeta));

            Assert.IsNotNull(((CleanMeta)dic["Song"]).PropertyRoutes[0].ToString(), "(AlbumEntity).Songs/Name");
        }

        [TestMethod]
        public void MetaCoallesce()
        {
            var dic = DynamicQueryCore.QueryMetadata(
                    from a in Database.Query<AlbumEntity>()
                    select new { Author = (ArtistEntity)a.Author ?? (IAuthorEntity)(BandEntity)a.Author }
                    );

            DirtyMeta meta = (DirtyMeta)dic["Author"];

            Assert.AreEqual(meta.Implementations, Implementations.By(typeof(ArtistEntity), typeof(BandEntity)));
        }

        [TestMethod]
        public void MetaConditional()
        {
            var dic = DynamicQueryCore.QueryMetadata(
                    from a in Database.Query<AlbumEntity>()
                    select new { Author = a.Id > 1 ? (ArtistEntity)a.Author : (IAuthorEntity)(BandEntity)a.Author }
                    );

            DirtyMeta meta = (DirtyMeta)dic["Author"];

            Assert.AreEqual(meta.Implementations, Implementations.By(typeof(ArtistEntity), typeof(BandEntity)));
        }

    }
}
