using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signum.Engine;
using Signum.Entities;
using System.Diagnostics;
using System.IO;
using Signum.Utilities;
using Signum.Test.Environment;
using System.Data.SqlClient;
using Signum.Engine.Maps;

namespace Signum.Test.LinqProviderUpdateDelete
{
    /// <summary>
    /// Summary description for LinqProvider
    /// </summary>
    [TestClass]
    public class UpdateInsertTest
    {
        [ClassInitialize()]
        public static void MyClassInitialize(TestContext testContext)
        {
            MusicStarter.StartAndLoad();
            Schema.Current.EntityEvents<AlbumEntity>().PreUnsafeInsert += (query, constructor, entityQuery) => constructor;
        }

        [TestInitialize]
        public void Initialize()
        {
            Connector.CurrentLogger = new DebugTextWriter();
        }

        [TestMethod]
        public void InsertSimple()
        {
            using (Transaction tr = new Transaction())
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

        [TestMethod]
        public void InsertSimpleParameter()
        {
            using (Transaction tr = new Transaction())
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

        [TestMethod]
        public void InsertSimpleId()
        {
            using (Transaction tr = new Transaction())
            {
                using (Administrator.DisableIdentity<AlbumEntity>())
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

        [TestMethod]
        public void InsertMListSimple()
        {
            using (Transaction tr = new Transaction())
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


        [TestMethod]
        public void InsertMListParameter()
        {
            using (Transaction tr = new Transaction())
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


        [TestMethod]
        public void InsertMListId()
        {
            using (Transaction tr = new Transaction())
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


        [TestMethod]
        public void InsertSimpleSingle()
        {
            using (Transaction tr = new Transaction())
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

        [TestMethod]
        public void InsertDistinct()
        {
            using (Transaction tr = new Transaction())
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

        [TestMethod]
        public void UnsafeInsertMyView()
        {
            using (Transaction tr = new Transaction())
            {
                Administrator.CreateTemporaryTable<MyTempView>();

                Database.Query<ArtistEntity>().Where(a => a.Name.StartsWith("M")).UnsafeInsertView(a => new MyTempView { Artist = a.ToLite() });
                
                tr.Commit();
            }

        }
    }
}
