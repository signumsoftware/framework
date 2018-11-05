using Microsoft.SqlServer.Types;
using Signum.Engine;
using Signum.Engine.Operations;
using Signum.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Threading;

namespace Signum.Test.Environment
{
    public static class MusicLoader
    {
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
            var ama = new AmericanMusicAwardEntity { Category = "Indie Rock", Year = 1991, Result = AwardResult.Nominated }
                .Execute(AwardOperation.Save);

            BandEntity smashingPumpkins = new BandEntity
            {
                Name = "Smashing Pumpkins",
                Members = "Billy Corgan, James Iha, D'arcy Wretzky, Jimmy Chamberlin"
                .Split(',').Select(s => new ArtistEntity { Name = s.Trim(), Sex = s.Contains("Wretzky") ? Sex.Female : Sex.Male, Status = s.Contains("Wretzky") ? Status.Married : (Status?)null }).ToMList(),
                LastAward = ama,
            }.Execute(BandOperation.Save);

            CountryEntity usa = new CountryEntity { Name = "USA" };
            CountryEntity japan = new CountryEntity { Name = Japan };

            smashingPumpkins.Members.ForEach(m => m.Friends = smashingPumpkins.Members.Where(a => a.Sex != m.Sex).Select(a => a.ToLiteFat()).ToMList());

            smashingPumpkins.Execute(BandOperation.Save);

            new NoteWithDateEntity { CreationTime = DateTime.Now.AddHours(+8), Text = "American alternative rock band", Target = smashingPumpkins }
                .Execute(NoteWithDateOperation.Save);

            LabelEntity virgin = new LabelEntity { Name = "Virgin", Country = usa, Node = SqlHierarchyId.GetRoot().FirstChild() }
                .Execute(LabelOperation.Save);

            new AlbumEntity
            {
                Name = "Siamese Dream",
                Year = 1993,
                Author = smashingPumpkins,
                Songs = { new SongEmbedded { Name = "Disarm" } },
                Label = virgin
            }.Execute(AlbumOperation.Save);

            AlbumEntity mellon = new AlbumEntity
            {
                Name = "Mellon Collie and the Infinite Sadness",
                Year = 1995,
                Author = smashingPumpkins,
                Songs = 
                { 
                    new SongEmbedded { Name = "Zero", Duration = TimeSpan.FromSeconds(123) }, 
                    new SongEmbedded { Name = "1976" }, 
                    new SongEmbedded { Name = "Tonight, Tonight", Duration = TimeSpan.FromSeconds(376) } 
                },
                BonusTrack = new SongEmbedded { Name = "Jellybelly" },
                Label = virgin
            }.Execute(AlbumOperation.Save);
            
            new NoteWithDateEntity { CreationTime = DateTime.Now.AddDays(-100).AddHours(-8), Text = "The blue one with the angel", Target = mellon }
                .Execute(NoteWithDateOperation.Save);

            LabelEntity wea = new LabelEntity { Name = "WEA International", Country = usa, Owner = virgin.ToLite(), Node = virgin.Node.FirstChild() }
                .Execute(LabelOperation.Save);
            
            new AlbumEntity
            {
                Name = "Zeitgeist",
                Year = 2007,
                Author = smashingPumpkins,
                Songs = { new SongEmbedded { Name = "Tarantula" } },
                BonusTrack = new SongEmbedded { Name = "1976" },
                Label = wea,
            }.Execute(AlbumOperation.Save);

            new AlbumEntity
            {
                Name = "American Gothic",
                Year = 2008,
                Author = smashingPumpkins,
                Songs = { new SongEmbedded { Name = "The Rose March", Duration = TimeSpan.FromSeconds(276) } },
                Label = wea,
            }.Execute(AlbumOperation.Save);

            var pa = new PersonalAwardEntity { Category = "Best Artist", Year = 1983, Result = AwardResult.Won }.Execute(AwardOperation.Save);

            ArtistEntity michael = new ArtistEntity
            {
                Name = "Michael Jackson",
                Dead = true,
                LastAward = pa,
                Status = Status.Single,
                Friends = { smashingPumpkins.Members.SingleEx(a=>a.Name.Contains("Billy Corgan")).ToLite() }
            }.Execute(ArtistOperation.Save); ;

            new NoteWithDateEntity { CreationTime = new DateTime(2009, 6, 25, 0, 0, 0), Text = "Death on June, 25th", Target = michael }
                .Execute(NoteWithDateOperation.Save);

            new NoteWithDateEntity { CreationTime = new DateTime(2000, 1, 1, 0, 0, 0), Text = null, Target = michael }
                .SetMixin((CorruptMixin c) => c.Corrupt, true)
                .Do(n => n.Mixin<ColaboratorsMixin>().Colaborators.Add(michael))
                .Execute(NoteWithDateOperation.Save);

            LabelEntity universal = new LabelEntity { Name = "UMG Recordings", Country = usa, Node = virgin.Node.NextSibling() }
                .Execute(LabelOperation.Save);

            new AlbumEntity
            {
                Name = "Ben",
                Year = 1972,
                Author = michael,
                Songs = { new SongEmbedded { Name = "Ben" } },
                BonusTrack = new SongEmbedded { Name = "Michael" },
                Label = universal,
            }.Execute(AlbumOperation.Save);

            LabelEntity sony = new LabelEntity { Name = "Sony", Country = japan, Node = universal.Node.NextSibling() }
                .Execute(LabelOperation.Save);

            new AlbumEntity
            {
                Name = "Thriller",
                Year = 1982,
                Author = michael,
                Songs = "Wanna Be Startin' Somethin', Thriller, Beat It"
                .Split(',').Select(s => new SongEmbedded { Name = s.Trim() }).ToMList(),
                BonusTrack = new SongEmbedded { Name = "Billie Jean" },
                Label = sony
            }.Execute(AlbumOperation.Save);

