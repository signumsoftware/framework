using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signum.Engine;
using Signum.Entities;
using Signum.Test.Environment;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using System.Collections.Generic;
using System.Linq;

namespace Signum.Test.LinqProvider
{
    /// <summary>
    /// Summary description for LinqProvider
    /// </summary>
    [TestClass]
    public class GroupByTest
    {
        [ClassInitialize()]
        public static void MyClassInitialize(TestContext testContext)
        {
            MusicStarter.StartAndLoad();
        }

        [TestInitialize]
        public void Initialize()
        {
            Connector.CurrentLogger = new DebugTextWriter();
        }

        [TestMethod]
        public void GroupStringByEnum()
        {
            var list = Database.Query<ArtistEntity>().GroupBy(a => a.Sex, a => a.Name).ToList();
        }


        [TestMethod]
        public void GroupStringByEnumSimilar()
        {
            var queryA = (from a in Database.Query<ArtistEntity>()
                          group a.Name by a.Sex into g
                          select g).QueryText();

            var queryN = Database.Query<ArtistEntity>().GroupBy(a => a.Sex, a => a.Name).QueryText();

            Assert.AreEqual(queryN, queryA);
        }

        [TestMethod]
        public void GroupMultiAggregate()
        {
            var sexos = from a in Database.Query<ArtistEntity>()
                        group a.Name.Length by a.Sex into g
                        select new
                        {
                            Key = g.Key,
                            Count = g.Count(),
                            Sum = g.Sum(),
                            Min = g.Min(),
                            Max = g.Max(),
                            Avg = g.Average(),
                        };
            sexos.ToList();
        }

        [TestMethod]
        public void GroupCountNull()
        {
            var sexes = from a in Database.Query<ArtistEntity>()
                        group a by a.Sex into g
                        select new
                        {
                            Key = g.Key,
                            Count = g.Count(), //Fast
                            CountNames = g.Count(a => a.Name != null), //Fast
                            CountNullFast = g.Count(a => (a.Name == null ? "hi" : null) != null), //Fast
                            CountNullFast1 = g.Where(a => a.Name == null).Count(), //Fast
                            CountNullFast2= g.Count(a => a.Name == null), //Fast
                            CountLastAward = g.Count(a => a.LastAward != null), //Fast
                        };
            sexes.ToList();
        }

        [TestMethod]
        public void GroupCountDistinctFast()
        {
            var sexes = from a in Database.Query<ArtistEntity>()
                        group a by a.Sex into g
                        select new
                        {
                            Key = g.Key,
                            Count1 = g.Select(a => a.Name).Where(a => a != null).Distinct().Count(), //Fast
                            Count2 = g.Where(a => a.Name != null).Select(a => a.Name).Distinct().Count(), //Fast
                            Count3 = g.Select(a => a.Name).Distinct().Where(a => a != null).Count(), //Fast
                            Count4 = g.Select(a => a.Name).Distinct().Count(a => a != null), //Fast
                        };
            sexes.ToList();
        }

        [TestMethod]
        public void RootCountDistinct()
        {
            var count = Database.Query<ArtistEntity>().Select(a => a.Name).Where(a => a != null).Distinct().Count();
        }


        [TestMethod]
        public void GroupCountDistinctSlow()
        {
            var sexes = from a in Database.Query<ArtistEntity>()
                        group a by a.Sex into g
                        select new
                        {
                            Key = g.Key,
                            Count1 = g.Select(a => a.Name).Distinct().Count(), //Slow
                            Count2 = g.Distinct().Count(), //Slow
                        };
            sexes.ToList();
        }

        [TestMethod]
        public void GroupMultiAggregateNoKeys()
        {
            var sexos = from a in Database.Query<ArtistEntity>()
                        group a.Name.Length by new { } into g
                        select new
                        {
                            Key = g.Key,
                            Count = g.Count(),
                            Sum = g.Sum(),
                            Min = g.Min(),
                            Max = g.Max(),
                            Avg = g.Average(),
                        };
            sexos.ToList();
        }


