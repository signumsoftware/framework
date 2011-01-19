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

        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Func<string, string> absolute = viewName => "~/Views/Music/{0}.cshtml".Formato(viewName);

                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<AlbumDN>(EntityType.NotSaving) { PartialViewName = e => absolute("Album") },
                    new EntitySettings<AmericanMusicAwardDN>(EntityType.Default) { PartialViewName = e => absolute("AmericanMusicAward") },
                    new EntitySettings<ArtistDN>(EntityType.Default) { PartialViewName = e => absolute("Artist") },
                    new EntitySettings<BandDN>(EntityType.Default) { PartialViewName = e => absolute("Band") },
                    new EntitySettings<GrammyAwardDN>(EntityType.Default) { PartialViewName = e => absolute("GrammyAward") },
                    new EntitySettings<LabelDN>(EntityType.Default) { PartialViewName = e => absolute("Label") },
                    new EntitySettings<PersonalAwardDN>(EntityType.Default) { PartialViewName = e => absolute("PersonalAward") },
                    new EmbeddedEntitySettings<SongDN>() { PartialViewName = e => absolute("Song")},

                    new EmbeddedEntitySettings<AlbumFromBandModel>(){PartialViewName = e => absolute("AlbumFromBandModel")},
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
