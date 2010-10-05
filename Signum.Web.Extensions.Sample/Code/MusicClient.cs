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
                    new EntitySettings<AlbumDN>(EntityType.NotSaving) { PartialViewName = e => ViewPrefix + "Album" },
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

                OperationClient.Manager.Settings.AddRange(new Dictionary<Enum, OperationSettings>
                {
                    { AlbumOperation.Clone, new EntityOperationSettings 
                    { 
                        OnClick = ctx => new JsOperationConstructorFrom(ctx.Options()).DefaultSubmit(),
                        OnContextualClick = ctx => Js.Confirm("Do you wish to clone album {0}".Formato(ctx.Entity.ToStr),
                            new JsOperationConstructorFrom(ctx.Options()).OperationAjax(ctx.Prefix, JsOpSuccess.DefaultContextualDispatcher)),
                        IsVisible = ctx => true,
                    }},
                    { AlbumOperation.CreateFromBand, new EntityOperationSettings 
                    { 
                        ControllerUrl = "Music/CreateAlbumFromBand", 
                        OnClick = ctx => JsValidator.EntityIsValid(ctx.Prefix, 
                            new JsOperationConstructorFrom(ctx.Options())
                            .OperationAjax(Js.NewPrefix(ctx.Prefix), JsOpSuccess.OpenPopupNoDefaultOk)),
                        OnContextualClick = ctx => Js.Confirm("Do you wish to create an album for band {0}".Formato(ctx.Entity.ToStr),
                            new JsOperationConstructorFrom(ctx.Options()).OperationAjax(Js.NewPrefix(ctx.Prefix), JsOpSuccess.OpenPopupNoDefaultOk)),
                    }},
                    { AlbumOperation.CreateGreatestHitsAlbum, new QueryOperationSettings
                    {
                        ControllerUrl = "Music/CreateGreatestHitsAlbum",
                        OnClick = ctx => new JsOperationConstructorFromMany(ctx.Options()).DefaultSubmit()
                    }},
                });
            }
        }
    }
}
