using System.Linq;
using Xunit;
using Signum.Engine;
using Signum.Entities;
using Signum.Utilities;
using Signum.Test.Environment;

namespace Signum.Test.LinqProviderUpdateDelete
{
    /// <summary>
    /// Summary description for LinqProvider
    /// </summary>
    public class UnsafeDeleteTest
    {
        public UnsafeDeleteTest()
        {
            MusicStarter.StartAndLoad();
            Connector.CurrentLogger = new DebugTextWriter();
        }

        [Fact]
        public void DeleteAll()
        {
            using (Transaction tr = new Transaction())
            {
                int count = Database.Query<AlbumEntity>().UnsafeDelete();

                //tr.Commit();
            }

        }

        [Fact]
        public void Delete()
        {
            using (Transaction tr = new Transaction())
            {
                int count = Database.Query<AlbumEntity>().Where(a => a.Year < 1990).UnsafeDelete();

                //tr.Commit();
            }
        }

        [Fact]
        public void DeleteChunks()
        {
            using (Transaction tr = new Transaction())
            {
                int count = Database.Query<AlbumEntity>().UnsafeDeleteChunks(2);

                //tr.Commit();
            }
        }

        [Fact]
        public void DeleteJoin()
        {
            using (Transaction tr = new Transaction())
            {
                int count = Database.Query<AlbumEntity>().Where(a => ((ArtistEntity)a.Author).Dead).UnsafeDelete();
                //tr.Commit();
            }
        }


        [Fact]
        public void DeleteMListLite()
        {
            using (Transaction tr = new Transaction())
            {
                int count = Database.MListQuery((ArtistEntity a) => a.Friends).UnsafeDeleteMList();
                //tr.Commit();
            }
        }

        [Fact]
        public void DeleteMListEntity()
        {
            using (Transaction tr = new Transaction())
            {
                int count = Database.MListQuery((BandEntity a) => a.Members).UnsafeDeleteMList();

                //tr.Commit();
            }
        }

        [Fact]
        public void DeleteMListEmbedded()
        {
            using (Transaction tr = new Transaction())
            {
                int count = Database.MListQuery((AlbumEntity a) => a.Songs).UnsafeDeleteMList();

                //tr.Commit();
            }
        }


        [Fact]
        public void DeleteManual()
        {
            using (Transaction tr = new Transaction())
            {
                var list = Database.Query<AlbumEntity>().Where(a => ((ArtistEntity)a.Author).Dead).Select(a => a.ToLite()).ToList();

                Database.DeleteList(list);
                //tr.Commit();
            }
        }

        [TableName("#MyView")]
        class MyTempView : IView
        {
            [ViewPrimaryKey]
            public int MyId { get; set; }
        }

        [Fact]
        public void UnsafeDeleteMyView()
        {
            using (Transaction tr = new Transaction())
            {
                Administrator.CreateTemporaryTable<MyTempView>();

                Database.Query<ArtistEntity>().UnsafeInsertView(a => new MyTempView { MyId = (int)a.Id });

                Database.View<MyTempView>().Where(a=>a.MyId > 1).UnsafeDeleteView();

                tr.Commit();
            }

        }
    }
}
