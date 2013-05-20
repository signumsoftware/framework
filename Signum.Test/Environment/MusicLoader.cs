using Microsoft.SqlServer.Types;
using Signum.Engine;
using Signum.Engine.Operations;
using Signum.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;

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
            var ama = new AmericanMusicAwardDN { Category = "Indie Rock", Year = 1991, Result = AwardResult.Nominated }
                .Execute(AwardOperation.Save);

            BandDN smashingPumpkins = new BandDN
            {
                Name = "Smashing Pumpkins",
                Members = "Billy Corgan, James Iha, D'arcy Wretzky, Jimmy Chamberlin"
                .Split(',').Select(s => new ArtistDN { Name = s.Trim(), Sex = s.Contains("Wretzky") ? Sex.Female : Sex.Male, Status = s.Contains("Wretzky") ? Status.Married : (Status?)null }).ToMList(),
                LastAward = ama,
            }.Execute(BandOperation.Save);

            CountryDN usa = new CountryDN { Name = "USA" };
            CountryDN japan = new CountryDN { Name = Japan };

            smashingPumpkins.Members.ForEach(m => m.Friends = smashingPumpkins.Members.Where(a => a.Sex != m.Sex).Select(a => a.ToLiteFat()).ToMList());

            smashingPumpkins.Execute(BandOperation.Save);

            new NoteWithDateDN { CreationTime = DateTime.Now.AddHours(+8), Text = "American alternative rock band", Target = smashingPumpkins }
                .Execute(NoteWithDateOperation.Save);

            LabelDN virgin = new LabelDN { Name = "Virgin", Country = usa, Node = SqlHierarchyId.GetRoot().FirstChild() }
                .Execute(LabelOperation.Save);

            new AlbumDN
            {
                Name = "Siamese Dream",
                Year = 1993,
                Author = smashingPumpkins,
                Songs = { new SongDN { Name = "Disarm" } },
                Label = virgin
            }.Execute(AlbumOperation.Save);

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
            }.Execute(AlbumOperation.Save);
            
            new NoteWithDateDN { CreationTime = DateTime.Now.AddDays(-100).AddHours(-8), Text = "The blue one with the angel", Target = mellon }
                .Execute(NoteWithDateOperation.Save);

            LabelDN wea = new LabelDN { Name = "WEA International", Country = usa, Owner = virgin.ToLite(), Node = virgin.Node.FirstChild() }
                .Execute(LabelOperation.Save);
            
            new AlbumDN
            {
                Name = "Zeitgeist",
                Year = 2007,
                Author = smashingPumpkins,
                Songs = { new SongDN { Name = "Tarantula" } },
                BonusTrack = new SongDN { Name = "1976" },
                Label = wea,
            }.Execute(AlbumOperation.Save);

            new AlbumDN
            {
                Name = "American Gothic",
                Year = 2008,
                Author = smashingPumpkins,
                Songs = { new SongDN { Name = "The Rose March", Duration = TimeSpan.FromSeconds(276) } },
                Label = wea,
            }.Execute(AlbumOperation.Save);

            var pa = new PersonalAwardDN { Category = "Best Artist", Year = 1983, Result = AwardResult.Won }.Execute(AwardOperation.Save);

            ArtistDN michael = new ArtistDN
            {
                Name = "Michael Jackson",
                Dead = true,
                LastAward = pa,
                Status = Status.Single,
            }.Execute(ArtistOperation.Save); ;

            new NoteWithDateDN { CreationTime = new DateTime(2009, 6, 25, 0, 0, 0), Text = "Death on June, 25th", Target = michael }
                .Execute(NoteWithDateOperation.Save);

            new NoteWithDateDN { CreationTime = new DateTime(2000, 1, 1, 0, 0, 0), Text = null, Target = michael }
                .SetMixin((CorruptMixin c) => c.Corrupt, true)
                .Do(n => n.Mixin<ColaboratorsMixin>().Colaborators.Add(michael))
                .Execute(NoteWithDateOperation.Save);

            LabelDN universal = new LabelDN { Name = "UMG Recordings", Country = usa, Node = virgin.Node.NextSibling() }
                .Execute(LabelOperation.Save);

            new AlbumDN
            {
                Name = "Ben",
                Year = 1972,
                Author = michael,
                Songs = { new SongDN { Name = "Ben" } },
                BonusTrack = new SongDN { Name = "Michael" },
                Label = universal,
            }.Execute(AlbumOperation.Save);

            LabelDN sony = new LabelDN { Name = "Sony", Country = japan, Node = universal.Node.NextSibling() }
                .Execute(LabelOperation.Save);

            new AlbumDN
            {
                Name = "Thriller",
                Year = 1982,
                Author = michael,
                Songs = "Wanna Be Startin' Somethin', Thriller, Beat It"
                .Split(',').Select(s => new SongDN { Name = s.Trim() }).ToMList(),
                BonusTrack = new SongDN { Name = "Billie Jean" },
                Label = sony
            }.Execute(AlbumOperation.Save);

            LabelDN mjj = new LabelDN { Name = "MJJ", Country = usa, Owner = sony.ToLite(), Node = sony.Node.FirstChild() }
                .Execute(LabelOperation.Save);

            new AlbumDN
            {
                Name = "Bad",
                Year = 1989,
                Author = michael,
                Songs = "Bad, Man in the Mirror, Dirty Diana, Smooth Criminal"
                .Split(',').Select(s => new SongDN { Name = s.Trim() }).ToMList(),
                Label = mjj
            }.Execute(AlbumOperation.Save);

            new AlbumDN
            {
                Name = "Dangerous",
                Year = 1991,
                Author = michael,
                Songs = "Black or White, Who Is It, Give it to Me"
                .Split(',').Select(s => new SongDN { Name = s.Trim() }).ToMList(),
                Label = mjj
            }.Execute(AlbumOperation.Save);

            new AlbumDN
            {
                Name = "HIStory",
                Year = 1995,
                Author = michael,
                Songs = "Billie Jean, Stranger In Moscow"
                .Split(',').Select(s => new SongDN { Name = s.Trim() }).ToMList(),
                BonusTrack = new SongDN { Name = "Heal The World" },
                Label = mjj
            }.Execute(AlbumOperation.Save);

            new AlbumDN
            {
                Name = "Blood on the Dance Floor",
                Year = 1995,
                Author = michael,
                Songs = "Blood on the Dance Floor, Morphine"
                .Split(',').Select(s => new SongDN { Name = s.Trim() }).ToMList(),
                Label = mjj
            }.Execute(AlbumOperation.Save); ;

            var ga = new GrammyAwardDN { Category = "Foreing Band", Year = 2001, Result = AwardResult.Won }
                .Execute(AwardOperation.Save);

            BandDN sigurRos = new BandDN
            {
                Name = "Sigur Ros",
                Members = "Jón Þór Birgisson, Georg Hólm, Orri Páll Dýrason"
                .Split(',').Select(s => new ArtistDN { Name = s.Trim() }.Execute(ArtistOperation.Save)).ToMList(),
                LastAward = ga,
            }.Execute(BandOperation.Save);

            LabelDN fatCat = new LabelDN { Name = "FatCat Records", Country = usa, Owner = universal.ToLite(), Node = universal.Node.FirstChild() }
                .Execute(LabelOperation.Save);

            new AlbumDN
            {
                Name = "Ágaetis byrjun",
                Year = 1999,
                Author = sigurRos,
                Songs = "Scefn-g-englar"
                .Split(',').Select(s => new SongDN { Name = s.Trim() }).ToMList(),
                BonusTrack = new SongDN { Name = "Intro" },
                Label = fatCat,
            }.Execute(AlbumOperation.Save);

            LabelDN emi = new LabelDN { Name = "EMI", Country = usa, Node = sony.Node.NextSibling() }.Execute(LabelOperation.Save);

            new AlbumDN
            {
                Name = "Takk...",
                Year = 2005,
                Author = sigurRos,
                Songs = "Hoppípolla, Glósóli, Saeglópur"
                .Split(',').Select(s => new SongDN { Name = s.Trim() }).ToMList(),
                BonusTrack = new SongDN { Name = "Svo hljótt" },
                Label = emi
            }.Execute(AlbumOperation.Save);

            new AwardNominationDN { Author = sigurRos.ToLite(), Award = ga.ToLite() }.Save();
            new AwardNominationDN { Author = michael.ToLite(), Award = ga.ToLite() }.Save();
            new AwardNominationDN { Author = smashingPumpkins.ToLite(), Award = ga.ToLite() }.Save();

            new AwardNominationDN { Author = sigurRos.ToLite(), Award = ama.ToLite() }.Save();
            new AwardNominationDN { Author = michael.ToLite(), Award = ama.ToLite() }.Save();
            new AwardNominationDN { Author = smashingPumpkins.ToLite(), Award = ama.ToLite() }.Save();

            new AwardNominationDN { Author = michael.ToLite(), Award = pa.ToLite() }.Save();
        }
    }
}
