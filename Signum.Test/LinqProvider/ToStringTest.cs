using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Signum.Engine;
using Signum.Utilities;
using Signum.Entities;
using Signum.Test.Environment;

namespace Signum.Test.LinqProvider
{
    public class ToStringTest
    {
        public ToStringTest()
        {
            MusicStarter.StartAndLoad();
            Connector.CurrentLogger = new DebugTextWriter();
        }

        [Fact]
        public void ToStringMainQuery()
        {
            Assert.Equal(
                Database.Query<ArtistEntity>().Select(a => a.Name).ToString(" | "),
                Database.Query<ArtistEntity>().ToString(a => a.Name, " | "));
        }

        [Fact]
        public void ToStringEntity()
        {
            Assert.Equal(
                Database.Query<ArtistEntity>().Select(a => a.Name).ToString(" | "),
                Database.Query<ArtistEntity>().ToString(" | "));
        }


        [Fact]
        public void ToStringSubCollection()
        {
            var result1 = (from b in Database.Query<BandEntity>()
                           orderby b.Name
                           select new
                           {
                               b.Name,
                               MembersToString = b.Members.OrderBy(a => a.Name).ToString(a => a.Name, " | "),
                           }).ToList();

            var result2 = (from b in Database.Query<BandEntity>()
                           orderby b.Name
                           select new
                           {
                               b.Name,
                               MembersToString = b.Members.OrderBy(a => a.Name).Select(a => a.Name).ToList().ToString(" | "),
                           }).ToList();

            Assert.True(Enumerable.SequenceEqual(result1, result2));

        }

        [Fact]
        public void ToStringSubQuery()
        {
            var result1 = (from b in Database.Query<BandEntity>()
                           orderby b.Name
                           select new
                           {
                               b.Name,
                               AlbumnsToString = Database.Query<AlbumEntity>().Where(a => a.Author == b).OrderBy(a => a.Name).ToString(a => a.Name, " | "),
                           }).ToList();

            var result2 = (from b in Database.Query<BandEntity>()
                           orderby b.Name
                           select new
                           {
                               b.Name,
                               AlbumnsToString = Database.Query<AlbumEntity>().Where(a => a.Author == b).OrderBy(a => a.Name).Select(a => a.Name).ToList().ToString(" | "),
                           }).ToList();

            Assert.Equal(result1, result2);
        }


        [Fact]
        public void ToStringNumbers()
        {
            var result1 = (from b in Database.Query<BandEntity>()
                           orderby b.Name
                           select new
                           {
                               b.Name,
                               AlbumnsToString = Database.Query<AlbumEntity>().Where(a => a.Author == b).OrderBy(a => a.Name).ToString(a => a.Id.ToString(), " | "),
                           }).ToList();

            var result2 = (from b in Database.Query<BandEntity>()
                           orderby b.Name
                           select new
                           {
                               b.Name,
                               AlbumnsToString = Database.Query<AlbumEntity>().Where(a => a.Author == b).OrderBy(a => a.Name).Select(a => a.Id).ToString(" | "),
                           }).ToList();

            Func<List<PrimaryKey>, string> toString = list => list.ToString(" | ");

            var result3 = (from b in Database.Query<BandEntity>()
                           orderby b.Name
                           select new
                           {
                               b.Name,
                               AlbumnsToString = toString(Database.Query<AlbumEntity>().Where(a => a.Author == b).OrderBy(a => a.Name).Select(a => a.Id).ToList()),
                           }).ToList();


            Assert.True(Enumerable.SequenceEqual(result1, result2));
            Assert.True(Enumerable.SequenceEqual(result2, result3));

        }

    }
}