        [TestMethod]
        public void GroupStdDev()
        {
            var sexos = from a in Database.Query<ArtistEntity>()
                        group a.Name.Length by a.Sex into g
                        select new
                        {
                            Key = g.Key,
                            StdDev = (double?)g.StdDev(),
                            StdDevInMemory = GetStdDev(g.ToList()),
                            StdDevP = (double?)g.StdDevP(),
                            StdDevPInMemory = GetStdDevP(g.ToList()),
                        };
            var list = sexos.ToList();
            list.ForEach(a => Assert2.AreSimilar(a.StdDev, a.StdDevInMemory));
            list.ForEach(a => Assert2.AreSimilar(a.StdDevP, a.StdDevPInMemory));
        }

        private double? GetStdDev(List<int> list)
        {
            return list.StdDev();
        }

        private double? GetStdDevP(List<int> list)
        {
            return list.StdDevP();
        }

        [TestMethod]
        public void GroupEntityByEnum()
        {
            var list = Database.Query<ArtistEntity>().GroupBy(a => a.Sex).ToList();
        }


        //[TestMethod]
        //public void GroupEntityByTypeFie()
        //{
        //    var list = Database.Query<AlbumEntity>().GroupBy(a => a.GetType()).ToList();
        //}


        [TestMethod]
        public void GroupEntityByTypeIb()
        {
            var list = Database.Query<AwardNominationEntity>().GroupBy(a => a.Award.EntityType).ToList();
        }

        [TestMethod]
        public void WhereGroup()
        {
            var list = Database.Query<ArtistEntity>().Where(a => a.Dead).GroupBy(a => a.Sex).ToList();
        }

        [TestMethod]
        public void GroupWhere()
        {
            var list = (from a in Database.Query<ArtistEntity>()
                        group a by a.Sex into g
                        select new { Sex = g.Key, DeadArtists = g.Where(a => a.Dead).ToList() }).ToList();
        }

        [TestMethod]
        public void GroupCount()
        {
            var songsAlbum = (from a in Database.Query<ArtistEntity>()
                              group a by a.Sex into g
                              select new { Sex = g.Key, Count = g.Count() }).ToList();
        }

        [TestMethod]
        public void GroupCountInterval()
        {
            var songsAlbum = (from a in Database.Query<ArtistEntity>()
                              group a by a.Id < 10 ? 0 : 10 into g
                              select new { Id = g.Key, Count = g.Count() }).ToList();
        }

        [TestMethod]
        public void GroupWhereCount()
        {
            var songsAlbum = (from a in Database.Query<ArtistEntity>()
                              group a by a.Sex into g
                              select new { Sex = g.Key, DeadArtists = (int?)g.Count(a => a.Dead) }).ToList();
        }

        [TestMethod]
        public void GroupEntityByTypeFieCount()
        {
            var list = Database.Query<AlbumEntity>().GroupBy(a => a.GetType()).Select(gr => new { gr.Key, Count = gr.Count() }).ToList();
        }


        [TestMethod]
        public void GroupEntityByTypeIbCount()
        {
            var list = Database.Query<AlbumEntity>().GroupBy(a => a.Author.GetType()).Select(gr => new { gr.Key, Count = gr.Count() }).ToList();
        }

        [TestMethod]
        public void GroupExpandKey()
        {
            var songs = (from a in Database.Query<AlbumEntity>()
                         group a by a.Label.Name into g
                         select new { g.Key, Count = g.Count() }).ToList();
        }

        [TestMethod]
        public void GroupExpandResult()
        {
            var songs = (from a in Database.Query<AlbumEntity>()
                         group a by a.Label into g
                         select new { g.Key.Name, Count = g.Count() }).ToList();
        }

