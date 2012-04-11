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
                    new EntitySettings<AlbumDN>(EntityType.DefaultNotSaving) { PartialViewName = e => ViewPrefix.Formato("Album") },
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
                                ControllerUrl = RouteHelper.New().Action("CloneWithData", "Music"),
                                Prefix = ctx.Prefix
                            }).ajax(Js.NewPrefix(ctx.Prefix), JsOpSuccess.OpenPopupNoDefaultOk).ToJS()
                        }
                    };
                });

                OperationsClient.AddSettings( new List<OperationSettings>
                {
                    new EntityOperationSettings(AlbumOperation.Clone)
                    { 
                        OnContextualClick = ctx => Js.Confirm("Do you wish to clone album {0}".Formato(ctx.Entity.ToString()),
                            new JsOperationConstructorFrom(ctx.Options()).ajax(ctx.Prefix, JsOpSuccess.DefaultContextualDispatcher)),
                        IsVisible = ctx => true,
                    },
                    new EntityOperationSettings(AlbumOperation.CreateFromBand)
                    { 
                        OnClick = ctx => new JsOperationConstructorFrom(ctx.Options("CreateAlbumFromBand", "Music"))
                            .validateAndAjax(Js.NewPrefix(ctx.Prefix), JsOpSuccess.OpenPopupNoDefaultOk),

                        OnContextualClick = ctx => Js.Confirm("Do you wish to create an album for band {0}".Formato(ctx.Entity.ToString()),
                            new JsOperationConstructorFrom(ctx.Options("CreateAlbumFromBand", "Music")).ajax(Js.NewPrefix(ctx.Prefix), JsOpSuccess.OpenPopupNoDefaultOk)),
                    },
                    new QueryOperationSettings(AlbumOperation.CreateGreatestHitsAlbum)
                    {
                        OnClick = ctx => new JsOperationConstructorFromMany(ctx.Options("CreateGreatestHitsAlbum", "Music")).submitSelected()
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
                    new EntitySettings<DemoPackageDN>(EntityType.Default){ PartialViewName = e => ViewPrefix.Formato("DemoPackage"), },
                    new EntitySettings<DemoPackageLineDN>(EntityType.Default){ PartialViewName = e => ViewPrefix.Formato("DemoPackageLine"), },
                });
            }
        }
    }
}
