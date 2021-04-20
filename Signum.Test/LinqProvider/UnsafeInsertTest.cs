using System.Linq;
using Xunit;
using Signum.Engine;
using Signum.Entities;
using Signum.Utilities;
using Signum.Test.Environment;
using Signum.Engine.Maps;

namespace Signum.Test.LinqProviderUpdateDelete
{
    /// <summary>
    /// Summary description for LinqProvider
    /// </summary>
    public class UpdateInsertTest
    {
        public UpdateInsertTest()
        {
            MusicStarter.StartAndLoad();
            Schema.Current.EntityEvents<AlbumEntity>().PreUnsafeInsert += (query, constructor, entityQuery) => constructor;
            Connector.CurrentLogger = new DebugTextWriter();
        }

        [Fact]
        public void InsertSimple()
        {
            using (var tr = new Transaction())
            {
                int value = Database.Query<AlbumEntity>().UnsafeInsert(a => new AlbumEntity
                {
                    Author = a.Author,
                    BonusTrack = a.BonusTrack,
                    Label = a.Label,
                    Name = a.Name + "copy",
                    State = a.State,
                    Year = a.Year,
                }.SetReadonly(_ => _.Ticks, a.Ticks));

                //tr.Commit();
            }
        }

        [Fact]
        public void InsertSimpleParameter()
        {
            using (var tr = new Transaction())
            {
                int value = Database.Query<AlbumEntity>().Select(a => new AlbumEntity
                {
                    Author = a.Author,
                    BonusTrack = a.BonusTrack,
                    Label = a.Label,
                    Name = a.Name + "copy",
                    State = a.State,
                    Year = a.Year,
                }.SetReadonly(_ => _.Ticks, a.Ticks)).UnsafeInsert(a => a);

                //tr.Commit();
            }
        }

        [Fact]
        public void InsertSimpleId()
        {
            using (var tr = new Transaction())
            {
                using (Administrator.SaveDisableIdentity<AlbumEntity>())
                {
                    int value = Database.Query<AlbumEntity>().UnsafeInsert(a => new AlbumEntity
                    {
                        Author = a.Author,
                        BonusTrack = a.BonusTrack,
                        Label = a.Label,
                        Name = a.Name + "copy",
                        State = a.State,
                        Year = a.Year,
                    }.SetReadonly(_ => _.Ticks, a.Ticks)
                    .SetReadonly(_ => _.Id, (int)a.Id + 100));
                }

                //tr.Commit();
            }
        }

        [Fact]
        public void InsertMListSimple()
        {
            using (var tr = new Transaction())
            {
                int value = Database.MListQuery((AlbumEntity a) => a.Songs)
                    .UnsafeInsertMList((AlbumEntity a) => a.Songs, mle => new MListElement<AlbumEntity, SongEmbedded>
                {
                    Parent = mle.Parent,
                    Element = mle.Element,
                    Order = mle.Order,
                });
                //tr.Commit();
            }
        }


        [Fact]
        public void InsertMListParameter()
        {
            using (var tr = new Transaction())
            {
                int value = Database.MListQuery((AlbumEntity a) => a.Songs)
                    .Select(mle => new MListElement<AlbumEntity, SongEmbedded>
                    {
                        Parent = mle.Parent,
                        Element = mle.Element,
                        Order = mle.Order,
                    })
                    .UnsafeInsertMList((AlbumEntity a) => a.Songs, mle => mle);
                //tr.Commit();
            }
        }


        [Fact]
        public void InsertMListId()
        {
            using (var tr = new Transaction())
            {
                using (Administrator.DisableIdentity((AlbumEntity a)=>a.Songs))
                {
                    int value = Database.MListQuery((AlbumEntity a) => a.Songs)
                        .UnsafeInsertMList((AlbumEntity a) => a.Songs, mle => new MListElement<AlbumEntity, SongEmbedded>
                        {
                            Parent = mle.Parent,
                            Element = mle.Element,
                            RowId = (int)mle.RowId + 1000,
                            Order = mle.Order,
                        });
                }
                //tr.Commit();
            }
        }


        [Fact]
        public void InsertSimpleSingle()
        {
            using (var tr = new Transaction())
            {
                int value = Database.Query<AlbumEntity>().UnsafeInsert(a => new AlbumEntity
                {
                    Author = a.Author,
                    BonusTrack = a.BonusTrack,
                    Label = Database.Query<LabelEntity>().Single(l => l.Is(a.Label)),
                    Name = a.Name + "copy",
                    State = a.State,
                    Year = a.Year,
                }.SetReadonly(_ => _.Ticks, a.Ticks));
                //tr.Commit();
            }
        }

        [Fact]
        public void InsertDistinct()
        {
            using (var tr = new Transaction())
            {
                int value = Database.Query<LabelEntity>().Select(a => a.Country).Distinct().UnsafeInsert(c => new CountryEntity
                {
                    Name = "Clone of " + c.Name,
                    Ticks = 0
                });
                //tr.Commit();
            }
        }

        [TableName("#MyView")]
        class MyTempView : IView
        {
            [ViewPrimaryKey]
            public Lite<ArtistEntity> Artist { get; set; }
        }

        [Fact]
        public void UnsafeInsertMyView()
        {
            using (var tr = new Transaction())
            {
                Administrator.CreateTemporaryTable<MyTempView>();

                Database.Query<ArtistEntity>().Where(a => a.Name.StartsWith("M")).UnsafeInsertView(a => new MyTempView { Artist = a.ToLite() });

                tr.Commit();
            }

        }
    }
}
