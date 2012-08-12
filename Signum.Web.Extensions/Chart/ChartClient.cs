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
using Signum.Web.Reports;
using Signum.Entities.UserQueries;
using Signum.Engine.Chart;
using Signum.Engine.Authorization;
using Signum.Entities.Authorization;
using Signum.Entities.Reflection;

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

                Mapping<QueryToken> qtMapping = ctx =>
                {
                    string tokenStr = "";
                    foreach (string key in ctx.Parent.Inputs.Keys.Where(k => k.Contains("ddlTokens")).Order())
                        tokenStr += ctx.Parent.Inputs[key] + ".";
                    while (tokenStr.EndsWith("."))
                        tokenStr = tokenStr.Substring(0, tokenStr.Length - 1);

                    string queryKey = ctx.Parent.Parent.Parent.Inputs[TypeContextUtilities.Compose("Query", "Key")];
                    object queryName = QueryLogic.ToQueryName(queryKey);
            
                    var chart = ((UserChartDN)ctx.Parent.Parent.Parent.UntypedValue);

                    QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryName);
                    return QueryUtils.Parse(tokenStr, qt => qt.SubTokensChart(qd.Columns, chart.GroupResults));
                };

                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EmbeddedEntitySettings<ChartRequest>(),
                    new EmbeddedEntitySettings<ChartColumnDN> { PartialViewName = _ => ViewPrefix.Formato("ChartToken") },
                    new EmbeddedEntitySettings<ChartScriptColumnDN>{ PartialViewName = _ => ViewPrefix.Formato("ChartScriptColumn") },
                    new EntitySettings<ChartScriptDN>(EntityType.Admin) { PartialViewName = _ => ViewPrefix.Formato("ChartScript") },

                    new EntitySettings<UserChartDN>(EntityType.Default) 
                    { 
                        PartialViewName = _ => ViewPrefix.Formato("UserChart"),
                        MappingAdmin = new EntityMapping<UserChartDN>(true)
                        
                            .SetProperty(cb=>cb.Columns, new MListMapping<ChartColumnDN>(mappingChartColumn))
                            .SetProperty(cr => cr.Filters, new MListMapping<QueryFilterDN>
                            {
                                ElementMapping = new EntityMapping<QueryFilterDN>(false)
                                    .CreateProperty(a=>a.Operation)
                                    .CreateProperty(a=>a.ValueString)
                                    .SetProperty(a=>a.Token, qtMapping)
                            })
                            .SetProperty(cr => cr.Orders, new MListMapping<QueryOrderDN>
                            {
                                ElementMapping = new EntityMapping<QueryOrderDN>(false)
                                    .CreateProperty(a=>a.OrderType)
                                    .SetProperty(a=>a.Token, qtMapping)
                            })
                    },

                    new EmbeddedEntitySettings<ChartPaletteModel>() 
                    { 
                        ShowSave = false,
                        PartialViewName = _ => ViewPrefix.Formato("ChartPalette"),
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

                ButtonBarQueryHelper.RegisterGlobalButtons(ButtonBarQueryHelper_GetButtonBarForQueryName);

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

                ButtonBarEntityHelper.RegisterEntityButtons<ChartPaletteModel>((ctx, entity) =>
                {
                    var typeName = Navigator.ResolveWebTypeName(entity.Type.ToType());
                    return new []
                    {
                        new ToolBarButton
                        {
                            Id = TypeContextUtilities.Compose(ctx.Prefix, "ebChartColorSave"),
                            Text = "Save palette",
                            OnClick = "SF.ChartColors.savePalette('{0}')".Formato(RouteHelper.New().Action((ChartController pc) => pc.SavePalette(typeName)))
                        },
                        new ToolBarButton
                        {
                            Id = TypeContextUtilities.Compose(ctx.Prefix, "ebChartColorCreate"),
                            Text = "New palette",
                            Href = RouteHelper.New().Action<ChartController>(cc => cc.CreateNewPalette(typeName))
                        }
                    };
                });
            }
        }

        static EntityMapping<ChartColumnDN> mappingChartColumn = new EntityMapping<ChartColumnDN>(true)
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
                    return ctx.None();

                var qd = DynamicQueryManager.Current.QueryDescription(
                    Navigator.ResolveQueryName(ctx.ControllerContext.HttpContext.Request.Params["webQueryName"]));

                var chartToken = (ChartColumnDN)ctx.Parent.UntypedValue;
                var chart = (IChartBase)ctx.Parent.Parent.UntypedValue;

                return QueryUtils.Parse(tokenName, qt => qt.SubTokensChart(qd.Columns, chart.GroupResults));
            })
            .SetProperty(ct => ct.DisplayName, ctx =>
            {
                if (string.IsNullOrEmpty(ctx.Input))
                    return ctx.None();

                return ctx.Input;
            });

        public static EntityMapping<ChartRequest> MappingChartRequest = new EntityMapping<ChartRequest>(true)
            .SetProperty(cr => cr.Filters, ctx => ExtractChartFilters(ctx))
            .SetProperty(cr => cr.Orders, ctx => ExtractChartOrders(ctx))
            .SetProperty(cb => cb.Columns, new MListMapping<ChartColumnDN>(mappingChartColumn));

        static List<Entities.DynamicQuery.Filter> ExtractChartFilters(MappingContext<List<Entities.DynamicQuery.Filter>> ctx)
        {
            var qd = DynamicQueryManager.Current.QueryDescription(
                Navigator.ResolveQueryName(ctx.ControllerContext.HttpContext.Request.Params["webQueryName"]));

            ChartRequest chartRequest = (ChartRequest)ctx.Parent.UntypedValue;

            return FindOptionsModelBinder.ExtractFilterOptions(ctx.ControllerContext.HttpContext, qt => qt.SubTokensChart(qd.Columns, chartRequest.GroupResults))
                .Select(fo => fo.ToFilter()).ToList();
        }

        static List<Order> ExtractChartOrders(MappingContext<List<Order>> ctx)
        {
            var qd = DynamicQueryManager.Current.QueryDescription(
                Navigator.ResolveQueryName(ctx.ControllerContext.HttpContext.Request.Params["webQueryName"]));

            ChartRequest chartRequest = (ChartRequest)ctx.Parent.UntypedValue;

            return FindOptionsModelBinder.ExtractOrderOptions(ctx.ControllerContext.HttpContext, qt => qt.SubTokensChart(qd.Columns, chartRequest.GroupResults))
                .Select(fo => fo.ToOrder()).ToList();
        }

        static ToolBarButton[] ButtonBarQueryHelper_GetButtonBarForQueryName(QueryButtonContext ctx)
        {
            if (ctx.Prefix.HasText())
                return null;

            return new[] { ChartQueryButton(ctx.Prefix) };
        }

        public static ToolBarButton ChartQueryButton(string prefix)
        {
            if (!ChartPermissions.ViewCharting.IsAuthorized())
                return null;

            string chartNewText = Resources.Chart_Chart;

            return
                new ToolBarButton
                {
                    Id = TypeContextUtilities.Compose(prefix, "qbChartNew"),
                    AltText = chartNewText,
                    Text = chartNewText,
                    OnClick = Js.SubmitOnly(RouteHelper.New().Action("Index", "Chart"), JsFindNavigator.GetFor(prefix).requestData()).ToJS(),
                    DivCssClass = ToolBarButton.DefaultQueryCssClass
                };
        }

        public static List<ToolBarButton> GetChartMenu(ControllerContext controllerContext, object queryName, Type entityType, string prefix)
        {
            if (!Navigator.IsViewable(typeof(UserChartDN), EntitySettingsContext.Admin))
                return null;
            
            var items = new List<ToolBarButton>();

            Lite<UserChartDN> currentUserChart = null;
            string url = (controllerContext.RouteData.Route as Route).TryCC(r => r.Url);
            if (url.HasText() && url.Contains("UC"))
                currentUserChart = new Lite<UserChartDN>(int.Parse(controllerContext.RouteData.Values["lite"].ToString()));

            foreach (var uc in UserChartLogic.GetUserCharts(queryName))
            {
                items.Add(new ToolBarButton
                {
                    Text = uc.ToString(),
                    AltText = uc.ToString(),
                    Href = RouteHelper.New().Action<ChartController>(c => c.ViewUserChart(uc)),
                    DivCssClass = ToolBarButton.DefaultQueryCssClass + (currentUserChart.Is(uc) ? " sf-userchart-selected" : "")
                });
            }

            if (items.Count > 0)
                items.Add(new ToolBarSeparator());

            if (Navigator.IsCreable(typeof(UserChartDN), EntitySettingsContext.Admin))
            {
                string uqNewText = Resources.UserChart_CreateNew;
                items.Add(new ToolBarButton
                {
                    Id = TypeContextUtilities.Compose(prefix, "qbUserChartNew"),
                    AltText = uqNewText,
                    Text = uqNewText,
                    OnClick = Js.Submit(RouteHelper.New().Action("CreateUserChart", "Chart"), "SF.Chart.getFor('{0}').requestProcessedData()".Formato(prefix)).ToJS(),
                    DivCssClass = ToolBarButton.DefaultQueryCssClass
                });
            }

            if (currentUserChart != null && currentUserChart.IsAllowedFor(TypeAllowedBasic.Modify, ExecutionContext.UserInterface))
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
            var buttons = new List<ToolBarButton> 
            {
                new ToolBarMenu
                {
                    Id = TypeContextUtilities.Compose(prefix, "tmUserCharts"),
                    AltText = ucUserChartText,
                    Text = ucUserChartText,
                    DivCssClass = ToolBarButton.DefaultQueryCssClass,
                    Items = items
                }
            };

            if (ReportsClient.ToExcelPlain)
            {
                string ucExportDataText = Resources.UserChart_ExportData;
                buttons.Add(new ToolBarButton
                {
                    Id = TypeContextUtilities.Compose(prefix, "qbUserChartExportData"),
                    AltText = ucExportDataText,
                    Text = ucExportDataText,
                    OnClick = "SF.Chart.getFor('{0}').exportData('{1}', '{2}')".Formato(prefix, RouteHelper.New().Action("Validate", "Chart"), RouteHelper.New().Action("ExportData", "Chart")),
                    DivCssClass = ToolBarButton.DefaultQueryCssClass
                });
            }

            return buttons;
        }

        public static object DataJson(ChartRequest request, ResultTable resultTable)
        {
            var cols = request.Columns.Select((c,i)=>new 
            { 
                name = "column" + i,
                title = c.GetTitle(), 
                converter = c.Converter(i)
            }).ToList();

            if (request.GroupResults)
            {
                cols.Insert(0, new
                {
                    name = "entity",
                    title = "",
                    converter = new Func<ResultRow, object>(r => r.Entity.Key())
                });
            }

            return new
            {
                labels = cols.ToDictionary(a => a.name, a => a.title),
                rows = resultTable.Rows.Select(r => cols.ToDictionary(a => a.name, a => a.converter(r))).ToList()
            };
        }

        private static Func<ResultRow,object> Converter(this ChartColumnDN ct, int columnIndex)
        {
            if (ct == null)
                return null;

            var type = ct.Token.Type.UnNullify();

            if (typeof(Lite).IsAssignableFrom(type))
            {
                return r =>
                {
                    Lite l = (Lite)r[columnIndex];
                    return new
                    {
                        key = l.TryCC(li => li.Key()),
                        toStr = l.TryCC(li => li.ToString()),
                        color = l == null ? "#555" : ChartColorLogic.ColorFor(l).TryToHtml(),
                    };
                };
            }
            else if (type.IsEnum)
            {
                var dic = ChartColorLogic.Colors.Value.TryGetC(EnumProxy.Generate(type));

                return r =>
                {
                    Enum e = (Enum)r[columnIndex];
                    return new
                    {
                        key = e.TryToString(),
                        toStr = e.TryCC(en => en.NiceToString()),
                        color = e == null ? "#555" : dic.TryGetS(Convert.ToInt32(e)).TryToHtml(),
                    };
                };
            }
            else if (typeof(DateTime) == type)
            {
                return r =>
                {
                    DateTime? e = (DateTime?)r[columnIndex];
                    if (e != null)
                        e = e.Value.ToUserInterface();
                    return new
                    {
                        key = e.TryToString("s"),
                        toStr = ct.Token.Format.HasText() ? e.TryToString(ct.Token.Format) : r[columnIndex].TryToString()
                    };
                };
            }
            else if (typeof(IFormattable).IsAssignableFrom(type) && ct.Token.Format.HasText())
            {
                return r =>
                {
                    return new
                    {
                        key = r[columnIndex],
                        toStr = ((IFormattable)r[columnIndex]).TryToString(ct.Token.Format)
                    };
                };
            }
            else
                return r => r[columnIndex];
        }

        public static MvcHtmlString ChartTokenCombo(this HtmlHelper helper, QueryTokenDN chartToken, IChartBase chart, object queryName, Context context)
        {
            QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryName);

            var tokenPath = chartToken.Token.FollowC(qt => qt.Parent).Reverse().NotNull().ToList();

            QueryToken queryToken = chartToken.Token;
            if (tokenPath.Count > 0)
                queryToken = tokenPath[0];

            HtmlStringBuilder sb = new HtmlStringBuilder();

            bool canAggregate = (chartToken as ChartColumnDN).TryCS(ct => ct.ShouldAggregate) ?? true;

            var rootTokens = ChartUtils.SubTokensChart(null, qd.Columns, chart.GroupResults && canAggregate);

            sb.AddLine(SearchControlHelper.TokenOptionsCombo(
                helper, qd.QueryName, SearchControlHelper.TokensCombo(rootTokens, queryToken), context, 0, false));
            
            for (int i = 0; i < tokenPath.Count; i++)
            {
                QueryToken t = tokenPath[i];
                List<QueryToken> subtokens = t.SubTokensChart(qd.Columns, canAggregate);
                if (!subtokens.IsEmpty())
                {
                    bool moreTokens = i + 1 < tokenPath.Count;
                    sb.AddLine(SearchControlHelper.TokenOptionsCombo(
                        helper, queryName, SearchControlHelper.TokensCombo(subtokens, moreTokens ? tokenPath[i + 1] : null), context, i + 1, !moreTokens));
                }
            }
            
            return sb.ToHtml();
        }

        public static MvcHtmlString ChartRootTokens(this HtmlHelper helper, IChartBase chart, QueryDescription qd, Context context)
        {
            var subtokens = ChartUtils.SubTokensChart(null, qd.Columns, chart.GroupResults);

            return SearchControlHelper.TokenOptionsCombo(
                helper, qd.QueryName, SearchControlHelper.TokensCombo(subtokens, null), context, 0, false);
        }

        public static string ToJS(this ChartRequest request)
        {
            return new JsOptionsBuilder(true)
            {
                { "webQueryName", request.QueryName.TryCC(q => Navigator.ResolveWebQueryName(q).SingleQuote()) },
                { "orders", request.Orders.IsEmpty() ? null : ("[" + request.Orders.ToString(oo => oo.ToJS().SingleQuote(), ",") + "]") }
            }.ToJS();
        }

        public static string ToJS(this Order order)
        {
            return (order.OrderType == OrderType.Descending ? "-" : "") + order.Token.FullKey();
        }

        public static string ToJS(this UserChartDN userChart)
        {
            return new JsOptionsBuilder(true)
            {
                { "webQueryName", userChart.QueryName.TryCC(q => Navigator.ResolveWebQueryName(q).SingleQuote()) },
                { "orders", userChart.Orders.IsEmpty() ? null : ("[" + userChart.Orders.ToString(oo => oo.ToJS().SingleQuote(), ",") + "]") }
            }.ToJS();
        }

        public static string ToJS(this QueryOrderDN order)
        {
            return (order.OrderType == OrderType.Descending ? "-" : "") + order.Token.FullKey();
        }
    }
}
