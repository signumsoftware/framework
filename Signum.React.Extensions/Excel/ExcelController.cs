using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Signum.Engine.Authorization;
using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Services;
using Signum.Utilities;
using Signum.React.Facades;
using Signum.React.Authorization;
using Signum.Engine.Cache;
using Signum.Engine;
using Signum.Entities.Cache;
using Signum.Utilities.ExpressionTrees;
using Signum.Entities.Processes;
using Signum.Engine.Processes;
using System.Threading;
using Signum.React.ApiControllers;
using Signum.Engine.Basics;
using System.Web;
using Signum.React.Files;
using System.IO;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Excel;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Excel;
using Signum.Entities.Chart;
using Signum.Engine.Chart;
using System.Threading.Tasks;
using Signum.React.Filters;

namespace Signum.React.Excel
{
    [ValidateModelFilter]
    public class MachineLearningController : ControllerBase
    {
        [HttpPost("api/excel/plain")]
        public async Task<FileStreamResult> ToPlainExcel([Required, FromBody]QueryRequestTS request, CancellationToken token)
        {
            var queryRequest = request.ToQueryRequest();

            ResultTable queryResult = await QueryLogic.Queries.ExecuteQueryAsync(queryRequest, token);
            byte[] binaryFile = PlainExcelGenerator.WritePlainExcel(queryResult, QueryUtils.GetNiceName(queryRequest.QueryName));

            var fileName = request.queryKey + TimeZoneManager.Now.ToString("yyyyMMdd-HHmmss") + ".xlsx";

            return FilesController.GetFileStreamResult(new MemoryStream(binaryFile), fileName);            
        }

        [HttpGet("api/excel/reportsFor/{queryKey}")]
        public IEnumerable<Lite<ExcelReportEntity>> GetExcelReports(string queryKey)
        {
            return ExcelLogic.GetExcelReports(QueryLogic.ToQueryName(queryKey));
        }

        [HttpPost("api/excel/excelReport")]
        public FileStreamResult GenerateExcelReport([Required, FromBody]ExcelReportRequest request)
        {
            byte[] file = ExcelLogic.ExecuteExcelReport(request.excelReport, request.queryRequest.ToQueryRequest());

            var fileName = request.excelReport.ToString() + "-" + TimeZoneManager.Now.ToString("yyyyMMdd-HHmmss") + ".xlsx";

            return FilesController.GetFileStreamResult(new MemoryStream(file),  fileName);
        }

        public class ExcelReportRequest
        {
            public QueryRequestTS queryRequest;
            public Lite<ExcelReportEntity> excelReport;
        }
    }
}
