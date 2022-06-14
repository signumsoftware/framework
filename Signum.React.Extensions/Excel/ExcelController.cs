using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using Signum.React.ApiControllers;
using Signum.React.Files;
using System.IO;
using Signum.Engine.Excel;
using Signum.Entities.Excel;
using System.Threading.Tasks;
using Signum.React.Filters;
using Signum.Engine.Authorization;
using Signum.Engine.Json;
using Signum.React.Facades;

namespace Signum.React.Excel;

[ValidateModelFilter]
public class ExcelController : ControllerBase
{
    [HttpPost("api/excel/plain")]
    public async Task<FileStreamResult> ToPlainExcel([Required, FromBody]QueryRequestTS request, CancellationToken token)
    {
        ExcelPermission.PlainExcel.AssertAuthorized();

        var queryRequest = request.ToQueryRequest(SignumServer.JsonSerializerOptions, this.HttpContext.Request.Headers.Referer);

        ResultTable queryResult = await QueryLogic.Queries.ExecuteQueryAsync(queryRequest, token);
        byte[] binaryFile = PlainExcelGenerator.WritePlainExcel(queryResult, QueryUtils.GetNiceName(queryRequest.QueryName));

        var fileName = request.queryKey + Clock.Now.ToString("yyyyMMdd-HHmmss") + ".xlsx";

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
        byte[] file = ExcelLogic.ExecuteExcelReport(request.excelReport, request.queryRequest.ToQueryRequest(SignumServer.JsonSerializerOptions, this.HttpContext.Request.Headers.Referer));

        var fileName = request.excelReport.ToString() + "-" + Clock.Now.ToString("yyyyMMdd-HHmmss") + ".xlsx";

        return FilesController.GetFileStreamResult(new MemoryStream(file),  fileName);
    }

    public class ExcelReportRequest
    {
        public QueryRequestTS queryRequest;
        public Lite<ExcelReportEntity> excelReport;
    }
}
