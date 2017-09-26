using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Web.Mvc;
using Signum.Utilities;
using System.Web.UI;
using Signum.Entities.Basics;
using Signum.Entities;
using System.Web.Routing;
using Signum.Web.Files;
using Signum.Engine;
using Signum.Entities.DynamicQuery;
using Signum.Engine.Basics;
using Signum.Entities.Files;
using Signum.Entities.UserQueries;
using Signum.Web.Chart;
using Signum.Entities.Excel;
using Signum.Engine.Excel;
using Signum.Entities.Mailing;
using Signum.Engine.DynamicQuery;

namespace Signum.Web.Excel
{
    public class ExcelClient
    {
        public static string ViewPrefix = "~/Excel/Views/{0}.cshtml";
        public static JsModule Module = new JsModule("Extensions/Signum.Web.Extensions/Excel/Scripts/Excel");

        public static bool ToExcelPlain { get; private set; }
        public static bool ExcelReport { get; private set; }

        public static void Start(bool toExcelPlain, bool excelReport, bool excelAttachment)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                ToExcelPlain = toExcelPlain;
                ExcelReport = excelReport;

                Navigator.RegisterArea(typeof(ExcelClient));

                if (excelReport)
                {
                    if (!Navigator.Manager.EntitySettings.ContainsKey(typeof(FileEmbedded)))
                        throw new InvalidOperationException("Call FileEmbedded first");

                    if (!Navigator.Manager.EntitySettings.ContainsKey(typeof(QueryEntity)))
                        Navigator.Manager.EntitySettings.Add(typeof(QueryEntity), new EntitySettings<QueryEntity>());

                    
                    Navigator.AddSetting(new EntitySettings<ExcelReportEntity> { PartialViewName = _ => ViewPrefix.FormatWith("ExcelReport") });
                        }

                if (toExcelPlain || excelReport)
                    ButtonBarQueryHelper.RegisterGlobalButtons(ButtonBarQueryHelper_GetButtonBarForQueryName); 

                if (excelAttachment)
                {
                    Navigator.AddSetting(new EntitySettings<ExcelAttachmentEntity> { PartialViewName = _ => ViewPrefix.FormatWith("ExcelAttachment") });
                }
            }
        }

        static ToolBarButton[] ButtonBarQueryHelper_GetButtonBarForQueryName(QueryButtonContext ctx)
        {
            if (ctx.Prefix.HasText())
                return null;
            
            if (ExcelReport && DynamicQueryManager.Current.QueryAllowed(typeof(ExcelReportEntity), false))

            {
                var items = new List<IMenuItem>();
                
                if (ToExcelPlain)
                    items.Add(PlainExcel(ctx).ToMenuItem());

                List<Lite<ExcelReportEntity>> reports = ExcelLogic.GetExcelReports(ctx.QueryName);

                if (reports.Count > 0)
                {
                    if (items.Count > 0)
                        items.Add(new MenuItemSeparator());

                    foreach (Lite<ExcelReportEntity> report in reports)
                    {
                        items.Add(new MenuItem(ctx.Prefix, "sfExcelReport" + report.Id)
                        {
                            Title = report.ToString(),
                            Text = report.ToString(),
                            OnClick = Module["toExcelReport"](ctx.Prefix, ctx.Url.Action("ExcelReport", "Excel"), report.Key()),
                        });
                    }
                }

                items.Add(new MenuItemSeparator());

                var current =  QueryLogic.GetQueryEntity(ctx.QueryName).ToLite().Key();

                items.Add(new MenuItem(ctx.Prefix, "qbReportAdminister")
                {
                    Title = ExcelMessage.Administer.NiceToString(),
                    Text = ExcelMessage.Administer.NiceToString(),
                    OnClick = Module["administerExcelReports"](ctx.Prefix, Finder.ResolveWebQueryName(typeof(ExcelReportEntity)),current),
                });

                items.Add(new MenuItem(ctx.Prefix, "qbReportCreate")
                {
                    Title = ExcelMessage.CreateNew.NiceToString(),
                    Text = ExcelMessage.CreateNew.NiceToString(),
                    OnClick = Module["createExcelReports"](ctx.Prefix, ctx.Url.Action("Create", "Excel"), current),
                });

                return new ToolBarButton[]
                {
                    new ToolBarDropDown(ctx.Prefix, "tmExcel")
                    { 
                        Title = "Excel", 
                        Text = "Excel",
                        Items = items
                    }
                };
            }
            else
            {
                if (ToExcelPlain)
                    return new ToolBarButton[] { PlainExcel(ctx) };
            }

            return null;
        }

        private static ToolBarButton PlainExcel(QueryButtonContext ctx)
        {
            return new ToolBarButton(ctx.Prefix, "sfToExcelPlain")
            {
                Title = ExcelMessage.ExcelReport.NiceToString(),
                Text = ExcelMessage.ExcelReport.NiceToString(),
                OnClick = Module["toPlainExcel"](ctx.Prefix, ctx.Url.Action("ToExcelPlain", "Excel"))
            };
        }

        internal static ToolBarButton UserChartButton(UrlHelper url, string prefix)
        {
            return new ToolBarButton(prefix, "qbUserChartExportData")
            {
                Title = ExcelMessage.ExcelReport.NiceToString(),
                Text = ExcelMessage.ExcelReport.NiceToString(),
                OnClick = ChartClient.Module["exportData"](prefix,
                    url.Action((ChartController c) => c.Validate()),
                    url.Action((ChartController c) => c.ExportData())),
            };
        }
    }
}
