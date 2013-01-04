using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signum.Engine.Maps;
using Signum.Engine;
using System.Data.SqlClient;
using Signum.Utilities;
using System.Threading;

namespace Signum.Test
{
    [TestClass]
    public class GlobalQueryLazyTest
    {
        [ClassInitialize()]
        public static void MyClassInitialize(TestContext testContext)
        {
            Starter.StartAndLoad();
        }

        static Lazy<List<string>> albumNames = Schema.GlobalLazyAutoInvalidate(() => Database.Query<AlbumDN>().Select(a => a.Name).ToList());
        static Lazy<List<Tuple<string, string>>> albumLabelNames = Schema.GlobalLazyAutoInvalidate(() => (from a in Database.Query<AlbumDN>().Where(a=>a.Id > 1)
                                                                                                          join l in Database.Query<LabelDN>() on a.Label equals l
                                                                                                          select Tuple.Create(a.Name, l.Name)).ToList());

        [TestMethod]
        public void NoMetadata()
        {
            var cs = ((Connection)ConnectionScope.Current).ConnectionString;

            Administrator.SetBrockerEnabled(true);

            SqlDependency.Start(cs);


            {
                var query = Database.Query<AlbumDN>().Where(a => a.Id == 1);

                LoadAll();

                query.UnsafeUpdate(a => new AlbumDN { Name = a.Name + "hola" });

                AssertInvalidated(albumes: true, albumLabels: false);

                LoadAll();

                query.UnsafeUpdate(a => new AlbumDN { Name = a.Name.Substring(0, a.Name.Length - 4) }); //rollback

                AssertInvalidated(albumes: true, albumLabels: false);
            }

            {
                var query = Database.Query<AlbumDN>().Where(a => a.Id == 3);

                LoadAll();

                query.UnsafeUpdate(a => new AlbumDN { Name = a.Name + "hola" });

                AssertInvalidated(albumes: true, albumLabels: true);

                LoadAll();

                query.UnsafeUpdate(a => new AlbumDN { Name = a.Name.Substring(0, a.Name.Length - 4) }); //rollback

                AssertInvalidated(albumes: true, albumLabels: true);
            }

            //Does not invalidate
            {
                var query = Database.Query<LabelDN>().Where(a => a.Id == 1);

                LoadAll();

                query.UnsafeUpdate(a => new LabelDN { Name = a.Name + "hola" });

                AssertInvalidated(albumes: false, albumLabels: true);

                LoadAll();

                query.UnsafeUpdate(a => new LabelDN { Name = a.Name.Substring(0, a.Name.Length - 4) }); //rollback

                AssertInvalidated(albumes: false, albumLabels: true);
            }

            //Does not invalidate
            {
                LoadAll();

                var label = new LabelDN
                {
                    Name = "NewLabel"
                }.Save();

                AssertInvalidated(albumes: false, albumLabels: false);

                LoadAll();

                label.Delete();

                AssertInvalidated(albumes: false, albumLabels: false);
            }

            SqlDependency.Stop(cs);
        }

        private static void LoadAll()
        {
            albumNames.Load();
            albumLabelNames.Load();
        }

        private static void AssertInvalidated(bool albumes, bool albumLabels)
        {
            Thread.Sleep(10);

            Assert.AreEqual(albumNames.IsValueCreated, !albumes);
            Assert.AreEqual(albumLabelNames.IsValueCreated, !albumLabels);
        }
    }
}
