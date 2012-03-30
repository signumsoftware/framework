using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Engine.Maps;
using Signum.Engine;
using Signum.Test.Properties;
using System.IO;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signum.Entities.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities;
using Microsoft.SqlServer.Types;

namespace Signum.Test
{
    public static class Starter
    {
        static bool startedAndLoaded = false;
        public static void StartAndLoad()
        {
            if (!startedAndLoaded)
            {
                Start(UserConnections.Replace(Settings.Default.SignumTest));

                Administrator.TotalGeneration();

                Schema.Current.Initialize();

                Load();

                startedAndLoaded = true;
            }
        }

        public static void Start(string connectionString)
        {
            DBMS dbms = DBMS.SqlServer2008;

            SchemaBuilder sb = new SchemaBuilder(dbms);
            DynamicQueryManager dqm = new DynamicQueryManager();
            if (dbms == DBMS.SqlCompact)
                Connector.Default = new SqlCeConnector(@"Data Source=C:\BaseDatos.sdf", sb.Schema, dqm);
            else
                Connector.Default = new SqlConnector(connectionString, sb.Schema, dqm);

            StartMusic(sb, dqm);
        }

        public static void StartMusic(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.Schema.Settings.DBMS == DBMS.SqlCompact || sb.Schema.Settings.DBMS == DBMS.SqlServer2005)
            {
                sb.Settings.OverrideAttributes<AlbumDN>(a => a.Songs[0].Duration, new Signum.Entities.IgnoreAttribute());
                sb.Settings.OverrideAttributes<AlbumDN>(a => a.BonusTrack.Duration, new Signum.Entities.IgnoreAttribute());
            }

            sb.Include<AlbumDN>();
            sb.Include<NoteDN>();
            sb.Include<NoteWithDateDN>();
            sb.Include<AlertDN>();
            sb.Include<PersonalAwardDN>();
            sb.Include<AwardNominationDN>();

            dqm[typeof(AlbumDN)] = (from a in Database.Query<AlbumDN>()
                                    select new
                                    {
                                        Entity = a.ToLite(),
                                        a.Id,
                                        Author = a.Author.ToLite(),
                                        Label = a.Label.ToLite(),
                                        a.Name,
                                        a.Year
                                    }).ToDynamic();

            dqm[typeof(NoteDN)] = (from a in Database.Query<NoteDN>()
                                           select new
                                           {
                                               Entity = a.ToLite(),
                                               a.Id,
                                               a.Text,
                                               a.Target
                                           }).ToDynamic();

            dqm[typeof(NoteWithDateDN)] = (from a in Database.Query<NoteWithDateDN>()
                                    select new
                                    {
                                        Entity = a.ToLite(),
                                        a.Id,
                                        a.Text,
                                        Target = a.Target.ToLite(),
                                        a.CreationTime,
                                    }).ToDynamic();

            dqm[typeof(ArtistDN)] = (from a in Database.Query<ArtistDN>()
                                     select new
                                     {
                                         Entity = a.ToLite(),
                                         a.Id,
                                         a.Name,
                                         a.IsMale,
                                         a.Sex,
                                         a.Dead,
                                         LastAward = a.LastAward.ToLite(),
                                     }).ToDynamic();

            dqm.RegisterExpression((IAuthorDN au) => Database.Query<AlbumDN>().Where(a => a.Author == au), () => "Albums", "Albums"); 

            dqm[typeof(BandDN)] = (from a in Database.Query<BandDN>()
                                   select new
                                   {
                                       Entity = a.ToLite(),
                                       a.Id,
                                       a.Name,
                                       LastAward = a.LastAward.ToLite(),
                                   }).ToDynamic();


            dqm[typeof(LabelDN)] = (from a in Database.Query<LabelDN>()
                                    select new
                                    {
                                        Entity = a.ToLite(),
                                        a.Id,
                                        a.Name,
                                    }).ToDynamic();


            dqm[typeof(AmericanMusicAwardDN)] = (from a in Database.Query<AmericanMusicAwardDN>()
                                                 select new
                                                 {
                                                     Entity = a.ToLite(),
                                                     a.Id,
                                                     a.Year,
                                                     a.Category,
                                                     a.Result,
                                                 }).ToDynamic();

