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
using Signum.Web.Basic;
using Signum.Web.Operations;

namespace Signum.Web.Chart
{
    public static class UserChartClient
    {
        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                QueryClient.Start();

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
                    new EntitySettings<UserChartDN>() 
                    { 
                        PartialViewName = _ => ChartClient.ViewPrefix.Formato("UserChart"),
                        MappingMain = new EntityMapping<UserChartDN>(true)
                        
                            .SetProperty(cb=>cb.Columns, new ChartClient.MListCorrelatedOrDefaultMapping<ChartColumnDN>(ChartClient.MappingChartColumn))
                            .SetProperty(cr => cr.Filters, new MListMapping<QueryFilterDN>
                            {
                                ElementMapping = new EntityMapping<QueryFilterDN>(false)
                                    .CreateProperty(a=>a.Operation)
                                    .CreateProperty(a=>a.ValueString)
                                    .SetProperty(a=>a.TryToken, qtMapping)
                            })
                            .SetProperty(cr => cr.Orders, new MListMapping<QueryOrderDN>
                            {
                                ElementMapping = new EntityMapping<QueryOrderDN>(false)
                                    .CreateProperty(a=>a.OrderType)
                                    .SetProperty(a=>a.TryToken, qtMapping)
                            })
                    }
                });

                RouteTable.Routes.MapRoute(null, "UC/{webQueryName}/{lite}",
                     new { controller = "Chart", action = "ViewUserChart" });

                UserChartDN.SetConverters(query => QueryLogic.ToQueryName(query.Key), queryname => 
                    QueryLogic.RetrieveOrGenerateQuery(queryname));

                OperationsClient.AddSetting(new EntityOperationSettings(UserChartOperation.Delete) 
                { 
                    OnClick = ctx => new JsOperationDelete(ctx.Options("DeleteUserChart", "Chart"))
                        .confirmAndAjax(ctx.Entity)
                });
            }
        }

        public static List<ToolBarButton> GetChartMenu(ControllerContext controllerContext, object queryName, Type entityType, string prefix)
        {
            if (!Navigator.IsNavigable(typeof(UserChartDN), isSearchEntity: true))
                return new List<ToolBarButton>();

            var items = new List<ToolBarButton>();

            Lite<UserChartDN> currentUserChart = null;
            string url = (controllerContext.RouteData.Route as Route).TryCC(r => r.Url);
            if (url.HasText() && url.Contains("UC"))
                currentUserChart = Lite.Create<UserChartDN>(int.Parse(controllerContext.RouteData.Values["lite"].ToString()));

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

            if (Navigator.IsCreable(typeof(UserChartDN), isSearchEntity: true))
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
            
            if (currentUserChart != null && currentUserChart.IsAllowedFor(TypeAllowedBasic.Modify, inUserInterface: true))
            {
                string ucEditText = Resources.UserChart_Edit;
                items.Add(new ToolBarButton
                {
                    Id = TypeContextUtilities.Compose(prefix, "qbUserChartEdit"),
                    AltText = ucEditText,
                    Text = ucEditText,
                    Href = Navigator.NavigateRoute(currentUserChart),
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
    }
}