#region usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Web.Mvc;
using Signum.Utilities;
using System.Web.UI;
using Signum.Web.Extensions.Properties;
using Signum.Entities.Reports;
using Signum.Entities.Basics;
using Signum.Entities;
using Signum.Engine.Reports;
using System.Web.Routing;
using Signum.Web.Files;
#endregion

namespace Signum.Web.Reports
{
    public class ReportClient
    {
        static bool ToExcelPlain;
        static bool ExcelReport;
        
        public static string ToExcelPlainControllerUrl = "Report/ToExcelPlain";
        public static string ExcelReportControllerUrl = "Report/ExcelReport";

        public static string ViewPrefix = "reports/Views/";

        public static void Start(bool toExcelPlain, bool excelReport)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                ToExcelPlain = toExcelPlain;
                ExcelReport = excelReport;

                AssemblyResourceManager.RegisterAreaResources(
                    new AssemblyResourceStore(typeof(ReportClient), "/reports/", "Signum.Web.Extensions.Reports."));

                RouteTable.Routes.InsertRouteAt0("reports/{resourcesFolder}/{*resourceName}",
                    new { controller = "Resources", action = "Index", area = "reports" },
                    new { resourcesFolder = new InArray(new string[] { "Scripts", "Content", "Images" }) });

                Navigator.AddSettings(new List<EntitySettings>{
                    new EntitySettings<ExcelReportDN>(EntityType.NotSaving) { PartialViewName = _ => ViewPrefix + "ExcelReport" }
                });

                if (excelReport)
                {
                    FilesClient.Start();

                    if (!Navigator.Manager.EntitySettings.ContainsKey(typeof(QueryDN)))
                        Navigator.Manager.EntitySettings.Add(typeof(QueryDN), new EntitySettings<QueryDN>(EntityType.Default));

                    ButtonBarEntityHelper.RegisterEntityButtons<ExcelReportDN>((controllerContext, entity, partialViewName, prefix) =>
                    {
                        var buttons = new List<ToolBarButton>
                        {
                            new ToolBarButton 
                            { 
                                Text = Signum.Web.Properties.Resources.Save, 
                                OnClick = Js.Submit("Report/Save").ToJS()
                            }
                        };

                        if (!entity.IsNew)
                        {
                            buttons.Add(new ToolBarButton
                            {
                                Text = Resources.Delete,
                                OnClick = Js.Confirm(Resources.AreYouSureOfDeletingReport0.Formato(entity.DisplayName),
                                                    Js.AjaxCall("Report/Delete", "{{excelReport:{0}}}".Formato(entity.Id), null)).ToJS(),
                            });

                            buttons.Add(new ToolBarButton
                            {
                                Text = Resources.Download,
                                OnClick = "window.open($('base').attr('href') + \"Report/DownloadTemplate?excelReport=" + entity.Id + "\")",
                            });
                        }

                        return buttons.ToArray();
                    });
                }

                if (toExcelPlain || excelReport)
                    ButtonBarQueryHelper.GetButtonBarForQueryName +=new GetToolBarButtonQueryDelegate(ButtonBarQueryHelper_GetButtonBarForQueryName); 
            }
        }

        static ToolBarButton[] ButtonBarQueryHelper_GetButtonBarForQueryName(ControllerContext controllerContext, object queryName, Type entityType, string prefix)
        {
            int idCurrentUserQuery = 0;
            string url = (controllerContext.RouteData.Route as Route).TryCC(r => r.Url);
            if (url.HasText() && url.Contains("UQ"))
                idCurrentUserQuery = int.Parse(controllerContext.RouteData.Values["id"].ToString());

            ToolBarButton plain = new ToolBarButton
            {
                AltText = Resources.ExcelReport,
                Text = Resources.ExcelReport,
                OnClick = "SubmitOnly('{0}', $.extend({{userQuery:'{1}'}},new FindNavigator({{prefix:'{2}'}}).requestData()));".Formato(ToExcelPlainControllerUrl, (idCurrentUserQuery > 0 ? (int?)idCurrentUserQuery : null), prefix),
                DivCssClass = ToolBarButton.DefaultQueryCssClass
            };

            if (ExcelReport && idCurrentUserQuery == 0) //Excel Reports not allowed for UserQueries yet
            {
                var items = new List<ToolBarButton>();
                
                if (ToExcelPlain)
                    items.Add(plain);

                List<Lite<ExcelReportDN>> reports = ReportsLogic.GetExcelReports(queryName);

                if (reports.Count > 0)
                {
                    if (items.Count > 0)
                        items.Add(new ToolBarSeparator());

                    foreach (Lite<ExcelReportDN> report in reports)
                    {
                        items.Add(new ToolBarButton
                        {
                            AltText = report.ToStr,
                            Text = report.ToStr,
                            OnClick = "SubmitOnly('{0}', $.extend({{excelReport:'{1}'}},new FindNavigator({{prefix:'{2}'}}).requestData()));".Formato(ExcelReportControllerUrl, report.Id, prefix),
                            DivCssClass = ToolBarButton.DefaultQueryCssClass
                        });
                    }
                }

                items.Add(new ToolBarSeparator());

                items.Add(new ToolBarButton
                {
                    AltText = Resources.ExcelAdminister,
                    Text = Resources.ExcelAdminister,
                    OnClick = Js.SubmitOnly("Report/Administer", "{{queryUrlName:'{0}'}}".Formato(Navigator.Manager.QuerySettings[queryName].UrlName)).ToJS(),
                    DivCssClass = ToolBarButton.DefaultQueryCssClass
                });

                return new ToolBarButton[]
                {
                    new ToolBarMenu
                    { 
                        AltText = "Excel", 
                        Text = "Excel",
                        DivCssClass = ToolBarButton.DefaultQueryCssClass,
                        Items = items
                    }
                };
            }
            else
            {
                if (ToExcelPlain)
                    return new ToolBarButton[] { plain };
            }

            return null;
        }
    }
}
