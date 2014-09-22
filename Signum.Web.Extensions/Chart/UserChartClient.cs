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
using Signum.Web.Basic;
using Signum.Web.Operations;
using Signum.Web.Extensions.UserQueries;
using Signum.Web.Excel;
using Signum.Web.UserAssets;
using Signum.Entities.UserAssets;

namespace Signum.Web.Chart
{
    public static class UserChartClient
    {
        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                QueryClient.Start();

                UserAssetsClient.Start();
                UserAssetsClient.RegisterExportAssertLink<UserChartDN>();

                Func<SubTokensOptions, Mapping<QueryTokenDN>> qtMapping = ops=>ctx =>
                {
                    string tokenStr = UserAssetsHelper.GetTokenString(ctx);

                    string queryKey = ctx.Parent.Parent.Parent.Inputs[TypeContextUtilities.Compose("Query", "Key")];
                    object queryName = QueryLogic.ToQueryName(queryKey);

                    var chart = ((UserChartDN)ctx.Parent.Parent.Parent.UntypedValue);

                    QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryName);
                    return new QueryTokenDN(QueryUtils.Parse(tokenStr, qd, ops | (chart.GroupResults ? SubTokensOptions.CanAggregate : 0)));
                };

                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<UserChartDN> { PartialViewName = _ => ChartClient.ViewPrefix.Formato("UserChart") }
                });

                Navigator.EntitySettings<UserChartDN>().MappingMain = Navigator.EntitySettings<UserChartDN>().MappingLine = 
                    new EntityMapping<UserChartDN>(true)
                        .SetProperty(cb => cb.Columns, new ChartClient.MListCorrelatedOrDefaultMapping<ChartColumnDN>(ChartClient.MappingChartColumn))
                        .SetProperty(cr => cr.Filters, new MListMapping<QueryFilterDN>
                        {
                            ElementMapping = new EntityMapping<QueryFilterDN>(false)
                                .CreateProperty(a => a.Operation)
                                .CreateProperty(a => a.ValueString)
                                .SetProperty(a => a.Token, qtMapping(SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement))
                        })
                        .SetProperty(cr => cr.Orders, new MListMapping<QueryOrderDN>
                        {
                            ElementMapping = new EntityMapping<QueryOrderDN>(false)
                                .CreateProperty(a => a.OrderType)
                                .SetProperty(a => a.Token, qtMapping(SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement))
                        }); 

                RouteTable.Routes.MapRoute(null, "UC/{webQueryName}/{lite}",
                     new { controller = "Chart", action = "ViewUserChart" });

                UserChartDN.SetConverters(query => QueryLogic.ToQueryName(query.Key), queryName =>
                    QueryLogic.GetQuery(queryName));

                OperationClient.AddSetting(new EntityOperationSettings<UserChartDN>(UserChartOperation.Delete)
                {
                    Click = ctx => ChartClient.Module["deleteUserChart"](ctx.Options(), Finder.FindRoute(((UserChartDN)ctx.Entity).Query.ToQueryName())),
                    Contextual = { IsVisible = a => true },
                    ContextualFromMany = { IsVisible = a => true },
                });

                LinksClient.RegisterEntityLinks<IdentifiableEntity>((entity, ctrl) =>
                {
                    if (!ChartPermission.ViewCharting.IsAuthorized())
                        return null;

                    return UserChartLogic.GetUserChartsEntity(entity.EntityType)
                        .Select(cp => new UserChartQuickLink(cp, entity)).ToArray();
                });

            }
        }

        class UserChartQuickLink : QuickLink
        {
            Lite<UserChartDN> userChart;
            Lite<IdentifiableEntity> entity;

            public UserChartQuickLink(Lite<UserChartDN> userChart, Lite<IdentifiableEntity> entity)
            {
                this.Text = userChart.ToString();
                this.userChart = userChart;
                this.entity = entity;
                this.IsVisible = true;
                this.Glyphicon = "glyphicon-stats";
                this.GlyphiconColor = "darkviolet";
            }

            public override MvcHtmlString Execute()
            {
                return new HtmlTag("a").Attr("href", RouteHelper.New().Action((ChartController c) => c.ViewUserChart(userChart, entity))).InnerHtml(TextAndIcon());
            }
        }

        public static List<ToolBarButton> GetChartMenu(ControllerContext controllerContext, UrlHelper url, object queryName, Type entityType, string prefix, Lite<UserChartDN> currentUserChart)
        {
            if (!Navigator.IsNavigable(typeof(UserChartDN), null, isSearch: true))
                return new List<ToolBarButton>();

            var buttons = new List<ToolBarButton>();
            buttons.Add(UserCharButton(url, queryName, prefix, currentUserChart));

            if (ExcelClient.ToExcelPlain)
                buttons.Add(ExcelClient.UserChartButton(url, prefix)); 

            return buttons;
        }

        private static ToolBarButton UserCharButton(UrlHelper url, object queryName, string prefix, Lite<UserChartDN> currentUserChart)
        {

            var items = new List<IMenuItem>();

            foreach (var uc in UserChartLogic.GetUserCharts(queryName).OrderBy(a => a.ToString()))
            {
                items.Add(new MenuItem(prefix, "sfUserChart" + uc.Id)
                {
                    Text = uc.ToString(),
                    Title = uc.ToString(),
                    Href = RouteHelper.New().Action<ChartController>(c => c.ViewUserChart(uc, null)),
                    CssClass = (currentUserChart.Is(uc) ? " sf-userchart-selected" : "")
                });
            }

            if (items.Count > 0)
                items.Add(new MenuItemSeparator());

            if (Navigator.IsCreable(typeof(UserChartDN), isSearch: true))
            {
                string uqNewText = ChartMessage.CreateNew.NiceToString();
                items.Add(new MenuItem(prefix, "qbUserChartNew")
                {
                    Title = uqNewText,
                    Text = uqNewText,
                    OnClick = ChartClient.Module["createUserChart"](prefix, url.Action((ChartController c) => c.CreateUserChart())),
                });
            }

            if (currentUserChart != null && currentUserChart.IsAllowedFor(TypeAllowedBasic.Modify, inUserInterface: true))
            {
                string ucEditText = ChartMessage.EditUserChart.NiceToString();
                items.Add(new MenuItem(prefix, "qbUserChartEdit")
                {
                    Title = ucEditText,
                    Text = ucEditText,
                    Href = Navigator.NavigateRoute(currentUserChart)
                });
            }

            string ucUserChartText = typeof(UserChartDN).NicePluralName();

            return new ToolBarDropDown(prefix,"tmUserCharts")
            {
                Title = ucUserChartText,
                Text = ucUserChartText,
                Items = items
            };
        }
    }
}