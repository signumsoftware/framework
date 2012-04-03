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
            
                    var chart = ((UserChartDN)ctx.Parent.Parent.Parent.UntypedValue).Chart;

                    QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryName);
                    return QueryUtils.Parse(tokenStr, qt => chart.SubTokensChart(qt, qd.Columns, true));
                };

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
                    return ctx.None();

                var qd = DynamicQueryManager.Current.QueryDescription(
                    Navigator.ResolveQueryName(ctx.GlobalInputs[TypeContextUtilities.Compose(ctx.Root.ControlID, ViewDataKeys.QueryName)]));

                var chartToken = (ChartTokenDN)ctx.Parent.UntypedValue;
                var chart = (ChartBase)ctx.Parent.Parent.UntypedValue;

                return QueryUtils.Parse(tokenName, qt => chart.SubTokensChart(qt, qd.Columns, true));
            })
            .SetProperty(ct => ct.DisplayName, ctx =>
            {
                if (string.IsNullOrEmpty(ctx.Input))
                    return ctx.None();

                return ctx.Input;
            });

        public static EntityMapping<ChartBase> MappingChartBase = new EntityMapping<ChartBase>(true)
            .SetProperty(cb => cb.Dimension1, ctx => { if (ctx.Value == null) return ctx.None(); else return mappingChartToken.GetValue(ctx); })
            .SetProperty(cb => cb.Dimension2, ctx => { if (ctx.Value == null) return ctx.None(); else return mappingChartToken.GetValue(ctx); })
            .SetProperty(cb => cb.Value1, ctx => { if (ctx.Value == null) return ctx.None(); else return mappingChartToken.GetValue(ctx); })
            .SetProperty(cb => cb.Value2, ctx => { if (ctx.Value == null) return ctx.None(); else return mappingChartToken.GetValue(ctx); });

        public static EntityMapping<ChartRequest> MappingChartRequest = new EntityMapping<ChartRequest>(true)
            .SetProperty(cr => cr.Filters, ctx => ExtractChartFilters(ctx))
            .SetProperty(cr => cr.Orders, ctx => ExtractChartOrders(ctx))
            .SetProperty(cr => cr.Chart, MappingChartBase);
                 
        static List<Entities.DynamicQuery.Filter> ExtractChartFilters(MappingContext<List<Entities.DynamicQuery.Filter>> ctx)
            {
            var qd = DynamicQueryManager.Current.QueryDescription(
                Navigator.ResolveQueryName(ctx.GlobalInputs[TypeContextUtilities.Compose(ctx.Root.ControlID, ViewDataKeys.QueryName)]));

            ChartRequest chartRequest = (ChartRequest)ctx.Parent.UntypedValue;

            return FindOptionsModelBinder.ExtractFilterOptions(ctx.ControllerContext.HttpContext, qt => chartRequest.Chart.SubTokensFilters(qt, qd.Columns)).Select(fo => fo.ToFilter()).ToList();
        }

        static List<Order> ExtractChartOrders(MappingContext<List<Order>> ctx)
        {
            var qd = DynamicQueryManager.Current.QueryDescription(
                Navigator.ResolveQueryName(ctx.GlobalInputs[TypeContextUtilities.Compose(ctx.Root.ControlID, ViewDataKeys.QueryName)]));

            ChartRequest chartRequest = (ChartRequest)ctx.Parent.UntypedValue;
            
            return FindOptionsModelBinder.ExtractOrderOptions(ctx.ControllerContext.HttpContext, qt => chartRequest.Chart.SubTokensFilters(qt, qd.Columns)).Select(fo => fo.ToOrder()).ToList();
        }

        static ToolBarButton[] ButtonBarQueryHelper_GetButtonBarForQueryName(QueryButtonContext ctx)
        {
            if (!ChartPermissions.ViewCharting.IsAuthorized())
                return null;

            string chartNewText = Resources.Chart_Chart;

            return new[]
            {
                new ToolBarButton
                {
                    Id = TypeContextUtilities.Compose(ctx.Prefix, "qbChartNew"),
                    AltText = chartNewText,
                    Text = chartNewText,
                    OnClick = Js.SubmitOnly(RouteHelper.New().Action("Index", "Chart"), new JsFindNavigator(ctx.Prefix).requestData()).ToJS(),
                    DivCssClass = ToolBarButton.DefaultQueryCssClass
                }
            };
        }

        public static List<ToolBarButton> GetChartMenu(ControllerContext controllerContext, object queryName, Type entityType, string prefix)
        {
            var allowed = TypeAuthLogic.GetAllowed(typeof(UserChartDN)).Max().GetUI();
            if (allowed < TypeAllowedBasic.Read)
                return null;
            
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

            if (allowed == TypeAllowedBasic.Create)
            {
                string uqNewText = Resources.UserChart_CreateNew;
                items.Add(new ToolBarButton
                {
                    Id = TypeContextUtilities.Compose(prefix, "qbUserChartNew"),
                    AltText = uqNewText,
                    Text = uqNewText,
                    OnClick = Js.Submit(RouteHelper.New().Action("CreateUserChart", "Chart"), "SF.Chart.Builder.requestProcessedData({0})".Formato(prefix)).ToJS(),
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
                    OnClick = "SF.Chart.Builder.exportData('{0}', '{1}', '{2}')".Formato(prefix, RouteHelper.New().Action("Validate", "Chart"), RouteHelper.New().Action("ExportData", "Chart")),
                    DivCssClass = ToolBarButton.DefaultQueryCssClass
                });
            }

            return buttons;
        }

        public static object DataJson(ChartRequest request, ResultTable resultTable)
        {
            var chart = request.Chart;
            

            switch (chart.ChartResultType)
            {
                case ChartResultType.TypeValue:
                    {
                        var d1Converter = chart.Dimension1.Converter(true);
                        var v1Converter = chart.Value1.Converter(false);
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
                    }
                case ChartResultType.TypeTypeValue:
                    {
                        var d1Converter = chart.Dimension1.Converter(false);
                        var d2Converter = chart.Dimension2.Converter(true);
                        var v1Converter = chart.Value1.Converter(false);

                        object NullValue = "- None -";
                        List<object> dimension1Values = resultTable.Rows.Select(r => r[0]).Distinct().ToList();
                        Dictionary<object, Dictionary<object, object>> dic1dic0 = resultTable.Rows.AgGroupToDictionary(r => r[1] ?? NullValue, gr => gr.ToDictionary(r => r[0] ?? NullValue, r => r[2]));

                        return new
                        {
                            labels = new
                            {
                                dimension1 = chart.Dimension1.GetTitle(),
                                dimension2 = chart.Dimension2.GetTitle(),
                                value1 = chart.Value1.GetTitle()
                            },
                            dimension1 = dimension1Values.Select(d1Converter).ToList(),
                            series = dic1dic0.Select(kvp => new
                            {
                                dimension2 = d2Converter(kvp.Key == NullValue ? null : kvp.Key),
                                values = dimension1Values.Select(dim1 => kvp.Value.TryGetC(dim1 ?? NullValue)).ToList(),
                            }).ToList()
                        };
                    }
                case ChartResultType.Points:
                    {
                        var d1Converter = chart.Dimension1.Converter(false);
                        var d2Converter = chart.Dimension2.Converter(false);
                        var v1Converter = chart.Value1.Converter(true);

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
                    }

                case ChartResultType.Bubbles:
                    {
                        var d1Converter = chart.Dimension1.Converter(false);
                        var d2Converter = chart.Dimension2.Converter(false);
                        var v1Converter = chart.Value1.Converter(true);
                        var v2Converter = chart.Value2.Converter(false);

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
                    }
                default:
                    throw new NotImplementedException("");
            }
        }

        private static Func<object,object> Converter(this ChartTokenDN ct, bool color)
        {
            if (ct == null)
                return null;

            var type = ct.Token.Type.UnNullify();

            if (typeof(Lite).IsAssignableFrom(type))
            {
                if(color)
                    return p =>
                    {
                        Lite l = (Lite)p;
                        return new
                        {
                            key = l.TryCC(li => li.Key()),
                            toStr = l.TryCC(li => li.ToString()),
                            color = l == null ? "#555" : ChartColorLogic.ColorFor(l).TryToHtml(),
                        };
                    };
                else
                    return p =>
                    {
                        Lite l = (Lite)p;
                        return new
                        {
                            key = l.TryCC(li => li.Key()),
                            toStr = l.TryCC(li => li.ToString())
                        };
                    };
            }
            else if (type.IsEnum)
            {
                var dic = ChartColorLogic.Colors.Value.TryGetC(Reflector.GenerateEnumProxy(type));

                if (color)
                    return p =>
                    {
                        Enum e = (Enum)p;
                        return new
                        {
                            key = e.TryToString(),
                            toStr = e.TryCC(en => en.NiceToString()),
                            color = e == null ? "#555" : dic.TryGetS(Convert.ToInt32(e)).TryToHtml(),
                        };
                    };
                else
                    return p =>
                    {
                        Enum e = (Enum)p;
                        return new
                        {
                            key = e.TryToString(),
                            toStr = e.TryCC(en => en.NiceToString())
                        };
                    };
            }
            else if (typeof(DateTime) == type)
            {
                return p =>
                {
                    DateTime? e = (DateTime?)p;
                    if (e != null)
                        e = e.Value.ToUserInterface();
                    return new
                    {
                        key = e.TryToString("s"),
                        toStr = ct.Token.Format.HasText() ? e.TryToString(ct.Token.Format) : p.TryToString()
                    };
                };
            }
            else if (typeof(IFormattable).IsAssignableFrom(type) && ct.Token.Format.HasText())
            {
                return p =>
                {
                    return new
                    {
                        key = p,
                        toStr = ((IFormattable)p).TryToString(ct.Token.Format)
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

        public static MvcHtmlString ChartTokenCombo(this HtmlHelper helper, QueryTokenDN chartToken, ChartBase chart, object queryName, Context context)
        {
            QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryName);

            var tokenPath = chartToken.Token.FollowC(qt => qt.Parent).Reverse().NotNull().ToList();

            QueryToken queryToken = chartToken.Token;
            if (tokenPath.Count > 0)
                queryToken = tokenPath[0];

            HtmlStringBuilder sb = new HtmlStringBuilder();

            bool canAggregate = (chartToken as ChartTokenDN).TryCS(ct => ct.ShouldAggregate) ?? true;

            var rootTokens = chart.SubTokensChart(null, qd.Columns, canAggregate);

            sb.AddLine(SearchControlHelper.TokenOptionsCombo(
                helper, qd.QueryName, SearchControlHelper.TokensCombo(rootTokens, queryToken), context, 0, false));
            
            for (int i = 0; i < tokenPath.Count; i++)
            {
                QueryToken t = tokenPath[i];
                List<QueryToken> subtokens = chart.SubTokensChart(t, qd.Columns, canAggregate);
                if (!subtokens.IsEmpty())
                {
                    bool moreTokens = i + 1 < tokenPath.Count;
                    sb.AddLine(SearchControlHelper.TokenOptionsCombo(
                        helper, queryName, SearchControlHelper.TokensCombo(subtokens, moreTokens ? tokenPath[i + 1] : null), context, i + 1, !moreTokens));
                }
            }
            
            return sb.ToHtml();
        }

        public static MvcHtmlString ChartRootTokens(this HtmlHelper helper, ChartBase chart, QueryDescription qd, Context context)
        {
            var subtokens = chart.SubTokensChart(null, qd.Columns, true);

            return SearchControlHelper.TokenOptionsCombo(
                helper, qd.QueryName, SearchControlHelper.TokensCombo(subtokens, null), context, 0, false);
        }
    }
}
