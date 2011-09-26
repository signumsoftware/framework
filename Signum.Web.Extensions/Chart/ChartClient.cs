using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Reflection;
using Signum.Web;
using Signum.Web.Extensions.Properties;
using Signum.Entities.Chart;
using Signum.Utilities;
using Signum.Engine.DynamicQuery;
using Signum.Entities.DynamicQuery;
using System.Web.Script.Serialization;
using Signum.Entities;
using Signum.Engine;

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
                    OnClick =  Js.SubmitOnly(RouteHelper.New().Action("Index", "Chart"), new JsFindNavigator(prefix).requestData()).ToJS(),
                    DivCssClass = ToolBarButton.DefaultQueryCssClass
                }
            };
        }

        public static object DataJson(ChartRequest request, ResultTable resultTable)
        {
            var labelDictionary = request.ChartTokens()
                .Select((t, i) => KVP.Create("token" + (i + 1), t.DisplayName.HasText() ? t.DisplayName : t.Token.NiceName()))
                .ToDictionary();

            switch (request.Chart.ChartResultType)
            { 
                case ChartResultType.TypeValue:
                    return new
                    {
                        labels = labelDictionary,
                        values = resultTable.Rows.Select(r => resultTable.Columns.Select((c, i) => KVP.Create("token" + (i + 1), Convert(r[c]))).ToDictionary()).ToList()
                    };
                case ChartResultType.TypeTypeValue:
                    //Results grouped in series for distinct token2
                    return new
                    {
                        labels = labelDictionary,
                        series = resultTable.Rows.Select(r => resultTable.Columns.Select((c, i) => KVP.Create("token" + (i + 1), Convert(r[c]))).ToDictionary())
                            .GroupBy(dic => dic["token2"]).Select(gr => new
                            {
                                token2 = gr.Key,
                                values = gr.Select(t => new { token1 = t["token1"], token3 = t["token3"] }).ToList()
                            }).ToList()
                    };
                default:
                    throw new NotImplementedException("");
            }
        }

        private static object Convert(object p)
        {
            if (p is Lite)
            {
                Lite l = (Lite)p;
                return new
                {
                    key = l.Key(),
                    toStr = l.ToStr
                };
            }
            else
                return p;
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