            dqm[typeof(GrammyAwardDN)] = (from a in Database.Query<GrammyAwardDN>()
                                          select new
                                          {
                                              Entity = a.ToLite(),
                                              a.Id,
                                              a.Year,
                                              a.Category,
                                              a.Result
                                          }).ToDynamic();

            dqm[typeof(PersonalAwardDN)] = (from a in Database.Query<PersonalAwardDN>()
                                            select new
                                            {
                                                Entity = a.ToLite(),
                                                a.Id,
                                                a.Year,
                                                a.Category,
                                                a.Result
                                            }).ToDynamic();

            dqm[typeof(AwardNominationDN)] = (from a in Database.Query<AwardNominationDN>()
                                            select new
                                            {
                                                Entity = a.ToLite(),
                                                a.Id,
                                                a.Award,
                                                a.Author
                                            }).ToDynamic();
            
            var alertExpr = Linq.Expr((AlertDN a) => new
            {
                Entity = a.ToLite(),
                a.Id,
                a.AlertDate,
                Text = a.Text.Etc(100),
                a.CheckDate,
                Target = a.Entity
            });

            dqm[typeof(AlertDN)] = Database.Query<AlertDN>().Select(alertExpr).ToDynamic();
            dqm[AlertQueries.NotAttended] = Database.Query<AlertDN>().Where(a => a.NotAttended).Select(alertExpr).ToDynamic();
            dqm[AlertQueries.Attended] = Database.Query<AlertDN>().Where(a => a.Attended).Select(alertExpr).ToDynamic();
            dqm[AlertQueries.Future] = Database.Query<AlertDN>().Where(a => a.Future).Select(alertExpr).ToDynamic();
            
            dqm[typeof(IAuthorDN)] = DynamicQuery.Manual((request, descriptions) =>
                                    {
                                        var one = (from a in Database.Query<ArtistDN>()
                                                   select new
                                                   {
                                                       Entity = a.ToLite<IAuthorDN>(),
                                                       a.Id,
                                                       Type = "Artist",
                                                       a.Name,
                                                       Lonely = a.Lonely(),
                                                       LastAward = a.LastAward.ToLite()
                                                   }).ToDQueryable(descriptions)
                                                    .SelectMany(request.Multiplications)
                                                    .Where(request.Filters)
                                                    .OrderBy(request.Orders)
                                                    .Select(request.Columns)
                                                    .TryPaginatePartial(request.MaxElementIndex);


                                        var two = (from a in Database.Query<BandDN>()
                                                   select new
                                                   {
                                                       Entity = a.ToLite<IAuthorDN>(),
                                                       a.Id,
                                                       Type = "Band",
                                                       a.Name,
                                                       Lonely = a.Lonely(),
                                                       LastAward = a.LastAward.ToLite()
                                                   }).ToDQueryable(descriptions)
                                                    .SelectMany(request.Multiplications)
                                                    .Where(request.Filters)
                                                    .OrderBy(request.Orders)
                                                    .Select(request.Columns)
                                                    .TryPaginatePartial(request.MaxElementIndex);

                                        return one.Concat(two).OrderBy(request.Orders).TryPaginate(request.ElementsPerPage, request.CurrentPage);
                                    })
                                    .Column(a => a.Entity, cl => cl.Implementations = new ImplementedByAttribute(typeof(ArtistDN), typeof(BandDN)))
                                    .Column(a => a.LastAward, cl => cl.Implementations = new ImplementedByAllAttribute());
        }

        public const string Japan = "Japan";

        public static SqlHierarchyId FirstChild(this SqlHierarchyId parent)
        {
            return parent.GetDescendant(SqlHierarchyId.Null, SqlHierarchyId.Null);
        }

        public static SqlHierarchyId NextSibling(this SqlHierarchyId sibling)
        {
            return sibling.GetAncestor(1).GetDescendant(sibling, SqlHierarchyId.Null);
        }
        
