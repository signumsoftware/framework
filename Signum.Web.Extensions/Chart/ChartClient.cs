using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Reflection;
using Signum.Web;
using Signum.Web.Extensions.Properties;
using Signum.Entities.Chart;
using Signum.Utilities;

namespace Signum.Web.Chart
{
    public static class ChartClient
    {
        public static string ViewPrefix = "~/Chart/Views/{0}.cshtml";

        public static string ChartControlView = ViewPrefix.Formato("ChartControl");
        public static string ChartBuilderView = ViewPrefix.Formato("ChartBuilder");
        public static string ChartResultsView = ViewPrefix.Formato("ChartResults");

        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.RegisterArea(typeof(ChartClient));

                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EmbeddedEntitySettings<ChartRequest>(),
                    new EmbeddedEntitySettings<ChartBase>(),
                    new EmbeddedEntitySettings<ChartTokenDN> { PartialViewName = _ => ViewPrefix.Formato("ChartToken") }
                });

                Mapping.RegisterValue<ChartRequest>(new ChartRequestMapping().GetValue);

                ButtonBarQueryHelper.GetButtonBarForQueryName += new GetToolBarButtonQueryDelegate(ButtonBarQueryHelper_GetButtonBarForQueryName);
            }
        }

        class ChartRequestMapping : EntityMapping<ChartRequest>
        {
            public ChartRequestMapping()
                : base(true)
            {

            }

            public override ChartRequest GetValue(MappingContext<ChartRequest> ctx)
            {
                ctx.Value = new ChartRequest(Navigator.ResolveQueryName(ctx.Inputs[ViewDataKeys.QueryName]));
                SetProperties(ctx);
                RecursiveValidation(ctx);
                return ctx.Value;
            }
        }

        static ToolBarButton[] ButtonBarQueryHelper_GetButtonBarForQueryName(System.Web.Mvc.ControllerContext controllerContext, object queryName, Type entityType, string prefix)
        {
            string chartNewText = Resources.Chart_Chart;
            return new ToolBarButton[] {
                new ToolBarButton
                {
                    Id = TypeContextUtilities.Compose(prefix, "qbChartNew"),
                    AltText = chartNewText,
                    Text = chartNewText,
                    OnClick =  Js.SubmitOnly(RouteHelper.New().Action("Index", "Chart"), new JsFindNavigator(prefix).requestData()).ToJS(),
                    DivCssClass = ToolBarButton.DefaultQueryCssClass
                }
            };
        }

        public static string ChartTypeImgClass(ChartBase chart, ChartType type)
        {
            string css = "sf-chart-img sf-chart-img-" + type.ToString().ToLower();

            if (ChartUtils.GetChartResultType(type) == chart.ChartResultType)
                css += " sf-chart-img-equiv";

            if (type == chart.ChartType)
                css += " sf-chart-img-curr";
            
            return css;
        }
    }
}