using System.Linq;
using Xunit;
using Signum.Engine;
using Signum.Engine.DynamicQuery;
using Signum.Utilities;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Test.Environment;

namespace Signum.Test
{
    public class MetaTest
    {
        public MetaTest()
        {
            MusicStarter.StartAndLoad();
        }


        [Fact]
        public void MetaNoMetadata()
        {
            Assert.Null(DynamicQueryCore.QueryMetadata(Database.Query<NoteWithDateEntity>().Select(a => a.Target)));
        }

        [Fact]
        public void MetaRawEntity()
        {
            var dic = DynamicQueryCore.QueryMetadata(Database.Query<NoteWithDateEntity>())!;
            Assert.NotNull(dic);
        }

        [Fact]
        public void MetaAnonymousType()
        {
            var dic = DynamicQueryCore.QueryMetadata(Database.Query<NoteWithDateEntity>().Select(a => new { a.Target, a.Text, a.ToString().Length, Sum = a.ToString() + a.ToString() }))!;
            Assert.IsType<CleanMeta>(dic["Target"]);
            Assert.IsType<CleanMeta>(dic["Text"]);
            Assert.IsType<DirtyMeta>(dic["Length"]);
            Assert.IsType<DirtyMeta>(dic["Sum"]);
        }

        public class Bla
        {
            public string ToStr { get; set; }
            public int Length { get; set; }
        }

        [Fact]
        public void MetaNamedType()
        {
            var dic = DynamicQueryCore.QueryMetadata(Database.Query<NoteWithDateEntity>().Select(a => new Bla { ToStr = a.ToString(), Length = a.ToString().Length }))!;
            Assert.IsType<CleanMeta>(dic["ToStr"]);
            Assert.IsType<DirtyMeta>(dic["Length"]);
        }

        [Fact]
        public void MetaComplexJoin()
        {
            var dic = DynamicQueryCore.QueryMetadata(
                    from l in Database.Query<LabelEntity>()
                    join a in Database.Query<AlbumEntity>() on l equals a.Label
                    select new { Label = l.Name, a.Name, Sum = l.Name.Length + a.Name })!;

            Assert.IsType<CleanMeta>(dic["Label"]);
            Assert.IsType<CleanMeta>(dic["Name"]);
            Assert.IsType<DirtyMeta>(dic["Sum"]);

            var metas = ((DirtyMeta)dic["Sum"]!).CleanMetas;
            Assert.Equal("(Album).Name,(Label).Name", metas.SelectMany(cm => cm.PropertyRoutes).Distinct().ToString(","));
        }

        [Fact]
        public void MetaComplexJoinGroup()
        {
            var dic = DynamicQueryCore.QueryMetadata(
                      from l in Database.Query<LabelEntity>()
                      join a in Database.Query<AlbumEntity>() on l equals a.Label into g
                      select new { l.Name, Num = g.Count() })!;

            Assert.IsType<CleanMeta>(dic["Name"]);
            Assert.IsType<DirtyMeta>(dic["Num"]);

            Assert.True(((DirtyMeta)dic["Num"]!).CleanMetas.Count == 0);
        }

        [Fact]
        public void MetaComplexGroup()
        {
            var dic = DynamicQueryCore.QueryMetadata(
                    from a in Database.Query<AlbumEntity>()
                    group a by a.Label into g
                    select new { g.Key, Num = g.Count() })!;

            Assert.IsType<CleanMeta>(dic["Key"]);
            Assert.IsType<DirtyMeta>(dic["Num"]);

            Assert.True(((DirtyMeta)dic["Num"]!).CleanMetas.Count == 0);
        }

        [Fact]
        public void MetaSelectMany()
        {
            var dic = DynamicQueryCore.QueryMetadata(
                    from a in Database.Query<AlbumEntity>()
                    from s in a.Songs
                    select new { a.Name, Song = s.Name }
                    )!;

            Assert.IsType<CleanMeta>(dic["Name"]);
            Assert.IsType<CleanMeta>(dic["Song"]);

            Assert.Equal("(Album).Songs/Name", ((CleanMeta)dic["Song"]!).PropertyRoutes[0].ToString());
        }

        [Fact]
        public void MetaCoalesce()
        {
            var dic = DynamicQueryCore.QueryMetadata(
                    from a in Database.Query<AlbumEntity>()
                    select new { Author = (ArtistEntity?)a.Author ?? (IAuthorEntity?)(BandEntity?)a.Author }
                    )!;

            DirtyMeta meta = (DirtyMeta)dic["Author"]!;

            Assert.Equal(meta.Implementations, Implementations.By(typeof(ArtistEntity), typeof(BandEntity)));
        }

        [Fact]
        public void MetaConditional()
        {
            var dic = DynamicQueryCore.QueryMetadata(
                    from a in Database.Query<AlbumEntity>()
                    select new { Author = a.Id > 1 ? (ArtistEntity)a.Author : (IAuthorEntity)(BandEntity)a.Author }
                    )!;

            DirtyMeta meta = (DirtyMeta)dic["Author"]!;

            Assert.Equal(meta.Implementations, Implementations.By(typeof(ArtistEntity), typeof(BandEntity)));
        }

    }
}
