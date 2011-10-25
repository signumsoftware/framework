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
using System.Web.Routing;
using System.Web.Mvc;
using Signum.Entities.Basics;
using Signum.Engine.Basics;
using Signum.Engine.Extensions.Chart;

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
                    new EmbeddedEntitySettings<ChartTokenDN> { PartialViewName = _ => ViewPrefix.Formato("ChartToken") },

                    new EntitySettings<UserChartDN>(EntityType.Default) 
                    { 
                        PartialViewName = _ => ViewPrefix.Formato("UserChart"),
                        MappingAdmin = new EntityMapping<UserChartDN>(true)
                            .SetProperty(cr => cr.Chart, new EntityMapping<ChartBase>(true)
                                .SetProperty(cb => cb.Dimension1, mappingChartToken)
                                .SetProperty(cb => cb.Dimension2, mappingChartToken)
                                .SetProperty(cb => cb.Value1, mappingChartToken)
                                .SetProperty(cb => cb.Value2, mappingChartToken))
                    },
                });

                ButtonBarQueryHelper.GetButtonBarForQueryName += new GetToolBarButtonQueryDelegate(ButtonBarQueryHelper_GetButtonBarForQueryName);

                RouteTable.Routes.MapRoute(null, "ChartFor/{webQueryName}",
                    new { controller = "Chart", action = "Index", webQueryName = "" });

                RouteTable.Routes.MapRoute(null, "UC/{webQueryName}/{lite}",
                     new { controller = "Chart", action = "ViewUserChart" });

                UserChartDN.SetConverters(query => QueryLogic.ToQueryName(query.Key), queryname => QueryLogic.RetrieveOrGenerateQuery(queryname));

                ButtonBarEntityHelper.RegisterEntityButtons<UserChartDN>((ctx, entity) =>
                {
                    var buttons = new List<ToolBarButton> {};
                    
                    if (!entity.IsNew)
                    {
                        buttons.Add(new ToolBarButton
                        {
                            Id = TypeContextUtilities.Compose(ctx.Prefix, "ebUserChartDelete"),
                            Text = Resources.Delete,
                            OnClick = Js.Confirm(Resources.Chart_AreYouSureOfDeletingUserChart0.Formato(entity.DisplayName),
                                Js.Submit(RouteHelper.New().Action<ChartController>(cc => cc.DeleteUserChart(entity.ToLite())))).ToJS()
                        });
                    }

                    return buttons.ToArray();
                });
            }
        }

        static EntityMapping<ChartTokenDN> mappingChartToken = new EntityMapping<ChartTokenDN>(true)
            .SetProperty(ct => ct.Token, ctx =>
            {
                var tokenName = "";

                var chartTokenInputs = ctx.Parent.Inputs;
                bool stop = false;
                for (var i = 0; !stop; i++)
                {
                    var subtokenName = chartTokenInputs.TryGetC("ddlTokens_" + i);
                    if (string.IsNullOrEmpty(subtokenName))
                        stop = true;
                    else
                        tokenName = tokenName.HasText() ? (tokenName + "." + subtokenName) : subtokenName;
                }

                if (string.IsNullOrEmpty(tokenName))
                    return null;

                var qd = DynamicQueryManager.Current.QueryDescription(
                    Navigator.ResolveQueryName(ctx.GlobalInputs[TypeContextUtilities.Compose(ctx.Root.ControlID, ViewDataKeys.QueryName)]));

                return QueryUtils.Parse(tokenName, qd);
            })
            .SetProperty(ct => ct.DisplayName, ctx =>
            {
                if (string.IsNullOrEmpty(ctx.Input))
                    return ctx.None();

                return ctx.Input;
            })
            .SetProperty(ct => ct.Format, ctx =>
            {
                if (string.IsNullOrEmpty(ctx.Input))
                    return ctx.None();

                return ctx.Input;
            })
            .SetProperty(ct => ct.Unit, ctx =>
            {
                if (string.IsNullOrEmpty(ctx.Input))
                    return ctx.None();

                return ctx.Input;
            })
            .SetProperty(ct => ct.OrderType, ctx =>
            {
                if (string.IsNullOrEmpty(ctx.Input))
                    return ctx.None();
                 
                return ctx.Input.ToEnum<OrderType>();
            })
            .SetProperty(ct => ct.OrderPriority, ctx =>
            {
                if (string.IsNullOrEmpty(ctx.Input))
                    return ctx.None();

                return ctx.Input.ToInt();
            });

        public static EntityMapping<ChartRequest> MappingChartRequest = new EntityMapping<ChartRequest>(true)
            .SetProperty(cr => cr.Chart, new EntityMapping<ChartBase>(true)
                .SetProperty(cb => cb.Dimension1, mappingChartToken)
                .SetProperty(cb => cb.Dimension2, mappingChartToken)
                .SetProperty(cb => cb.Value1, mappingChartToken)
                .SetProperty(cb => cb.Value2, mappingChartToken))
            .SetProperty(cr => cr.Filters, ctx => ExtractChartFilters(ctx));

        static List<Entities.DynamicQuery.Filter> ExtractChartFilters(MappingContext<List<Entities.DynamicQuery.Filter>> ctx)
        {
            var qd = DynamicQueryManager.Current.QueryDescription(
                Navigator.ResolveQueryName(ctx.GlobalInputs[TypeContextUtilities.Compose(ctx.Root.ControlID, ViewDataKeys.QueryName)]));

            return FindOptionsModelBinder.ExtractFilterOptions(ctx.ControllerContext.HttpContext, qd).Select(fo => fo.ToFilter()).ToList();
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

        public static List<ToolBarButton> GetChartMenu(ControllerContext controllerContext, object queryName, Type entityType, string prefix)
        {
            var items = new List<ToolBarButton>();

            Lite<UserChartDN> currentUserChart = null;
            string url = (controllerContext.RouteData.Route as Route).TryCC(r => r.Url);
            if (url.HasText() && url.Contains("UC"))
                currentUserChart = new Lite<UserChartDN>(int.Parse(controllerContext.RouteData.Values["lite"].ToString()));

            foreach (var uc in ChartLogic.GetUserCharts(queryName))
            {
                string ucName = uc.InDB().Select(q => q.DisplayName).SingleOrDefaultEx();
                items.Add(new ToolBarButton
                {
                    Text = ucName,
                    AltText = ucName,
                    Href = RouteHelper.New().Action<ChartController>(c => c.ViewUserChart(uc)),
                    DivCssClass = ToolBarButton.DefaultQueryCssClass + (currentUserChart.Is(uc) ? " sf-userchart-selected" : "")
                });
            }

            if (items.Count > 0)
                items.Add(new ToolBarSeparator());

            string uqNewText = Resources.UserChart_CreateNew;
            items.Add(new ToolBarButton
            {
                Id = TypeContextUtilities.Compose(prefix, "qbUserChartNew"),
                AltText = uqNewText,
                Text = uqNewText,
                OnClick = Js.Submit(RouteHelper.New().Action("CreateUserChart", "Chart"), "SF.Chart.Builder.requestProcessedData({0})".Formato(prefix)).ToJS(),
                DivCssClass = ToolBarButton.DefaultQueryCssClass
            });

            if (currentUserChart != null)
            {
                string ucEditText = Resources.UserChart_Edit;
                items.Add(new ToolBarButton
                {
                    Id = TypeContextUtilities.Compose(prefix, "qbUserChartEdit"),
                    AltText = ucEditText,
                    Text = ucEditText,
                    Href = Navigator.ViewRoute(currentUserChart),
                    DivCssClass = ToolBarButton.DefaultQueryCssClass
                });
            }

            string ucUserChartText = Resources.UserChart_UserCharts;
            return new List<ToolBarButton> {
                new ToolBarMenu
                {
                    Id = TypeContextUtilities.Compose(prefix, "tmUserCharts"),
                    AltText = ucUserChartText,
                    Text = ucUserChartText,
                    DivCssClass = ToolBarButton.DefaultQueryCssClass,
                    Items = items
                }
            };
        }

        public static object DataJson(ChartRequest request, ResultTable resultTable)
        {
            var chart = request.Chart;

            var d1Converter = chart.Dimension1.Converter();
            var d2Converter = chart.Dimension2.Converter();
            var v1Converter = chart.Value1.Converter();
            var v2Converter = chart.Value2.Converter();

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
                                { "dimension1", d1Converter(r[0]) }, 
                                { "value1", v1Converter(r[1]) }
                            }).ToList() :
                            resultTable.Rows.Select(r => new Dictionary<string, object>
                            { 
                                { "dimension1", d1Converter(r[0]) }, 
                                { "value1", v1Converter(r[1]) },
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
                        dimension1 = dimension1Values.Select(d1Converter).ToList(),
                        series = resultTable.Rows.Select(r => r[1]).Distinct().Select(dim2 => new 
                        {
                            dimension2 = d2Converter(dim2),
                            values = (dimension1Values
                                .Select(dim1 => resultTable.Rows.FirstOrDefault(r => object.Equals(r[0], dim1) && object.Equals(r[1], dim2))
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
                            value1 = v1Converter(r[2]),
                            dimension1 = d1Converter(r[0]),
                            dimension2 = d2Converter(r[1]) 
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
                            value1 = v1Converter(r[2]),
                            dimension1 = d1Converter(r[0]),
                            dimension2 = d2Converter(r[1]),
                            value2 = v2Converter(r[3])
                        }).ToList()
                    };
                default:
                    throw new NotImplementedException("");
            }
        }

        private static Func<object,object> Converter(this ChartTokenDN token)
        {
            if (token == null)
                return null;

            if (typeof(Lite).IsAssignableFrom( token.Type))
            {
                return p =>
                {
                    Lite l = (Lite)p;
                    return new
                    {
                        key = l.Key(),
                        toStr = l.ToStr
                    };
                };
            }
            else if (token.Type.UnNullify().IsEnum)
            {
                return p =>
                {
                    Enum e = (Enum)p;
                    return new
                    {
                        key = e.ToString(),
                        toStr = e.NiceToString()
                    };
                };
            }
            else if (typeof(IFormattable).IsAssignableFrom(token.Type.UnNullify()) && token.Format.HasText())
            {
                return p =>
                {
                    return new
                    {
                        key = p,
                        toStr = (p as IFormattable).TryToString(token.Format) ?? p.TryToString() ?? ""
                    };
                };
            }
            else if (typeof(DateTime) == token.Type.UnNullify())
            {
                return p =>
                {
                    DateTime e = (DateTime)p;
                    return new
                    {
                        key = e,
                        toStr = e.TryToString(),
                    };
                };
            }
            else
                return p => p;
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