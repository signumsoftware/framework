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
using Signum.Engine;
using Signum.Entities.DynamicQuery;
using Signum.Engine.Basics;
using Signum.Entities.Files;
#endregion

namespace Signum.Web.Reports
{
    public class ReportsClient
    {
        static bool ToExcelPlain;
        static bool ExcelReport;

        public static void Start(bool toExcelPlain, bool excelReport)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                ToExcelPlain = toExcelPlain;
                ExcelReport = excelReport;

                Navigator.RegisterArea(typeof(ReportsClient));

                Navigator.AddSettings(new List<EntitySettings>{
                    new EntitySettings<ExcelReportDN>(EntityType.NotSaving) 
                    { 
                        PartialViewName = _ => RouteHelper.AreaView("ExcelReport", "Reports"),
                        MappingAdmin = new EntityMapping<ExcelReportDN>(true)  
                        { 
                            GetEntity = ctx => 
                            {
                                RuntimeInfo runtimeInfo = ctx.GetRuntimeInfo();
                                if (runtimeInfo.IsNew)
                                {
                                    ctx.Value = new ExcelReportDN();
                                    
                                    string queryKey = ctx.Inputs[TypeContextUtilities.Compose("Query", "Key")];
                                    object queryName = Navigator.Manager.QuerySettings.Keys.First(key => QueryUtils.GetQueryUniqueKey(key) == queryKey);
                               
                                    ctx.Value.Query = QueryLogic.RetrieveOrGenerateQuery(queryName);
                                }
                                else
                                    ctx.Value = Database.Retrieve<ExcelReportDN>(runtimeInfo.IdOrNull.Value);

                                return ctx.Value;
                            }
                        }
                    }
                });

                if (excelReport)
                {
                    FilesClient.Start(false, true);

                    if (!Navigator.Manager.EntitySettings.ContainsKey(typeof(QueryDN)))
                        Navigator.Manager.EntitySettings.Add(typeof(QueryDN), new EntitySettings<QueryDN>(EntityType.Default));

                    ButtonBarEntityHelper.RegisterEntityButtons<ExcelReportDN>((controllerContext, entity, partialViewName, prefix) =>
                    {
                        var buttons = new List<ToolBarButton>
                        {
                            new ToolBarButton 
                            { 
                                Text = Signum.Web.Properties.Resources.Save, 
                                OnClick = Js.Submit(RouteHelper.New().Action("Save", "Report")).ToJS()
                            }
                        };

                        if (!entity.IsNew)
                        {
                            buttons.Add(new ToolBarButton
                            {
                                Text = Resources.Delete,
                                OnClick = Js.Confirm(Resources.AreYouSureOfDeletingReport0.Formato(entity.DisplayName),
                                                    Js.AjaxCall(RouteHelper.New().Action("Delete", "Report"), "{{excelReport:{0}}}".Formato(entity.Id), null)).ToJS(),
                            });

                            buttons.Add(new ToolBarButton
                            {
                                Text = Resources.Download,
                                OnClick = "window.open('" + RouteHelper.New().Action("DownloadTemplate", "Report", new { excelReport = entity.Id } ) + "');",
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
                OnClick = "SubmitOnly('{0}', $.extend({{userQuery:'{1}'}},new FindNavigator({{prefix:'{2}'}}).requestDataForSearch()));".Formato(RouteHelper.New().Action("ToExcelPlain", "Report"), (idCurrentUserQuery > 0 ? (int?)idCurrentUserQuery : null), prefix),
                DivCssClass = ToolBarButton.DefaultQueryCssClass
            };

            if (ExcelReport) 
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
                            OnClick = "SubmitOnly('{0}', $.extend({{excelReport:'{1}'}},new FindNavigator({{prefix:'{2}'}}).requestDataForSearch()));".Formato(RouteHelper.New().Action("ExcelReport", "Report"), report.Id, prefix),
                            DivCssClass = ToolBarButton.DefaultQueryCssClass
                        });
                    }
                }

                items.Add(new ToolBarSeparator());

                items.Add(new ToolBarButton
                {
                    AltText = Resources.ExcelAdminister,
                    Text = Resources.ExcelAdminister,
                    OnClick = Js.SubmitOnly(RouteHelper.New().Action("Administer", "Report"), "{{webQueryName:'{0}'}}".Formato(Navigator.ResolveWebQueryName(queryName))).ToJS(),
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