        public static void Load()
        {
            var ama = new AmericanMusicAwardDN { Category = "Indie Rock", Year = 1991, Result = AwardResult.Nominated }.Save();

            BandDN smashingPumpkins = new BandDN
            {
                Name = "Smashing Pumpkins",
                Members = "Billy Corgan, James Iha, D'arcy Wretzky, Jimmy Chamberlin"
                .Split(',').Select(s => new ArtistDN { Name = s.Trim(), Sex = s.Contains("Wretzky") ? Sex.Female : Sex.Male, Status = s.Contains("Wretzky") ? Status.Married: (Status?)null }).ToMList(),
                LastAward = ama,
            };

            CountryDN usa = new CountryDN { Name = "USA" };
            CountryDN japan = new CountryDN { Name = Japan };

            smashingPumpkins.Members.ForEach(m => m.Friends = smashingPumpkins.Members.Where(a => a.Sex != m.Sex).Select(a => a.ToLiteFat()).ToMList());

            new NoteWithDateDN { CreationTime = DateTime.Now.AddHours(+8), Text = "American alternative rock band", Target = smashingPumpkins }.Save();

            LabelDN virgin = new LabelDN { Name = "Virgin", Country = usa, Node = SqlHierarchyId.GetRoot().FirstChild() };

            new AlbumDN
            {
                Name = "Siamese Dream",
                Year = 1993,
                Author = smashingPumpkins,
                Songs = { new SongDN { Name = "Disarm" } },
                Label = virgin
            }.Save();

            AlbumDN mellon = new AlbumDN
            {
                Name = "Mellon Collie and the Infinite Sadness",
                Year = 1995,
                Author = smashingPumpkins,
                Songs = 
                { 
                    new SongDN { Name = "Zero", Duration = TimeSpan.FromSeconds(123) }, 
                    new SongDN { Name = "1976" }, 
                    new SongDN { Name = "Tonight, Tonight", Duration = TimeSpan.FromSeconds(376) } 
                },
                BonusTrack = new SongDN { Name = "Jellybelly" },
                Label = virgin
            };

            mellon.Save();

            new NoteWithDateDN { CreationTime = DateTime.Now.AddDays(-100).AddHours(-8), Text = "The blue one with the angel", Target = mellon }.Save();

            LabelDN wea = new LabelDN { Name = "WEA International", Country = usa, Owner = virgin.ToLite(), Node = virgin.Node.FirstChild() };

            new AlbumDN
            {
                Name = "Zeitgeist",
                Year = 2007,
                Author = smashingPumpkins,
                Songs = { new SongDN { Name = "Tarantula" } },
                BonusTrack = new SongDN{Name = "1976"},
                Label = wea,
            }.Save();

            new AlbumDN
            {
                Name = "American Gothic", 
                Year = 2008,
                Author = smashingPumpkins,
                Songs = { new SongDN { Name = "The Rose March", Duration = TimeSpan.FromSeconds(276) } },
                Label = wea,
            }.Save();

            var pa  =new PersonalAwardDN { Category = "Best Artist", Year = 1983, Result = AwardResult.Won }.Save();

            ArtistDN michael = new ArtistDN
            {
                Name = "Michael Jackson",
                Dead = true,
                LastAward = pa,
                Status = Status.Single,
            };

            new NoteWithDateDN { CreationTime = new DateTime(2009, 6, 25, 0, 0, 0), Text = "Death on June, 25th", Target = michael }.Save();

            LabelDN universal = new LabelDN { Name = "UMG Recordings", Country = usa, Node = virgin.Node.NextSibling()  };

            new AlbumDN
            {
                Name = "Ben",
                Year = 1972,
                Author = michael,
                Songs = { new SongDN { Name = "Ben" } },
                BonusTrack = new SongDN{Name = "Michael"},
                Label = universal,
            }.Save();

            LabelDN sony = new LabelDN { Name = "Sony", Country = japan, Node = universal.Node.NextSibling() };

            new AlbumDN
            {
                Name = "Thriller",
                Year = 1982,
                Author = michael,
                Songs = "Wanna Be Startin' Somethin', Thriller, Beat It"
                .Split(',').Select(s => new SongDN { Name = s.Trim() }).ToMList(),
                BonusTrack = new SongDN{Name = "Billie Jean"},
                Label = sony
            }.Save();

            LabelDN mjj = new LabelDN { Name = "MJJ", Country = usa, Owner = sony.ToLite(), Node = sony.Node.FirstChild() };

            new AlbumDN
            {
                Name = "Bad",
                Year = 1989,
                Author = michael,
                Songs = "Bad, Man in the Mirror, Dirty Diana, Smooth Criminal"
                .Split(',').Select(s => new SongDN { Name = s.Trim() }).ToMList(),
                Label = mjj
            }.Save();

            new AlbumDN
            {
                Name = "Dangerous",
                Year = 1991,
                Author = michael,
                Songs = "Black or White, Who Is It, Give it to Me"
                .Split(',').Select(s => new SongDN { Name = s.Trim() }).ToMList(),
                Label = mjj
            }.Save();

            new AlbumDN
            {
                Name = "HIStory",
                Year = 1995,
                Author = michael,
                Songs = "Billie Jean, Stranger In Moscow"
                .Split(',').Select(s => new SongDN { Name = s.Trim() }).ToMList(),
                BonusTrack = new SongDN{Name="Heal The World"},
                Label = mjj
            }.Save();

            new AlbumDN
            {
                Name = "Blood on the Dance Floor",
                Year = 1995,
                Author = michael,
                Songs = "Blood on the Dance Floor, Morphine"
                .Split(',').Select(s => new SongDN { Name = s.Trim() }).ToMList(),
                Label = mjj
            }.Save();

            var ga = new GrammyAwardDN { Category = "Foreing Band", Year = 2001, Result = AwardResult.Won };

            BandDN sigurRos = new BandDN
            {
                Name = "Sigur Ros",
                Members = "Jón Þór Birgisson, Georg Hólm, Orri Páll Dýrason"
                .Split(',').Select(s => new ArtistDN { Name = s.Trim() }).ToMList(),
                LastAward = ga,
            };

            LabelDN fatCat = new LabelDN { Name = "FatCat Records", Country = usa, Owner = universal.ToLite(), Node = universal.Node.FirstChild() }; 

            new AlbumDN
            {
                Name = "Ágaetis byrjun",
                Year = 1999,
                Author = sigurRos,
                Songs = "Scefn-g-englar"
                .Split(',').Select(s => new SongDN { Name = s.Trim() }).ToMList(),
                BonusTrack = new SongDN { Name = "Intro" },
                Label = fatCat,
            }.Save();

            LabelDN emi = new LabelDN { Name = "EMI", Country = usa, Node = sony.Node.NextSibling() }; 

            new AlbumDN
            {
                Name = "Takk...",
                Year = 2005,
                Author = sigurRos,
                Songs = "Hoppípolla, Glósóli, Saeglópur"
                .Split(',').Select(s => new SongDN { Name = s.Trim() }).ToMList(),
                BonusTrack = new SongDN { Name = "Svo hljótt" },
                Label = emi
            }.Save();


            new AwardNominationDN { Author = sigurRos.ToLite<IAuthorDN>(), Award = ga.ToLite<AwardDN>() }.Save();
            new AwardNominationDN { Author = michael.ToLite<IAuthorDN>(), Award = ga.ToLite<AwardDN>() }.Save();
            new AwardNominationDN { Author = smashingPumpkins.ToLite<IAuthorDN>(), Award = ga.ToLite<AwardDN>() }.Save();

            new AwardNominationDN { Author = sigurRos.ToLite<IAuthorDN>(), Award = ama.ToLite<AwardDN>() }.Save();
            new AwardNominationDN { Author = michael.ToLite<IAuthorDN>(), Award = ama.ToLite<AwardDN>() }.Save();
            new AwardNominationDN { Author = smashingPumpkins.ToLite<IAuthorDN>(), Award = ama.ToLite<AwardDN>() }.Save();

            new AwardNominationDN { Author = michael.ToLite<IAuthorDN>(), Award = pa.ToLite<AwardDN>() }.Save();
        }
    }
}
