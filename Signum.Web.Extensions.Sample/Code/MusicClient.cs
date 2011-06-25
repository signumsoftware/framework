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
using System.Web.Mvc;

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

                    new EntitySettings<NoteWithDateDN>(EntityType.Default) { PartialViewName = e => ViewPrefix.Formato("Note") },

                    new EmbeddedEntitySettings<AlbumFromBandModel>(){PartialViewName = e => ViewPrefix.Formato("AlbumFromBandModel")},
                });

                QuickLinkWidgetHelper.RegisterEntityLinks<LabelDN>((helper, entity, partialViewName, prefix) =>
                {
                    if (entity.IsNew)
                        return null;

                    return new QuickLink[]
                    {
                        new QuickLinkFind(typeof(AlbumDN), "Label", entity, true)
                    };
                });

                ButtonBarEntityHelper.RegisterEntityButtons<AlbumDN>((ctx, entity, partialViewName, prefix) =>
                {
                    if (entity.IsNew)
                        return null;

                    return new ToolBarButton[]
                    {
                        new ToolBarButton
                        {
                            DivCssClass = ToolBarButton.DefaultEntityDivCssClass,
                            Id = TypeContextUtilities.Compose(prefix, "CloneWithData"),
                            Text = "Clone with data",
                            OnClick = new JsOperationConstructorFrom(new JsOperationOptions
                            { 
                                ControllerUrl = RouteHelper.New().Action("CloneWithData", "Music"),
                                Prefix = prefix
                            }).ajax(Js.NewPrefix(prefix), JsOpSuccess.OpenPopupNoDefaultOk).ToJS()
                        }
                    };
                });

                OperationsClient.Manager.Settings.AddRange(new Dictionary<Enum, OperationSettings>
                {
                    { AlbumOperation.Clone, new EntityOperationSettings 
                    { 
                        OnClick = ctx => new JsOperationConstructorFrom(ctx.Options()).validateAndSubmit(),
                        OnContextualClick = ctx => Js.Confirm("Do you wish to clone album {0}".Formato(ctx.Entity.ToStr),
                            new JsOperationConstructorFrom(ctx.Options()).ajax(ctx.Prefix, JsOpSuccess.DefaultContextualDispatcher)),
                        IsVisible = ctx => true,
                    }},
                    { AlbumOperation.CreateFromBand, new EntityOperationSettings 
                    { 
                        OnClick = ctx => new JsOperationConstructorFrom(ctx.Options("CreateAlbumFromBand", "Music"))
                            .validateAndAjax(Js.NewPrefix(ctx.Prefix), JsOpSuccess.OpenPopupNoDefaultOk),

                        OnContextualClick = ctx => Js.Confirm("Do you wish to create an album for band {0}".Formato(ctx.Entity.ToStr),
                            new JsOperationConstructorFrom(ctx.Options("CreateAlbumFromBand", "Music")).ajax(Js.NewPrefix(ctx.Prefix), JsOpSuccess.OpenPopupNoDefaultOk)),
                    }},
                    { AlbumOperation.CreateGreatestHitsAlbum, new QueryOperationSettings
                    {
                        OnClick = ctx => new JsOperationConstructorFromMany(ctx.Options("CreateGreatestHitsAlbum", "Music")).submitSelected()
                    }},
                });
            }
        }
    }
}