        [TestMethod]
        public void GroupSum()
        {
            var songsAlbum = (from a in Database.Query<ArtistEntity>()
                              group a by a.Sex into g
                              select new { Sex = g.Key, Max = g.Sum(a => a.Name.Length) }).ToList();
        }

        [TestMethod]
        public void GroupMax()
        {
            var songsAlbum = (from a in Database.Query<ArtistEntity>()
                              group a by a.Sex into g
                              select new { Sex = g.Key, Max = g.Max(a => a.Name.Length) }).ToList();
        }

        [TestMethod]
        public void GroupMin()
        {
            var songsAlbum = (from a in Database.Query<ArtistEntity>()
                              group a by a.Sex into g
                              select new { Sex = g.Key, Min = g.Min(a => a.Name.Length) }).ToList();
        }

        [TestMethod]
        public void GroupAverage()
        {
            var songsAlbum = (from a in Database.Query<ArtistEntity>()
                              group a by a.Sex into g
                              select new { Sex = g.Key, Avg = g.Average(a => a.Name.Length) }).ToList();
        }

        [TestMethod]
        public void RootCount()
        {
            var songsAlbum = Database.Query<ArtistEntity>().Count();
        }


        [TestMethod]
        public void RootCountWhere()
        {
            var songsAlbum = Database.Query<ArtistEntity>().Count(a => a.Name.StartsWith("M"));
        }

        [TestMethod]
        public void RootCountWhereZero()
        {
            Assert.AreEqual(0, Database.Query<ArtistEntity>().Count(a => false));
        }

        [TestMethod]
        public void RootSum()
        {
            var songsAlbum = Database.Query<ArtistEntity>().Sum(a => a.Name.Length);
        }


        [TestMethod]
        public void RootSumNoArgs()
        {
            var songsAlbum = Database.Query<ArtistEntity>().Select(a => a.Name.Length).Sum();
        }

        [TestMethod]
        public void SumWhere()
        {
            var songsAlbum = Database.Query<BandEntity>().Where(a => a.Members.Sum(m => m.Name.Length) > 0).ToList();
        }

        [TestMethod]
        public void SumSimplification()
        {
            var songsAlbum = Database.Query<BandEntity>().Select(a => new { a.Name, Sum = a.Members.Sum(m => m.Name.Length) }).Select(a => a.Name).ToList();
        }

        [TestMethod]
        public void RootSumZero()
        {
            Assert.AreEqual(0, Database.Query<ArtistEntity>().Where(a => false).Sum(a => a.Name.Length));
        }

        [TestMethod]
        public void RootSumNull()
        {
            Assert.IsNull(Database.Query<ArtistEntity>().Where(a => false).Sum(a => (int?)a.Name.Length));
        }

        [TestMethod]
        public void RootSumSomeNull()
        {
            Assert.IsTrue(Database.Query<AwardNominationEntity>().Sum(a => (int)a.Award.Id.Object) > 0);
        }

        [TestMethod]
        public void RootMax()
        {
            var songsAlbum = Database.Query<ArtistEntity>().Max(a => a.Name.Length);
        }

        [TestMethod]
        public void RootMaxNoArgs()
        {
            var songsAlbum = Database.Query<ArtistEntity>().Select(a => a.Name.Length).Max();
        }

        [TestMethod]
        public void RootMaxException()
        {
            Assert2.Throws<FieldReaderException>(() => Database.Query<ArtistEntity>().Where(a => false).Max(a => a.Name.Length));
        }

        [TestMethod]
        public void RootMin()
        {
            var songsAlbum = Database.Query<ArtistEntity>().Min(a => a.Name.Length);
        }

        [TestMethod]
        public void MinEnum()
        {
            var list = Database.Query<ArtistEntity>().GroupBy(a => a.Sex).Select(gr => gr.Min(a => a.Status));
            var list2 = Database.Query<ArtistEntity>().GroupBy(a => a.Sex).Select(gr => gr.Where(a => a.Id > 10).Min(a => a.Status));
            var minSex = Database.Query<ArtistEntity>().Min(a => a.Sex);
        }

