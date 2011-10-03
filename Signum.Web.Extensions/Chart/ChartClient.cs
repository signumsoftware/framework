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
            var chart = request.Chart;

            switch (chart.ChartResultType)
            { 
                case ChartResultType.TypeValue:
                    return new
                    {
                        labels = new 
                        {
                            dimension1 = chart.Dimension1.GetTitle(),
                            value1 = chart.Value1.GetTitle() 
                        },
                        serie = chart.GroupResults ? 
                            resultTable.Rows.Select(r => new Dictionary<string, object>
                            { 
                                { "dimension1", Convert(r[0]) }, 
                                { "value1", Convert(r[1]) }
                            }).ToList() :
                            resultTable.Rows.Select(r => new Dictionary<string, object>
                            { 
                                { "dimension1", Convert(r[0]) }, 
                                { "value1", Convert(r[1]) },
                                { "entity", r.Entity.Key() }
                            }).ToList()
                    };
                case ChartResultType.TypeTypeValue:
                    
                    var dimension1Values = resultTable.Rows.Select(r => r[0]).Distinct().ToList();

                    return new
                    {
                        labels = new 
                        {
                            dimension1 = chart.Dimension1.GetTitle(),
                            dimension2 = chart.Dimension2.GetTitle(),
                            value1 = chart.Value1.GetTitle() 
                        },
                        dimension1 = dimension1Values.Select(Convert).ToList(),
                        series = resultTable.Rows.Select(r => r[1]).Distinct().Select(d2 => new 
                        { 
                            dimension2 = Convert(d2),
                            values = (dimension1Values
                                .Select(d1 => resultTable.Rows.FirstOrDefault(r => object.Equals(r[0], d1) && object.Equals(r[1], d2))
                                .TryCC(r => r[2]))).ToList()
                        }).ToList()
                    };

                case ChartResultType.Points:
                    return new
                    {
                        labels = new
                        {
                            value1 = chart.Value1.GetTitle(),
                            dimension1 = chart.Dimension1.GetTitle(),
                            dimension2 = chart.Dimension2.GetTitle(),
                        },
                        points = resultTable.Rows.Select(r => new 
                        { 
                            value1 = Convert(r[2]), 
                            dimension1 = Convert(r[0]), 
                            dimension2 = Convert(r[1]) 
                        }).ToList()
                    };

                case ChartResultType.Bubbles:
                    return new
                    {
                        labels = new
                        {
                            value1 = chart.Value1.GetTitle(),
                            dimension1 = chart.Dimension1.GetTitle(),
                            dimension2 = chart.Dimension2.GetTitle(),
                            value2 = chart.Value2.GetTitle()
                        },
                        points = resultTable.Rows.Select(r => new
                        {
                            value1 = Convert(r[2]),
                            dimension1 = Convert(r[0]),
                            dimension2 = Convert(r[1]),
                            value2 = Convert(r[3])
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