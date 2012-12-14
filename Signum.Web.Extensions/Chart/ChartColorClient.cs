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
using Signum.Web.Extensions.Properties;
using Signum.Entities;
using Signum.Web.Reports;
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
        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                if (!Navigator.Manager.EntitySettings.ContainsKey(typeof(TypeDN)))
                    Navigator.AddSetting(new EntitySettings<TypeDN>());

                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EmbeddedEntitySettings<ChartPaletteModel>() 
                    { 
                        PartialViewName = _ => ChartClient.ViewPrefix.Formato("ChartPalette"),
                        MappingDefault = new EntityMapping<ChartPaletteModel>(true)
                            .SetProperty(a => a.Colors, new MListDictionaryMapping<ChartColorDN, Lite<IdentifiableEntity>>(cc=>cc.Related, "Related",
                                new EntityMapping<ChartColorDN>(false)
                                    .SetProperty(m => m.Color, ctx=>
                                    {
                                        var input = ctx.Inputs["Rgb"];
                                        int rgb;
                                        if(input.HasText() && int.TryParse(input, System.Globalization.NumberStyles.HexNumber, null, out rgb))
                                            return ColorDN.FromARGB(255, rgb);

                                        return null;
                                    })
                                    .CreateProperty(c => c.Related)))
                    }
                });

                ChartUtils.GetChartColor = ChartColorLogic.ColorFor;

                ButtonBarEntityHelper.RegisterEntityButtons<ChartPaletteModel>((ctx, entity) =>
                {
                    var typeName = Navigator.ResolveWebTypeName(entity.Type.ToType());
                    return new[]
                    {
                        new ToolBarButton
                        {
                            Id = TypeContextUtilities.Compose(ctx.Prefix, "ebChartColorSave"),
                            Text = "Save palette",
                            OnClick = "SF.ChartColors.savePalette('{0}')".Formato(RouteHelper.New().Action<ColorChartController>(pc => pc.SavePalette(typeName)))
                        },
                        new ToolBarButton
                        {
                            Id = TypeContextUtilities.Compose(ctx.Prefix, "ebChartColorCreate"),
                            Text = "New palette",
                            Href = RouteHelper.New().Action<ColorChartController>(cc => cc.CreateNewPalette(typeName))
                        }
                    };
                });
            }
        }
    }
}