            LabelEntity mjj = new LabelEntity { Name = "MJJ", Country = usa, Owner = sony.ToLite(), Node = sony.Node.FirstChild() }
                .Execute(LabelOperation.Save);

            new AlbumEntity
            {
                Name = "Bad",
                Year = 1989,
                Author = michael,
                Songs = "Bad, Man in the Mirror, Dirty Diana, Smooth Criminal"
                .Split(',').Select(s => new SongEmbedded { Name = s.Trim() }).ToMList(),
                Label = mjj
            }.Execute(AlbumOperation.Save);

            new AlbumEntity
            {
                Name = "Dangerous",
                Year = 1991,
                Author = michael,
                Songs = "Black or White, Who Is It, Give it to Me"
                .Split(',').Select(s => new SongEmbedded { Name = s.Trim() }).ToMList(),
                Label = mjj
            }.Execute(AlbumOperation.Save);

            new AlbumEntity
            {
                Name = "HIStory",
                Year = 1995,
                Author = michael,
                Songs = "Billie Jean, Stranger In Moscow"
                .Split(',').Select(s => new SongEmbedded { Name = s.Trim() }).ToMList(),
                BonusTrack = new SongEmbedded { Name = "Heal The World" },
                Label = mjj
            }.Execute(AlbumOperation.Save);

            new AlbumEntity
            {
                Name = "Blood on the Dance Floor",
                Year = 1995,
                Author = michael,
                Songs = "Blood on the Dance Floor, Morphine"
                .Split(',').Select(s => new SongEmbedded { Name = s.Trim() }).ToMList(),
                Label = mjj
            }.Execute(AlbumOperation.Save); ;

            var ga = (GrammyAwardEntity)new GrammyAwardEntity { Category = "Foreing Band", Year = 2001, Result = AwardResult.Won }
                .Execute(AwardOperation.Save);

            BandEntity sigurRos = new BandEntity
            {
                Name = "Sigur Ros",
                Members = "Jón Þór Birgisson, Georg Hólm, Orri Páll Dýrason"
                .Split(',').Select(s => new ArtistEntity { Name = s.Trim() }.Execute(ArtistOperation.Save)).ToMList(),
                LastAward = ga,
            }.Execute(BandOperation.Save);

            LabelEntity fatCat = new LabelEntity { Name = "FatCat Records", Country = usa, Owner = universal.ToLite(), Node = universal.Node.FirstChild() }
                .Execute(LabelOperation.Save);

            new AlbumEntity
            {
                Name = "Ágaetis byrjun",
                Year = 1999,
                Author = sigurRos,
                Songs = "Scefn-g-englar"
                .Split(',').Select(s => new SongEmbedded { Name = s.Trim() }).ToMList(),
                BonusTrack = new SongEmbedded { Name = "Intro" },
                Label = fatCat,
            }.Execute(AlbumOperation.Save);

            LabelEntity emi = new LabelEntity { Name = "EMI", Country = usa, Node = sony.Node.NextSibling() }.Execute(LabelOperation.Save);

            new AlbumEntity
            {
                Name = "Takk...",
                Year = 2005,
                Author = sigurRos,
                Songs = "Hoppípolla, Glósóli, Saeglópur"
                .Split(',').Select(s => new SongEmbedded { Name = s.Trim() }).ToMList(),
                BonusTrack = new SongEmbedded { Name = "Svo hljótt" },
                Label = emi
            }.Execute(AlbumOperation.Save);

            new AwardNominationEntity { Author = sigurRos.ToLite(), Award = ga.ToLite() }.Save();
            new AwardNominationEntity { Author = michael.ToLite(), Award = ga.ToLite() }.Save();
            new AwardNominationEntity { Author = smashingPumpkins.ToLite(), Award = ga.ToLite() }.Save();

            new AwardNominationEntity { Author = sigurRos.ToLite(), Award = ama.ToLite() }.Save();
            new AwardNominationEntity { Author = michael.ToLite(), Award = ama.ToLite() }.Save();
            new AwardNominationEntity { Author = smashingPumpkins.ToLite(), Award = ama.ToLite() }.Save();

            new AwardNominationEntity { Author = michael.ToLite(), Award = pa.ToLite() }.Save();
            new AwardNominationEntity { Author = michael.ToLite(), Award = null}.Save();

            new ConfigEntity
            {
                EmbeddedConfig = new EmbeddedConfigEmbedded
                {
                    Awards = { ga.ToLite() }
                }
            }.Execute(ConfigOperation.Save);


            CreateFolders();
        }


        //       |--X1(a)-|-X1(B)-|-X2--|
        //  |---A1-----|--A2-----------------|
        //    |---B1---------|---B2-------|

        private static void CreateFolders()
        {
            var TIME = 100;

            var a = new FolderEntity { Name = "A1" }.Save();
            Thread.Sleep(TIME);

            var b = new FolderEntity { Name = "B1" }.Save();
            Thread.Sleep(TIME);

            var x = new FolderEntity { Name = "X1", Parent = a.ToLite() }.Save();
            Thread.Sleep(TIME);

            a.Name = "A2";
            a.Save();
            Thread.Sleep(TIME);

            x.Parent = b.ToLite();
            x.Save();
            Thread.Sleep(TIME);

            x.Name = "X2";
            x.Save();
            Thread.Sleep(TIME);

            b.Name = "B2";
            b.Save();
            Thread.Sleep(TIME);

            x.Delete();
            Thread.Sleep(TIME);

            b.Delete();
            Thread.Sleep(TIME);

            a.Delete();
            Thread.Sleep(TIME);
        }
    }
}
