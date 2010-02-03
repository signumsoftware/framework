using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Signum.Web;
using Signum.Utilities;
using System.Reflection;
using Signum.Test;

namespace Signum.Web.Sample
{
    public static class MusicClient
    {
        public static string ViewPrefix = "Views/Music/";

        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.Manager.EntitySettings.AddRange(new Dictionary<Type, EntitySettings>
                {
                    { typeof(AlbumDN), new EntitySettings(false) { PartialViewName = ViewPrefix + "Album" }},
                    { typeof(AmericanMusicAwardDN), new EntitySettings(false) { PartialViewName = ViewPrefix + "AmericanMusicAward" }},
                    { typeof(IAuthorDN), new EntitySettings(false) },
                    { typeof(ArtistDN), new EntitySettings(false) { PartialViewName = ViewPrefix + "Artist" }},
                    { typeof(AwardDN), new EntitySettings(false) { PartialViewName = ViewPrefix + "Award" }},
                    { typeof(BandDN), new EntitySettings(false) { PartialViewName = ViewPrefix + "Band" }},
                    { typeof(GrammyAwardDN), new EntitySettings(false) { PartialViewName = ViewPrefix + "GrammyAward" }},
                    { typeof(LabelDN), new EntitySettings(false) { PartialViewName = ViewPrefix + "Label" }},
                    { typeof(PersonalAwardDN), new EntitySettings(false) { PartialViewName = ViewPrefix + "PersonalAward" }},
                    { typeof(SongDN), new EntitySettings(false) { PartialViewName = ViewPrefix + "Song" }},
                });
            }
        }
    }
}
