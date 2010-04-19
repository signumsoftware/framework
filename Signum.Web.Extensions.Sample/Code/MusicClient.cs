using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Signum.Web;
using Signum.Utilities;
using System.Reflection;
using Signum.Test;
using Signum.Web.Operations;
using Signum.Test.Extensions;

namespace Signum.Web.Extensions.Sample
{
    public static class MusicClient
    {
        public static string ViewPrefix = "Views/Music/";

        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<AlbumDN>(EntityType.Default) { PartialViewName = e => ViewPrefix + "Album" },
                    new EntitySettings<AmericanMusicAwardDN>(EntityType.Default) { PartialViewName = e => ViewPrefix + "AmericanMusicAward" },
                    new EntitySettings<ArtistDN>(EntityType.Default) { PartialViewName = e => ViewPrefix + "Artist" },
                    new EntitySettings<AwardDN>(EntityType.Default) { PartialViewName = e => ViewPrefix + "Award" },
                    new EntitySettings<BandDN>(EntityType.Default) { PartialViewName = e => ViewPrefix + "Band" },
                    new EntitySettings<GrammyAwardDN>(EntityType.Default) { PartialViewName = e => ViewPrefix + "GrammyAward" },
                    new EntitySettings<LabelDN>(EntityType.Default) { PartialViewName = e => ViewPrefix + "Label" },
                    new EntitySettings<PersonalAwardDN>(EntityType.Default) { PartialViewName = e => ViewPrefix + "PersonalAward" },
                    new EntitySettings<SongDN>(EntityType.Default) { PartialViewName = e => ViewPrefix + "Song" },

                    new EntitySettings<AlbumFromBandModel>(EntityType.Default){PartialViewName = e => ViewPrefix + "AlbumFromBandModel"},
                });

                Navigator.RegisterTypeName<IAuthorDN>();

                OperationClient.Manager.Settings.AddRange(new Dictionary<Enum, OperationButton>
                {
                    { AlbumOperation.Save, new OperationButton { Settings = new EntityOperationSettings { IsVisible = _ => false }}},
                    { AlbumOperation.Clone, new OperationButton { Settings = new EntityOperationSettings { Post = true }}},
                    { AlbumOperation.CreateFromBand, new OperationButton { Settings = new EntityOperationSettings { Options = { ControllerUrl = "Music/CreateAlbumFromBand", AvoidDefaultOk = true }}}},
                });
            }
        }
    }
}
