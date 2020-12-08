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
    public class EntityContextTest
    {
        public EntityContextTest()
        {
            MusicStarter.StartAndLoad();
            Connector.CurrentLogger = new DebugTextWriter();
        }

        [Fact]
        public void EntityIdMember()
        {
            var authors = Database.Query<AlbumEntity>().Count(a => EntityContext.EntityId(a.Label) == a.Id);
        }

        [Fact]
        public void EntityIdEmbeddedMember()
        {
            var authors = Database.Query<AlbumEntity>().Count(a => EntityContext.EntityId(a.BonusTrack!.Name) == a.Id);
        }

        [Fact]
        public void EntiyIdMList()
        {
            var authors = Database.Query<AlbumEntity>().Count(a =>  EntityContext.EntityId(a.Songs.FirstOrDefault()) == a.Id);
        }

        [Fact]
        public void EntityIdMListMember()
        {
            var authors = Database.Query<AlbumEntity>().Count(a => EntityContext.EntityId(a.Songs.FirstOrDefault()!.Name) == a.Id);
        }


        [Fact]
        public void RowIdMList()
        {
            var authors = Database.Query<AlbumEntity>().Count(a => EntityContext.MListRowId(a.Songs.FirstOrDefault()) == a.Id);
        }


        [Fact]
        public void RowIdMListMember()
        {
            var authors = Database.Query<AlbumEntity>().Count(a => EntityContext.MListRowId(a.Songs.FirstOrDefault()!.Name) == a.Id);
        }

    }
}
