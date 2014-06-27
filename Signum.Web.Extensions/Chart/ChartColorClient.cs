using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Reflection;
using Signum.Entities.DynamicQuery;
using Signum.Utilities;
using Signum.Engine.Basics;
using Signum.Entities.Chart;
using Signum.Engine.DynamicQuery;
using Signum.Entities.UserQueries;
using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Engine.Authorization;
using Signum.Engine;
using System.Web.Mvc;
using System.Web.Routing;
using Signum.Engine.Chart;
using Signum.Entities.Basics;

namespace Signum.Web.Chart
{
    public static class ChartColorClient
    {
        public static JsModule Module = new JsModule("Extensions/Signum.Web.Extensions/Chart/Scripts/ChartColors"); 

        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                if (!Navigator.Manager.EntitySettings.ContainsKey(typeof(TypeDN)))
                    Navigator.AddSetting(new EntitySettings<TypeDN>());

                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EmbeddedEntitySettings<ChartPaletteModel> 
                    { 
                        PartialViewName = _ => ChartClient.ViewPrefix.Formato("ChartPalette"),
                        MappingDefault = new EntityMapping<ChartPaletteModel>(true)
                            .SetProperty(a => a.Colors, new MListDictionaryMapping<ChartColorDN, Lite<IdentifiableEntity>>(cc=>cc.Related,
                                new EntityMapping<ChartColorDN>(false)
                                    .CreateProperty(m => m.Color)
                                    .CreateProperty(c => c.Related)))
                    }
                });

                ChartUtils.GetChartColor = ChartColorLogic.ColorFor;

                ButtonBarEntityHelper.RegisterEntityButtons<ChartPaletteModel>((ctx, entity) =>
                {
                    var typeName = Navigator.ResolveWebTypeName(entity.Type.ToType());
                    return new[]
                    {
                        new ToolBarButton(ctx.Prefix, "savePalette")
                        {
                            Text = ChartMessage.SavePalette.NiceToString(),
                            Style = BootstrapStyle.Primary,
                            OnClick = Module["savePalette"](ctx.Url.Action<ColorChartController>(pc => pc.SavePalette(typeName)))
                        },
                        new ToolBarButton(ctx.Prefix, "newPalette")
                        {
                            Text =ChartMessage.NewPalette.NiceToString(),
                            OnClick = Module["createPalette"](
                            ctx.Url.Action<ColorChartController>(pc => pc.CreateNewPalette(typeName)),
                            ChartColorLogic.Palettes.Keys,
                            ChartMessage.ChooseABasePalette.NiceToString())
                        },
                        new ToolBarButton(ctx.Prefix, "deletePalette")
                        {
                            Text =ChartMessage.DeletePalette.NiceToString(),
                            Style = BootstrapStyle.Danger,
                            OnClick = Module["deletePalette"](
                            ctx.Url.Action<ColorChartController>(pc => pc.DeletePalette(typeName)))
                        }
                    };
                });
            }
        }
    }
}