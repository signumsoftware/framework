#region usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using System.Web.Mvc;
using Signum.Utilities;
using Signum.Engine;
using Signum.Entities;
using Signum.Engine.Maps;
using Signum.Engine.DynamicQuery;
using Signum.Entities.Reflection;
using Signum.Entities.DynamicQuery;
using System.Reflection;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using Signum.Entities.Reports;
using Signum.Entities.Basics;
using Signum.Engine.Reports;
using Signum.Engine.Basics;
#endregion

namespace Signum.Web.Reports
{
    public class ReportController : Controller
    {
        [HttpPost]
        public ActionResult ToExcelPlain(QueryRequest request, string prefix)
        {
            if (!Navigator.IsFindable(request.QueryName))
                throw new UnauthorizedAccessException(NormalControlMessage.ViewForType0IsNotAllowed.NiceToString().Formato(request.QueryName));

            ResultTable queryResult = DynamicQueryManager.Current.ExecuteQuery(request);
            byte[] binaryFile = PlainExcelGenerator.WritePlainExcel(queryResult);

            return File(binaryFile, MimeType.FromExtension(".xlsx"), Navigator.ResolveWebQueryName(request.QueryName) + ".xlsx");
        }

        [HttpPost]
        public ActionResult ExcelReport(QueryRequest request, Lite<ExcelReportDN> excelReport, string prefix)
        {
            if (!Navigator.IsFindable(request.QueryName))
                throw new UnauthorizedAccessException(NormalControlMessage.ViewForType0IsNotAllowed.NiceToString().Formato(request.QueryName));

            byte[] file = ReportSpreadsheetsLogic.ExecuteExcelReport(excelReport, request);

            return File(file, MimeType.FromExtension(".xlsx"), Navigator.ResolveWebQueryName(request.QueryName) + "-" + TimeZoneManager.Now.ToString("yyyyMMdd-HHmmss") + ".xlsx");
            //Known Bug in IE: When the file dialog is shown, if Open is chosen the Excel will be broken as a result of IE automatically adding [1] to the name. 
            //There's not workaround for this, so either click on Save instead of Open, or use Firefox or Chrome
        }

        [HttpPost]
        public ViewResult Administer(string webQueryName)
        {
            object queryName = Navigator.ResolveQueryName(webQueryName);

            QueryDN query = QueryLogic.GetQuery(queryName);

            if (query.IsNew) //If the Query is new there won't be any reports associated => navigate directly to create one
            {
                return Navigator.NormalPage(this, new ExcelReportDN { Query = query });
            }
            else
            {
                FindOptions fo = new FindOptions(typeof(ExcelReportDN))
                {
                    FilterMode = FilterMode.AlwaysHidden,
                    SearchOnLoad = true,
                    FilterOptions = new List<FilterOption> 
                    { 
                        new FilterOption("Query", query.ToLite())
                    },
                    Creating = Js.SubmitOnly(RouteHelper.New().Action("Create", "Report"), "{{query:{0}}}".Formato(query.Id)).ToJS()
                };

                return Navigator.Find(this, fo);
            }
        }

        [HttpPost]
        public ViewResult Create(Lite<QueryDN> query)
        {
            ExcelReportDN report = new ExcelReportDN { Query = query.Retrieve() };
            return Navigator.NormalPage(this, report);
        }

        public ActionResult DownloadTemplate(Lite<ExcelReportDN> excelReport)
        {
            ExcelReportDN report = excelReport.RetrieveAndForget();

            //HttpContext.Response.AddHeader("content-disposition", "attachment; filename=" + Path.GetFileName(report.File.FileName));
            
            return File(report.File.BinaryFile,
                MimeType.FromFileName(report.File.FileName), 
                report.File.FileName);
        }
    }
}
