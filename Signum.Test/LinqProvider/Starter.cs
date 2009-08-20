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

namespace Signum.Test.LinqProvider
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

                Administrator.Initialize();

                Load();

                started = true;
            }
        }

        public static void Start(string connectionString)
        {
            SchemaBuilder sb = new SchemaBuilder();
            TypeLogic.Start(sb, false); 
            sb.Include<AlbumDN>();
            sb.Include<NoteDN>();
            ConnectionScope.Default = new Connection(connectionString, sb.Schema); 
        }

        public static void Load()
        {
            BandDN smashingPumpkins = new BandDN
            {
                Name = "Smashing Pumpkins",
                Members = "Billy Corgan, James Iha, D'arcy Wretzky, Jimmy Chamberlin"
                .Split(',').Select(s => new ArtistDN { Name = s.Trim(), Sex = s.Contains("Wretzky")? Sex.Female: Sex.Male }).ToMList()
            };

            new NoteDN { CreationTime = DateTime.Now.AddDays(-30), Text = "American alternative rock band", Target = smashingPumpkins }.Save(); 

            new AlbumDN
            {
                Name = "Siamese Dream",
                Year = 1993,
                Author = smashingPumpkins,
                Song = new MList<SongDN> { new SongDN { Name = "Disarm" } }
            }.Save();

            AlbumDN mellon = new AlbumDN
            {
                Name = "Mellon Collie and the Infinite Sadness",
                Year = 1995,
                Author = smashingPumpkins,
                Song = new MList<SongDN> 
                { 
                    new SongDN { Name = "Zero" }, 
                    new SongDN { Name = "1976" }, 
                    new SongDN { Name = "Tonight, Tonight" } 
                }
            };

            mellon.Save();

            new NoteDN { CreationTime = DateTime.Now.AddDays(-100), Text = "The blue one with the angel", Target = mellon }.Save(); 

            new AlbumDN
            {
                Name = "Zeitgeist",
                Year = 2007,
                Author = smashingPumpkins,
                Song = new MList<SongDN> { new SongDN { Name = "Tarantula" } }
            }.Save();

            new AlbumDN
            {
                Name = "American Gothic", 
                Year = 2008,
                Author = smashingPumpkins,
                Song = new MList<SongDN> { new SongDN { Name = "The Rose March" } }
            }.Save();

            ArtistDN michael = new ArtistDN { Name = "Michael Jackson", Dead = true };

            new NoteDN { CreationTime = new DateTime(2009, 6, 25, 0, 0, 0), Text = "Death on June, 25th", Target = michael }.Save(); 


            new AlbumDN
            {
                Name = "Ben",
                Year = 1972,
                Author = michael,
                Song = new MList<SongDN> { new SongDN { Name = "Ben" } }
            }.Save();

            new AlbumDN
            {
                Name = "Thriller",
                Year = 1982,
                Author = michael,
                Song = "Wanna Be Startin' Somethin', Thriller, Beat It"
                .Split(',').Select(s => new SongDN { Name = s.Trim() }).ToMList()
            }.Save();

            new AlbumDN
            {
                Name = "Bad",
                Year = 1989,
                Author = michael,
                Song = "Bad, Man in the Mirror, Dirty Diana, Smooth Criminal"
                .Split(',').Select(s => new SongDN { Name = s.Trim() }).ToMList()
            }.Save();

            new AlbumDN
            {
                Name = "Dangerous",
                Year = 1991,
                Author = michael,
                Song = "Black or White, Who Is It, Give it to Me"
                .Split(',').Select(s => new SongDN { Name = s.Trim() }).ToMList()
            }.Save();

            new AlbumDN
            {
                Name = "HIStory",
                Year = 1995,
                Author = michael,
                Song = "Heal The World, Stranger In Moscow"
                .Split(',').Select(s => new SongDN { Name = s.Trim() }).ToMList()
            }.Save();

            new AlbumDN
            {
                Name = "Blood on the Dance Floor",
                Year = 1995,
                Author = michael,
                Song = "Blood on the Dance Floor, Morphine"
                .Split(',').Select(s => new SongDN { Name = s.Trim() }).ToMList()
            }.Save();


            BandDN sigurRos = new BandDN
            {
                Name = "Sigur Ros",
                Members = "Jón Þór Birgisson, Georg Hólm, Orri Páll Dýrason"
                .Split(',').Select(s => new ArtistDN { Name = s.Trim() }).ToMList()
            };

            new AlbumDN
            {
                Name = "Ágaetis byrjun",
                Year = 1999,
                Author = sigurRos,
                Song = "Scefn-g-englar"
                .Split(',').Select(s => new SongDN { Name = s.Trim() }).ToMList()
            }.Save();

            new AlbumDN
            {
                Name = "Takk...",
                Year = 2005,
                Author = sigurRos,
                Song = "Hoppípolla, Glósóli, Saeglópur"
                .Split(',').Select(s => new SongDN { Name = s.Trim() }).ToMList()
            }.Save();
        }

    }

    public class DebugTextWriter : TextWriter
    {
        public override void Write(char[] buffer, int index, int count)
        {
            Debug.Write(new String(buffer, index, count));
        }

        public override void Write(string value)
        {
            Debug.Write(value);
        }

        public override Encoding Encoding
        {
            get { return Encoding.Default; }
        }
    }
}
