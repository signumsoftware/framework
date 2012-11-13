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
using Signum.Entities.Processes;

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
                    new EntitySettings<AlbumDN>(EntityType.Main) { PartialViewName = e => ViewPrefix.Formato("Album") },
                    new EntitySettings<AmericanMusicAwardDN>(EntityType.Shared) { PartialViewName = e => ViewPrefix.Formato("AmericanMusicAward") },
                    new EntitySettings<ArtistDN>(EntityType.Shared) { PartialViewName = e => ViewPrefix.Formato("Artist") },
                    new EntitySettings<BandDN>(EntityType.Main) { PartialViewName = e => ViewPrefix.Formato("Band") },
                    new EntitySettings<GrammyAwardDN>(EntityType.Shared) { PartialViewName = e => ViewPrefix.Formato("GrammyAward") },
                    new EntitySettings<LabelDN>(EntityType.Main) { PartialViewName = e => ViewPrefix.Formato("Label") },
                    new EntitySettings<PersonalAwardDN>(EntityType.Shared) { PartialViewName = e => ViewPrefix.Formato("PersonalAward") },
                    new EmbeddedEntitySettings<SongDN>() { PartialViewName = e => ViewPrefix.Formato("Song")},

                    new EntitySettings<NoteWithDateDN>(EntityType.Shared) { PartialViewName = e => ViewPrefix.Formato("NoteWithDate") },

                    new EmbeddedEntitySettings<AlbumFromBandModel>(){PartialViewName = e => ViewPrefix.Formato("AlbumFromBandModel")},
                });

                QuickLinkWidgetHelper.RegisterEntityLinks<LabelDN>((entity, partialViewName, prefix) =>
                {
                    if (entity.IsNew)
                        return null;

                    return new QuickLink[]
                    {
                        new QuickLinkFind(typeof(AlbumDN), "Label", entity, true)
                    };
                });

                ButtonBarEntityHelper.RegisterEntityButtons<AlbumDN>((ctx, entity) =>
                {
                    if (entity.IsNew)
                        return null;

                    return new ToolBarButton[]
                    {
                        new ToolBarButton
                        {
                            DivCssClass = ToolBarButton.DefaultEntityDivCssClass,
                            Id = TypeContextUtilities.Compose(ctx.Prefix, "CloneWithData"),
                            Text = "Clone with data",
                            OnClick = new JsOperationConstructorFrom(new JsOperationOptions
                            { 
                                ControllerUrl = RouteHelper.New().Action<MusicController>(mc => mc.CloneWithData(Js.NewPrefix(ctx.Prefix))),
                                Prefix = ctx.Prefix
                            }).ajax(Js.NewPrefix(ctx.Prefix), JsOpSuccess.OpenPopupNoDefaultOk).ToJS()
                        }
                    };
                });

                OperationsClient.AddSettings( new List<OperationSettings>
                {
                    new EntityOperationSettings(AlbumOperation.CreateFromBand)
                    { 
                        OnClick = ctx => new JsOperationConstructorFrom(ctx.Options<MusicController>(mc => mc.CreateAlbumFromBand(Js.NewPrefix(ctx.Prefix))))
                            .ajax(Js.NewPrefix(ctx.Prefix), JsOpSuccess.OpenPopupNoDefaultOk),

                        Contextual = new ContextualOperationSettings(AlbumOperation.CreateFromBand)
                        {
                            OnClick = ctx => new JsOperationConstructorFrom(ctx.Options<MusicController>(mc => mc.CreateAlbumFromBand(Js.NewPrefix(ctx.Prefix))))
                                .ajax(Js.NewPrefix(ctx.Prefix), JsOpSuccess.OpenPopupNoDefaultOk)
                        },

                        ContextualFromMany = new ContextualOperationSettings(AlbumOperation.CreateFromBand)
                    },
                    new ContextualOperationSettings(AlbumOperation.CreateGreatestHitsAlbum)
                    {
                        OnClick = ctx => new JsOperationConstructorFromMany(ctx.Options<MusicController>(mc => mc.CreateGreatestHitsAlbum()))
                            .submitSelected()
                    },
                });
            }
        }

        public static void StartDemoPackage()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Constructor.AddConstructor(() => new DemoPackageDN
                {
                    Name = "Demo Package",
                    RequestedLines = 100,
                    DelayMilliseconds = 100,
                    ErrorRate = 0.3,
                    MainError = false,
                });

                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<DemoPackageDN>(EntityType.Main){ PartialViewName = e => ViewPrefix.Formato("DemoPackage"), },
                    new EntitySettings<DemoPackageLineDN>(EntityType.Main){ PartialViewName = e => ViewPrefix.Formato("DemoPackageLine"), },
                });
            }
        }
    }
}
