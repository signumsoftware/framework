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

        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.RegisterArea(typeof(ChartClient));

                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EmbeddedEntitySettings<ChartTokenDN> { PartialViewName = _ => ViewPrefix.Formato("ChartToken") }
                });

                ButtonBarQueryHelper.GetButtonBarForQueryName += new GetToolBarButtonQueryDelegate(ButtonBarQueryHelper_GetButtonBarForQueryName);
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
                    Href = "",
                    //OnClick =  Js.SubmitOnly(RouteHelper.New().Action("Create", "UserQueries"), new JsFindNavigator(prefix).requestData()).ToJS(),
                    DivCssClass = ToolBarButton.DefaultQueryCssClass
                }
            };
        }
    }
}