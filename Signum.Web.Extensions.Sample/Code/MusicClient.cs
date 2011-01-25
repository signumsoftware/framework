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
        public static string ViewPrefix = "~/Views/Music/{0}.cshtml";

        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<AlbumDN>(EntityType.NotSaving) { PartialViewName = e => ViewPrefix.Formato("Album") },
                    new EntitySettings<AmericanMusicAwardDN>(EntityType.Default) { PartialViewName = e => ViewPrefix.Formato("AmericanMusicAward") },
                    new EntitySettings<ArtistDN>(EntityType.Default) { PartialViewName = e => ViewPrefix.Formato("Artist") },
                    new EntitySettings<BandDN>(EntityType.Default) { PartialViewName = e => ViewPrefix.Formato("Band") },
                    new EntitySettings<GrammyAwardDN>(EntityType.Default) { PartialViewName = e => ViewPrefix.Formato("GrammyAward") },
                    new EntitySettings<LabelDN>(EntityType.Default) { PartialViewName = e => ViewPrefix.Formato("Label") },
                    new EntitySettings<PersonalAwardDN>(EntityType.Default) { PartialViewName = e => ViewPrefix.Formato("PersonalAward") },
                    new EmbeddedEntitySettings<SongDN>() { PartialViewName = e => ViewPrefix.Formato("Song")},

                    new EntitySettings<NoteDN>(EntityType.Default) { PartialViewName = e => ViewPrefix.Formato("Note") },


                    new EmbeddedEntitySettings<AlbumFromBandModel>(){PartialViewName = e => ViewPrefix.Formato("AlbumFromBandModel")},
                });

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
                        OnClick = ctx => JsValidator.EntityIsValid(ctx.Prefix, 
                            new JsOperationConstructorFrom(ctx.Options("CreateAlbumFromBand", "Music"))
                            .OperationAjax(Js.NewPrefix(ctx.Prefix), JsOpSuccess.OpenPopupNoDefaultOk)),
                        OnContextualClick = ctx => Js.Confirm("Do you wish to create an album for band {0}".Formato(ctx.Entity.ToStr),
                            new JsOperationConstructorFrom(ctx.Options("CreateAlbumFromBand", "Music")).OperationAjax(Js.NewPrefix(ctx.Prefix), JsOpSuccess.OpenPopupNoDefaultOk)),
                    }},
                    { AlbumOperation.CreateGreatestHitsAlbum, new QueryOperationSettings
                    {
                        OnClick = ctx => new JsOperationConstructorFromMany(ctx.Options("CreateGreatestHitsAlbum", "Music")).DefaultSubmit()
                    }},
                });
            }
        }
    }
}
