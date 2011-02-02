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

namespace Signum.Test
{
    public static class Starter
    {
        static bool started = false; 
        public static void StartAndLoad()
        {
            if(!started)
            {
                Start(Settings.Default.SignumTest); 

                Administrator.TotalGeneration();

                Schema.Current.Initialize();

                Load();

                started = true;
            }
        }

        internal static void Dirty()
        {
            started = false;
        }

        public static void Start(string connectionString)
        {
            SchemaBuilder sb = new SchemaBuilder();
            DynamicQueryManager dqm = new DynamicQueryManager();
            ConnectionScope.Default = new Connection(connectionString, sb.Schema, dqm);

            StartMusic(sb, dqm);

        }

        public static void StartMusic(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            sb.Include<AlbumDN>();
            sb.Include<NoteDN>();
            sb.Include<AlertDN>();
            sb.Include<PersonalAwardDN>();

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
                                         Columna1 = "Microsoft is expanding its roster of SDL (security development lifecycle) tools and services this week with the beta release of an attack surface analyzer tool as well as the introduction of consulting services on secure development.",
                                         Columna2 = "Microsoft's Attack Surface Analyzer is an SDL verification tool for developers and IT professionals to identify whether newly developed or installed applications inadvertently change the attack surface of a Microsoft OS. The free tool is downloadable from Microsoft's website and is the same tool used by internal Microsoft product development teams.",
                                         Columna3 = "Microsoft also is updating its existing Threat Modeling and BinScope Binary Analyzer tools to enhance developer usability. These tools also are free and are accessible at Microsoft's security website. The threat modeling tool offers guidance on building and analyzing threat models, while the binary analyzer checks binaries to ensure they were built based on SDL requirements and recommendations.Consistent with the previous release of the tool, version 3.1.6 [of Threat Modeling] allows for early and structured analysis and proactive mitigation of potential security and privacy issues in new and existing applications, sad Ladd in the blog post. The Microsoft SDL Threat Modeling Tool beta is enhanced to support Microsoft Visio 2010 for diagram design and also contains bug fixes reported to Microsoft by members of the security developer community. Version 3.1.6 is currently in a beta release stage."
                                     }).ToDynamic();

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
        }

        public const string Japan = "Japan";
        
        public static void Load()
        {
            BandDN smashingPumpkins = new BandDN
            {
                Name = "Smashing Pumpkins",
                Members = "Billy Corgan, James Iha, D'arcy Wretzky, Jimmy Chamberlin"
                .Split(',').Select(s => new ArtistDN { Name = s.Trim(), Sex = s.Contains("Wretzky") ? Sex.Female : Sex.Male, Status = s.Contains("Wretzky") ? Status.Married: (Status?)null }).ToMList(),
                LastAward = new AmericanMusicAwardDN { Category = "Indie Rock", Year = 1991, Result = AwardResult.Nominated }
            };

            CountryDN usa = new CountryDN { Name = "USA" };
            CountryDN japan = new CountryDN { Name = Japan };

            smashingPumpkins.Members.ForEach(m => m.Friends = smashingPumpkins.Members.Where(a => a.Sex != m.Sex).Select(a => a.ToLiteFat()).ToMList());

            new NoteDN { CreationTime = DateTime.Now.AddHours(+8), Text = "American alternative rock band", Target = smashingPumpkins }.Save();

            LabelDN virgin = new LabelDN { Name = "Virgin", Country = usa };

            new AlbumDN
            {
                Name = "Siamese Dream",
                Year = 1993,
                Author = smashingPumpkins,
                Songs = new MList<SongDN> { new SongDN { Name = "Disarm" } },
                Label = virgin
            }.Save();

            AlbumDN mellon = new AlbumDN
            {
                Name = "Mellon Collie and the Infinite Sadness",
                Year = 1995,
                Author = smashingPumpkins,
                Songs = new MList<SongDN> 
                { 
                    new SongDN { Name = "Zero", Duration = 123 }, 
                    new SongDN { Name = "1976" }, 
                    new SongDN { Name = "Tonight, Tonight", Duration = 376 } 
                },
                BonusTrack = new SongDN { Name = "Jellybelly" },
                Label = virgin
            };

            mellon.Save();

            new NoteDN { CreationTime = DateTime.Now.AddDays(-100).AddHours(-8), Text = "The blue one with the angel", Target = mellon }.Save();

            LabelDN wea = new LabelDN { Name = "WEA International", Country = usa, Owner = virgin.ToLite() };

            new AlbumDN
            {
                Name = "Zeitgeist",
                Year = 2007,
                Author = smashingPumpkins,
                Songs = new MList<SongDN> { new SongDN { Name = "Tarantula" } },
                BonusTrack = new SongDN{Name = "1976"},
                Label = wea,
            }.Save();

            new AlbumDN
            {
                Name = "American Gothic", 
                Year = 2008,
                Author = smashingPumpkins,
                Songs = new MList<SongDN> { new SongDN { Name = "The Rose March", Duration = 276 } },
                Label = wea,
            }.Save();

            ArtistDN michael = new ArtistDN
            {
                Name = "Michael Jackson",
                Dead = true,
                LastAward = new PersonalAwardDN { Category = "Best Artist", Year = 1983, Result = AwardResult.Won },
                Status = Status.Single,
            };

            new NoteDN { CreationTime = new DateTime(2009, 6, 25, 0, 0, 0), Text = "Death on June, 25th", Target = michael }.Save();

            LabelDN universal = new LabelDN { Name = "UMG Recordings", Country = usa };

            new AlbumDN
            {
                Name = "Ben",
                Year = 1972,
                Author = michael,
                Songs = new MList<SongDN> { new SongDN { Name = "Ben" } },
                BonusTrack = new SongDN{Name = "Michael"},
                Label = universal,
            }.Save();

            LabelDN sony = new LabelDN { Name = "Sony", Country = japan };

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

            LabelDN mjj = new LabelDN { Name = "MJJ", Country = usa, Owner = sony.ToLite() };

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


            BandDN sigurRos = new BandDN
            {
                Name = "Sigur Ros",
                Members = "Jón Þór Birgisson, Georg Hólm, Orri Páll Dýrason"
                .Split(',').Select(s => new ArtistDN { Name = s.Trim() }).ToMList(),
                LastAward = new GrammyAwardDN { Category = "Foreing Band", Year = 2001, Result = AwardResult.Won }
            };

            LabelDN fatCat = new LabelDN { Name = "FatCat Records", Country = usa, Owner = universal.ToLite() }; 

            new AlbumDN
            {
                Name = "Ágaetis byrjun",
                Year = 1999,
                Author = sigurRos,
                Songs = "Scefn-g-englar"
                .Split(',').Select(s => new SongDN { Name = s.Trim() }).ToMList(),
                BonusTrack = new SongDN { Name = "Intro" },
                Label = fatCat
            }.Save();

            LabelDN emi = new LabelDN { Name = "EMI", Country = usa }; 

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
        }
    }
}
