using System.Linq;
using Xunit;
using Signum.Engine;
using Signum.Entities;
using Signum.Utilities;
using Signum.Test.Environment;

namespace Signum.Test.LinqProvider
{
    /// <summary>
    /// Summary description for LinqProvider
    /// </summary>
    public class InDbTest
    {
        public InDbTest()
        {
            MusicStarter.StartAndLoad();
            Connector.CurrentLogger = new DebugTextWriter();
        }

        private static ArtistEntity GetFemale()
        {
            return Database.Query<ArtistEntity>().Where(a => a.Sex == Sex.Female).Single();
        }

        [Fact]
        public void InDbTestSimple()
        {
            var female = GetFemale();

            Assert.Equal(Sex.Female, female.InDB().Select(a => a.Sex).Single());
            Assert.Equal(Sex.Female, female.ToLite().InDB().Select(a => a.Sex).Single());
        }

        [Fact]
        public void InDbTestSimpleList()
        {
            var female = GetFemale();

            var friends = female.InDB().Select(a => a.Friends.ToList()).Single();
            friends = female.ToLite().InDB().Select(a => a.Friends.ToList()).Single();
        }

        [Fact]
        public void InDbTestSelector()
        {
            var female = GetFemale();

            Assert.Equal(Sex.Female, female.InDB(a => a.Sex));
            Assert.Equal(Sex.Female, female.ToLite().InDB(a => a.Sex));
        }

        [Fact]
        public void InDbTestSelectosList()
        {
            var female = GetFemale();

            var friends = female.InDB(a => a.Friends.ToList());
            friends = female.ToLite().InDB(a => a.Friends.ToList());
        }



        [Fact]
        public void InDbQueryTestSimple()
        {
            var female = GetFemale();

            var list = Database.Query<ArtistEntity>().Where(a=>a.Sex != female.InDB().Select(a2 => a2.Sex).Single()).ToList();
            Assert.True(list.Count > 0);
            list = Database.Query<ArtistEntity>().Where(a => a.Sex != female.ToLite().InDB().Select(a2 => a2.Sex).Single()).ToList();
            Assert.True(list.Count > 0);
        }

        [Fact]
        public void InDbQueryTestSimpleList()
        {
            var female = GetFemale();

            var list = Database.Query<ArtistEntity>().Where(a =>female.InDB().Select(a2 => a2.Friends).Single().Contains(a.ToLite())).ToList();
            Assert.True(list.Count > 0);
            list = Database.Query<ArtistEntity>().Where(a => female.ToLite().InDB().Select(a2 => a2.Friends).Single().Contains(a.ToLite())).ToList();
            Assert.True(list.Count > 0);
        }

        [Fact]
        public void InDbQueryTestSimpleSelector()
        {
            var female = GetFemale();

            var list = Database.Query<ArtistEntity>().Where(a => a.Sex != female.InDB(a2 => a2.Sex)).ToList();
            Assert.True(list.Count > 0);
            list = Database.Query<ArtistEntity>().Where(a => a.Sex != female.ToLite().InDB(a2 => a2.Sex)).ToList();
            Assert.True(list.Count > 0);
        }

        [Fact]
        public void InDbQueryTestSimpleListSelector()
        {
            var female = GetFemale();

            var list = Database.Query<ArtistEntity>().Where(a => female.InDB(a2 => a2.Friends).Contains(a.ToLite())).ToList();
            Assert.True(list.Count > 0);
            list = Database.Query<ArtistEntity>().Where(a => female.ToLite().InDB(a2 => a2.Friends).Contains(a.ToLite())).ToList();
            Assert.True(list.Count > 0);
        }

        [Fact]
        public void SelectManyInDB()
        {
            var artistsInBands = (from b in Database.Query<BandEntity>()
                                  from a in b.Members
                                  select new
                                  {
                                      MaxAlbum = a.InDB(ar => ar.IsMale)
                                  }).ToList();
        }
    }
}
