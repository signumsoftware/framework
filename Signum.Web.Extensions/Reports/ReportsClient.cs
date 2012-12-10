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
using Signum.Entities.UserQueries;
#endregion

namespace Signum.Web.Reports
{
    public class ReportsClient
    {
        public static bool ToExcelPlain { get; private set; }
        static bool ExcelReport;

        public static void Start(bool toExcelPlain, bool excelReport)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                ToExcelPlain = toExcelPlain;
                ExcelReport = excelReport;

                Navigator.RegisterArea(typeof(ReportsClient));

                if (excelReport)
                {
                    string viewPrefix = "~/Reports/Views/{0}.cshtml";
                    Navigator.AddSettings(new List<EntitySettings>{
                        new EntitySettings<ExcelReportDN>(EntityType.Main) 
                        { 
                            PartialViewName = _ => viewPrefix.Formato("ExcelReport"),
                            MappingMain = new ExcelReportMapping()
                        }
                    });

                    FilesClient.Start(false, false, true);

                    if (!Navigator.Manager.EntitySettings.ContainsKey(typeof(QueryDN)))
                        Navigator.Manager.EntitySettings.Add(typeof(QueryDN), new EntitySettings<QueryDN>(EntityType.SystemString));

                    ButtonBarEntityHelper.RegisterEntityButtons<ExcelReportDN>((ctx, entity) =>
                    {
                        var buttons = new List<ToolBarButton>
                        {
                            new ToolBarButton 
                            { 
                                Id = TypeContextUtilities.Compose(ctx.Prefix, "ebReportSave"),
                                Text = Signum.Web.Properties.Resources.Save, 
                                OnClick = Js.Submit(RouteHelper.New().Action("Save", "Report")).ToJS()
                            }
                        };

                        if (!entity.IsNew)
                        {
                            buttons.Add(new ToolBarButton
                            {
                                Id = TypeContextUtilities.Compose(ctx.Prefix, "ebReportDelete"),
                                Text = Resources.Delete,
                                OnClick = Js.Confirm(Resources.AreYouSureOfDeletingReport0.Formato(entity.DisplayName),
                                                    Js.AjaxCall(RouteHelper.New().Action("Delete", "Report"), "{{excelReport:{0}}}".Formato(entity.Id), null)).ToJS(),
                            });

                            buttons.Add(new ToolBarButton
                            {
                                Id = TypeContextUtilities.Compose(ctx.Prefix, "ebReportDownload"),
                                Text = Resources.Download,
                                OnClick = "window.open('" + RouteHelper.New().Action("DownloadTemplate", "Report", new { excelReport = entity.Id } ) + "');",
                            });
                        }

                        return buttons.ToArray();
                    });
                }

                if (toExcelPlain || excelReport)
                    ButtonBarQueryHelper.RegisterGlobalButtons(ButtonBarQueryHelper_GetButtonBarForQueryName); 
            }
        }

        public class ExcelReportMapping : EntityMapping<ExcelReportDN>
        {
            public ExcelReportMapping() : base(true) { }

            public override ExcelReportDN GetEntity(MappingContext<ExcelReportDN> ctx)
            {
                RuntimeInfo runtimeInfo = ctx.GetRuntimeInfo();
                if (runtimeInfo.IsNew)
                {
                    var result = new ExcelReportDN();

                    string queryKey = ctx.Inputs[TypeContextUtilities.Compose("Query", "Key")];
                    object queryName = Navigator.Manager.QuerySettings.Keys.FirstEx(key => QueryUtils.GetQueryUniqueKey(key) == queryKey);

                    result.Query = QueryLogic.RetrieveOrGenerateQuery(queryName);

                    return result;
                }
                else
                    return Database.Retrieve<ExcelReportDN>(runtimeInfo.IdOrNull.Value);
            }
        }

        static ToolBarButton[] ButtonBarQueryHelper_GetButtonBarForQueryName(QueryButtonContext ctx)
        {
            if (ctx.Prefix.HasText())
                return null;

            Lite<UserQueryDN> currentUserQuery = null;
            string url = (ctx.ControllerContext.RouteData.Route as Route).TryCC(r => r.Url);
            if (url.HasText() && url.Contains("UQ"))
                currentUserQuery = Lite.Create<UserQueryDN>(int.Parse(ctx.ControllerContext.RouteData.Values["lite"].ToString()));

            ToolBarButton plain = new ToolBarButton
            {
                Id = TypeContextUtilities.Compose(ctx.Prefix, "qbToExcelPlain"),
                AltText = Resources.ExcelReport,
                Text = Resources.ExcelReport,
                OnClick = Js.SubmitOnly(RouteHelper.New().Action("ToExcelPlain", "Report"), "$.extend({{userQuery:'{0}'}}, SF.FindNavigator.getFor('{1}').requestDataForSearch())".Formato((currentUserQuery != null ? currentUserQuery.IdOrNull : null), ctx.Prefix)).ToJS(),
                DivCssClass = ToolBarButton.DefaultQueryCssClass
            };

            if (ExcelReport) 
            {
                var items = new List<ToolBarButton>();
                
                if (ToExcelPlain)
                    items.Add(plain);

                List<Lite<ExcelReportDN>> reports = ReportsLogic.GetExcelReports(ctx.QueryName);

                if (reports.Count > 0)
                {
                    if (items.Count > 0)
                        items.Add(new ToolBarSeparator());

                    foreach (Lite<ExcelReportDN> report in reports)
                    {
                        items.Add(new ToolBarButton
                        {
                            AltText = report.ToString(),
                            Text = report.ToString(),
                            OnClick = Js.SubmitOnly(RouteHelper.New().Action("ExcelReport", "Report"), "$.extend({{excelReport:'{0}'}}, SF.FindNavigator.getFor('{1}').requestDataForSearch())".Formato(report.Id, ctx.Prefix)).ToJS(),
                            DivCssClass = ToolBarButton.DefaultQueryCssClass
                        });
                    }
                }

                items.Add(new ToolBarSeparator());

                items.Add(new ToolBarButton
                {
                    Id = TypeContextUtilities.Compose(ctx.Prefix, "qbReportAdminister"),
                    AltText = Resources.ExcelAdminister,
                    Text = Resources.ExcelAdminister,
                    OnClick = Js.SubmitOnly(RouteHelper.New().Action("Administer", "Report"), "{{webQueryName:'{0}'}}".Formato(Navigator.ResolveWebQueryName(ctx.QueryName))).ToJS(),
                    DivCssClass = ToolBarButton.DefaultQueryCssClass
                });

                return new ToolBarButton[]
                {
                    new ToolBarMenu
                    { 
                        Id = TypeContextUtilities.Compose(ctx.Prefix, "tmExcel"),
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
