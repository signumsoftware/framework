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

            InternalStart(sb, dqm);

            ConnectionScope.Default = new Connection(connectionString, sb.Schema, dqm);
        }

        public static void InternalStart(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            TypeLogic.Start(sb);
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

            dqm[typeof(AmericanMusicAwardDN)] = (from a in Database.Query<AmericanMusicAwardDN>()
                                                 select new
                                                 {
                                                     Entity = a.ToLite(),
                                                     a.Id,
                                                     a.Year,
                                                     a.Category,
                                                     a.Result,
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

            dqm[typeof(AwardDN)] = (from a in Database.Query<AwardDN>()
                                    select new
                                    {
                                        Entity = a.ToLite(),
                                        a.Id,
                                        a.Year,
                                        a.Category,
                                        a.Result
                                    }).ToDynamic();

            dqm[typeof(BandDN)] = (from a in Database.Query<BandDN>()
                                   select new
                                   {
                                       Entity = a.ToLite(),
                                       a.Id,
                                       a.Name,
                                       LastAward = a.LastAward.ToLite(),
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

            dqm[typeof(LabelDN)] = (from a in Database.Query<LabelDN>()
                                    select new
                                    {
                                        Entity = a.ToLite(),
                                        a.Id,
                                        a.Name,
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
        
        public static void Load()
        {
            BandDN smashingPumpkins = new BandDN
            {
                Name = "Smashing Pumpkins",
                Members = "Billy Corgan, James Iha, D'arcy Wretzky, Jimmy Chamberlin"
                .Split(',').Select(s => new ArtistDN { Name = s.Trim(), Sex = s.Contains("Wretzky") ? Sex.Female : Sex.Male }).ToMList(),
                LastAward = new AmericanMusicAwardDN { Category = "Indie Rock", Year = 1991, Result = AwardResult.Nominated }
            };

            CountryDN usa = new CountryDN { Name = "USA" };
            CountryDN japan = new CountryDN { Name = "Japan" };

            smashingPumpkins.Members.ForEach(m => m.Friends = smashingPumpkins.Members.Where(a => a.Sex != m.Sex).Select(a => a.ToLiteFat()).ToMList());

            new NoteDN { CreationTime = DateTime.Now.AddDays(-30), Text = "American alternative rock band", Target = smashingPumpkins }.Save();

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
                    new SongDN { Name = "Zero" }, 
                    new SongDN { Name = "1976" }, 
                    new SongDN { Name = "Tonight, Tonight" } 
                },
                Label = virgin
            };

            mellon.Save();

            new NoteDN { CreationTime = DateTime.Now.AddDays(-100), Text = "The blue one with the angel", Target = mellon }.Save();

            LabelDN wea = new LabelDN { Name = "WEA International", Country = usa };

            new AlbumDN
            {
                Name = "Zeitgeist",
                Year = 2007,
                Author = smashingPumpkins,
                Songs = new MList<SongDN> { new SongDN { Name = "Tarantula" } },
                Label = wea,
            }.Save();

            new AlbumDN
            {
                Name = "American Gothic", 
                Year = 2008,
                Author = smashingPumpkins,
                Songs = new MList<SongDN> { new SongDN { Name = "The Rose March" } },
                Label = wea,
            }.Save();

            ArtistDN michael = new ArtistDN
            {
                Name = "Michael Jackson",
                Dead = true,
                LastAward = new PersonalAwardDN { Category = "Best Artist", Year = 1983, Result = AwardResult.Won }
            };

            new NoteDN { CreationTime = new DateTime(2009, 6, 25, 0, 0, 0), Text = "Death on June, 25th", Target = michael }.Save();

            LabelDN universal = new LabelDN { Name = "UMG Recordings", Country = usa };

            new AlbumDN
            {
                Name = "Ben",
                Year = 1972,
                Author = michael,
                Songs = new MList<SongDN> { new SongDN { Name = "Ben" } },
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
                Label = sony
            }.Save();

            LabelDN mjj = new LabelDN { Name = "MJJ", Country = usa };

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
                Songs = "Heal The World, Stranger In Moscow"
                .Split(',').Select(s => new SongDN { Name = s.Trim() }).ToMList(),
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

            LabelDN fatCat = new LabelDN { Name = "FatCat Records", Country = usa }; 

            new AlbumDN
            {
                Name = "Ágaetis byrjun",
                Year = 1999,
                Author = sigurRos,
                Songs = "Scefn-g-englar"
                .Split(',').Select(s => new SongDN { Name = s.Trim() }).ToMList(),
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
                Label = emi
            }.Save();
        }
    }
}
