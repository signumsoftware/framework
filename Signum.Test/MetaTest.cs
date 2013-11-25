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
            Starter.StartAndLoad();
        }


        [TestMethod]
        public void MetaNoMetadata()
        {
            Assert.IsNull(DynamicQuery.QueryMetadata(Database.Query<NoteWithDateDN>().Select(a => a.Target)));
        }

        [TestMethod]
        public void MetaRawEntity()
        {
            var dic = DynamicQuery.QueryMetadata(Database.Query<NoteWithDateDN>());
            Assert.IsNotNull(dic);
        }

        [TestMethod]
        public void MetaAnonymousType()
        {
            var dic = DynamicQuery.QueryMetadata(Database.Query<NoteWithDateDN>().Select(a => new { a.Target, a.Text, a.ToString().Length, Sum = a.ToString() + a.ToString() }));
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
            var dic = DynamicQuery.QueryMetadata(Database.Query<NoteWithDateDN>().Select(a => new Bla { ToStr = a.ToString(), Length = a.ToString().Length }));
            Assert.IsInstanceOfType(dic["ToStr"], typeof(CleanMeta));
            Assert.IsInstanceOfType(dic["Length"], typeof(DirtyMeta));
        }

        [TestMethod]
        public void MetaComplexJoin()
        {
            var dic = DynamicQuery.QueryMetadata(
                    from l in Database.Query<LabelDN>()
                    join a in Database.Query<AlbumDN>() on l equals a.Label
                    select new { Label = l.Name, Name = a.Name, Sum = l.Name.Length + a.Name });

            Assert.IsInstanceOfType(dic["Label"], typeof(CleanMeta));
            Assert.IsInstanceOfType(dic["Name"], typeof(CleanMeta));
            Assert.IsInstanceOfType(dic["Sum"], typeof(DirtyMeta));

            Assert.AreEqual(((DirtyMeta)dic["Sum"]).CleanMetas.Select(cm => cm.PropertyRoutes[0].ToString()).OrderBy().ToString(","), "(AlbumDN).Name,(LabelDN).Name");
        }

        [TestMethod]
        public void MetaComplexJoinGroup()
        {
            var dic = DynamicQuery.QueryMetadata(
                      from l in Database.Query<LabelDN>()
                      join a in Database.Query<AlbumDN>() on l equals a.Label into g
                      select new { l.Name, Num = g.Count() });

            Assert.IsInstanceOfType(dic["Name"], typeof(CleanMeta));
            Assert.IsInstanceOfType(dic["Num"], typeof(DirtyMeta));

            Assert.IsTrue(((DirtyMeta)dic["Num"]).CleanMetas.Count == 0);
        }

        [TestMethod]
        public void MetaComplexGroup()
        {
            var dic = DynamicQuery.QueryMetadata(
                    from a in Database.Query<AlbumDN>()
                    group a by a.Label into g
                    select new { g.Key, Num = g.Count() });

            Assert.IsInstanceOfType(dic["Key"], typeof(CleanMeta));
            Assert.IsInstanceOfType(dic["Num"], typeof(DirtyMeta));

            Assert.IsTrue(((DirtyMeta)dic["Num"]).CleanMetas.Count == 0);
        }

        [TestMethod]
        public void MetaSelectMany()
        {
            var dic = DynamicQuery.QueryMetadata(
                    from a in Database.Query<AlbumDN>()
                    from s in a.Songs
                    select new { a.Name, Song = s.Name }
                    );

            Assert.IsInstanceOfType(dic["Name"], typeof(CleanMeta));
            Assert.IsInstanceOfType(dic["Song"], typeof(CleanMeta));

            Assert.IsNotNull(((CleanMeta)dic["Song"]).PropertyRoutes[0].ToString(), "(AlbumDN).Songs/Name");
        }

        [TestMethod]
        public void MetaCoallesce()
        {
            var dic = DynamicQuery.QueryMetadata(
                    from a in Database.Query<AlbumDN>()
                    select new { Author = (ArtistDN)a.Author ?? (IAuthorDN)(BandDN)a.Author }
                    );

            DirtyMeta meta = (DirtyMeta)dic["Author"];

            Assert.AreEqual(meta.Implementations, Implementations.By(typeof(ArtistDN), typeof(BandDN)));
        }

        [TestMethod]
        public void MetaConditional()
        {
            var dic = DynamicQuery.QueryMetadata(
                    from a in Database.Query<AlbumDN>()
                    select new { Author = a.Id > 1 ? (ArtistDN)a.Author : (IAuthorDN)(BandDN)a.Author }
                    );

            DirtyMeta meta = (DirtyMeta)dic["Author"];

            Assert.AreEqual(meta.Implementations, Implementations.By(typeof(ArtistDN), typeof(BandDN)));
        }

    }
}