        [TestMethod]
        public void MinEnumNullable()
        {
            var minSex = Database.Query<ArtistEntity>().Where(a => false).Min(a => (Sex?)a.Sex);
            var minSexs = Database.Query<BandEntity>().Select(b => b.Members.Where(a => false).Min(a => (Sex?)a.Sex));
        }


        [TestMethod]
        public void RootMinException()
        {
            Assert2.Throws<FieldReaderException>(() => Database.Query<ArtistEntity>().Where(a => false).Min(a => a.Name.Length));
        }

        [TestMethod]
        public void RootMinNullable()
        {
            var min = Database.Query<ArtistEntity>().Where(a => false).Min(a => (int?)a.Name.Length);
        }

        [TestMethod]
        public void RootAverage()
        {
            var songsAlbum = Database.Query<ArtistEntity>().Average(a => a.Name.Length);
        }

        [TestMethod]
        public void GroupBySelectSelect()
        {
            var artistsBySex =
                Database.Query<ArtistEntity>()
                .GroupBy(a => a.Sex)
                .Select(g => g)
                .Select(g => new { Sex = g.Key, Count = g.Count() }).ToList();
        }

        [TestMethod]
        public void JoinGroupPair()
        {
            var list = (from a in Database.Query<AlbumEntity>()
                        group new { a, HasBonusTrack = a.BonusTrack != null } by a.Label into g
                        select new
                        {
                            Label = g.Key,
                            Albums = g.Count(),
                            BonusTracks = g.Count(a => a.HasBonusTrack)
                        }).ToList();
        }


        [TestMethod]
        public void GroupByEntity()
        {
            var list = (from a in Database.Query<AlbumEntity>()
                        group a by a.Label into g
                        select g.Key.ToLite()).ToList();
        }

        [TestMethod]
        public void GroupByEntityExpand()
        {
            var list = (from a in Database.Query<AlbumEntity>()
                        where a.Label.Name != "whatever"
                        group a by a.Label into g
                        select new
                        {
                            Label = g.Key.Name,
                            Albums = g.Count(),
                        }).ToList();
        }

        [TestMethod]
        public void SelectExpansionCount()
        {
            var albums = (from b in Database.Query<BandEntity>()
                          from a in b.Members
                          let count = Database.Query<ArtistEntity>().Count(a2 => a2.Sex == a.Sex) //a should be expanded here
                          select new
                          {
                              Album = a.ToLite(),
                              Count = count
                          }).ToList();
        }

        [TestMethod]
        public void GroupBySelectMany()
        {
            var songsAlbum = Database.Query<ArtistEntity>().GroupBy(a => a.Sex).SelectMany(a => a).ToList();
        }

        [TestMethod]
        public void SumSum()
        {
            var first = Database.Query<BandEntity>().Sum(b => b.Members.Sum(m => (int)m.Id.Object));
        }

        [TestMethod]
        public void SumGroupbySum()
        {
            var first = Database.Query<ArtistEntity>().GroupBy(a => a.Status).Select(gr => gr.Sum(b => b.Friends.Sum(m => (int)m.Id.Object)));
        }



        [TestMethod]
        public void MinMax()
        {
            var first = Database.Query<BandEntity>().Min(b => b.Members.Max(m => m.Id));
        }

        [TestMethod]
        public void MinGroupByMax()
        {
            var first = Database.Query<ArtistEntity>().GroupBy(a => a.Status).Select(gr => gr.Min(b => b.Friends.Max(m => m.Id)));
        }

        [TestMethod]
        public void GroupbyAggregateImplicitJoin()
        {
            var first = Database.Query<AlbumEntity>().GroupBy(a => a.Year).Select(gr => gr.Max(a => a.Label.Name)).ToList();
        }

