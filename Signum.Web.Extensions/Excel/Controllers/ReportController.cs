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
using Signum.Entities.Basics;
using Signum.Engine.Basics;
using Signum.Web.Operations;
using Signum.Entities.Excel;
using Signum.Engine.Excel;
#endregion

namespace Signum.Web.Excel
{
    public class ReportController : Controller
    {
        [HttpPost]
        public ActionResult ToExcelPlain(QueryRequest request)
        {
            if (!Finder.IsFindable(request.QueryName))
                throw new UnauthorizedAccessException(NormalControlMessage.ViewForType0IsNotAllowed.NiceToString().Formato(request.QueryName));

            ResultTable queryResult = DynamicQueryManager.Current.ExecuteQuery(request);
            byte[] binaryFile = PlainExcelGenerator.WritePlainExcel(queryResult);

            return File(binaryFile, MimeType.FromExtension(".xlsx"), Finder.ResolveWebQueryName(request.QueryName) + ".xlsx");
        }

        [HttpPost]
        public ActionResult ExcelReport(QueryRequest request, Lite<ExcelReportDN> excelReport)
        {
            if (!Finder.IsFindable(request.QueryName))
                throw new UnauthorizedAccessException(NormalControlMessage.ViewForType0IsNotAllowed.NiceToString().Formato(request.QueryName));

            byte[] file = ExcelLogic.ExecuteExcelReport(excelReport, request);

            return File(file, MimeType.FromExtension(".xlsx"), Finder.ResolveWebQueryName(request.QueryName) + "-" + TimeZoneManager.Now.ToString("yyyyMMdd-HHmmss") + ".xlsx");
            //Known Bug in IE: When the file dialog is shown, if Open is chosen the Excel will be broken as a result of IE automatically adding [1] to the name. 
            //There's not workaround for this, so either click on Save instead of Open, or use Firefox or Chrome
        }

        [HttpPost]
        public ActionResult Create(Lite<QueryDN> query, string prefix)
        {
            ExcelReportDN report = new ExcelReportDN { Query = query.Retrieve() };

            return this.DefaultConstructResult(report, prefix);
        }
    }
}
