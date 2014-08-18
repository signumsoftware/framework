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
            Schema.Current.EntityEvents<AlbumDN>().PreUnsafeInsert += (query, constructor, entityQuery) => { };
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
                int value = Database.Query<AlbumDN>().UnsafeInsert(a => new AlbumDN
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
        public void InsertSimpleId()
        {
            using (Transaction tr = new Transaction())
            {
                using (Administrator.DisableIdentity<AlbumDN>())
                {
                    int value = Database.Query<AlbumDN>().UnsafeInsert(a => new AlbumDN
                    {
                        Author = a.Author,
                        BonusTrack = a.BonusTrack,
                        Label = a.Label,
                        Name = a.Name + "copy",
                        State = a.State,
                        Year = a.Year,
                    }.SetReadonly(_ => _.Ticks, a.Ticks)
                    .SetReadonly(_ => _.Id, a.Id + 100));
                }

                //tr.Commit();
            }
        }

        [TestMethod]
        public void InsertMListSimple()
        {
            using (Transaction tr = new Transaction())
            {
                int value = Database.MListQuery((AlbumDN a) => a.Songs)
                    .UnsafeInsertMList((AlbumDN a) => a.Songs, mle => new MListElement<AlbumDN, SongDN>
                {
                    Parent = mle.Parent,
                    Element = mle.Element,
                    Order = mle.Order,
                });
                //tr.Commit();
            }
        }


        [TestMethod]
        public void InsertMListId()
        {
            using (Transaction tr = new Transaction())
            {
                using (Administrator.DisableIdentity((AlbumDN a)=>a.Songs))
                {
                    int value = Database.MListQuery((AlbumDN a) => a.Songs)
                        .UnsafeInsertMList((AlbumDN a) => a.Songs, mle => new MListElement<AlbumDN, SongDN>
                        {
                            Parent = mle.Parent,
                            Element = mle.Element,
                            RowId = mle.RowId + 1000,
                            Order = mle.Order,
                        });
                }
                //tr.Commit();
            }
        }
    }
}