        [TestMethod]
        public void GroupByTake()
        {
            var list = (from a in Database.Query<AlbumEntity>()
                        group a by new { Author = a.Author.ToLite(), Year = a.Year / 2 } into g
                        select new
                        {
                            Author = g.Key.Author,
                            Year = g.Key.Year,
                            Count = g.Count()
                        }).Take(10).ToList();
        }


        [TestMethod]
        public void GroupByTakeSomeKeys()
        {
            var list = (from a in Database.Query<AlbumEntity>()
                        group a by new { Author = a.Author.ToLite(), Year = a.Year / 2 } into g
                        select new
                        {
                            Author = g.Key.Author,
                            //Year = g.Key.Year,
                            Count = g.Count()
                        }).Take(10).ToList();
        }

        [TestMethod]
        public void GroupByExpandGroupBy()
        {
            var list = (from a in Database.Query<ArtistEntity>()
                        group a by a.Sex into g
                        select new
                        {
                            g.Key,
                            MaxFriends = g.Max(a => a.Friends.Count),
                        }).ToList();
        }

        [TestMethod]
        public void LetTrick()
        {
            var list = (from a in Database.Query<ArtistEntity>()
                        let friend = a.Friends
                        select new
                        {
                            Artist = a.ToLite(),
                            Friends = friend.Count(), // will also be expanded but then simplified
                            FemaleFriends = friend.Count(f => f.Entity.Sex == Sex.Female)
                        }).ToList();
        }

        [TestMethod]
        public void DistinctGroupByForce()
        {
            var list = Database.Query<ArtistEntity>()
                .Select(a => new { Initials = a.Name.Substring(0, 1), a.Sex })
                .Distinct()
                .GroupBy(a => a.Initials)
                .Select(gr => new { gr.Key, Count = gr.Count() })
                .ToList();
        }


        [TestMethod]
        public void GroupByCount()
        {
            var list = Database.Query<AlbumEntity>()
                .GroupBy(a => a.Songs.Count)
                .Select(gr => new { NumSongs = gr.Key, Count = gr.Count() })
                .ToList();
        }

        [TestMethod]
        public void FirstLastMList()
        {
            var list = (from a in Database.Query<AlbumEntity>()
                        where a.Songs.Count > 1
                        select new
                        {
                            FirstName = a.Songs.OrderBy(s => s.Name).FirstOrDefault().Name,
                            FirstDuration = a.Songs.OrderBy(s => s.Name).FirstOrDefault().Duration,
                            Last = a.Songs.OrderByDescending(s => s.Name).FirstOrDefault()
                        }).ToList();

            Assert.IsTrue(list.All(a => a.FirstName != a.Last.Name));
        }

        [TestMethod]
        public void FirstLastGroup()
        {
            var list = (from mle in Database.MListQuery((AlbumEntity a)=>a.Songs)
                        group mle.Element by mle.Parent into g
                        where g.Count() > 1
                        select new
                        {
                            FirstName = g.OrderBy(s => s.Name).FirstOrDefault().Name,
                            FirstDuration = g.OrderBy(s => s.Name).FirstOrDefault().Duration,
                            Last = g.OrderByDescending(s => s.Name).FirstOrDefault()
                        }).ToList();

            Assert.IsTrue(list.All(a => a.FirstName != a.Last.Name));


        }


        [TestMethod]
        public void FirstLastList()
        {
            var list = (from a in Database.Query<AlbumEntity>()
                        select a.Songs into songs
                        where songs.Count > 1
                        select new
                        {
                            FirstName = songs.OrderBy(s => s.Name).FirstOrDefault().Name,
                            FirstDuration = songs.OrderBy(s => s.Name).FirstOrDefault().Duration,
                            Last = songs.OrderByDescending(s => s.Name).FirstOrDefault()
                        }).ToList();

            Assert.IsTrue(list.All(a => a.FirstName != a.Last.Name));


        }
    }
}
