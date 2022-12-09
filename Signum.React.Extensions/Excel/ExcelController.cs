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

    [HttpPost("api/excel/validateForImport")]
    public void ImportFromExcel(QueryRequestTS queryRequest)
    {
        ExcelPermission.ImportFromExcel.AssertAuthorized();

        ImporterFromExcel.ParseQueryRequest(queryRequest.ToQueryRequest(SignumServer.JsonSerializerOptions, this.HttpContext.Request.Headers.Referer));
    }


    [HttpPost("api/excel/import")]
    public IAsyncEnumerable<ImportResult> ImportFromExcel(ExcelImportRequest request)
    {
        ExcelPermission.ImportFromExcel.AssertAuthorized();

        var queryRequest = request.QueryRequests.ToQueryRequest(SignumServer.JsonSerializerOptions, this.HttpContext.Request.Headers.Referer);

        var qd = QueryLogic.Queries.QueryDescription(queryRequest.QueryName);

        Type mainType = ImporterFromExcel.GetEntityType(qd);

        return ImporterFromExcel.ImportExcel(queryRequest, request.ImportModel.ExcelFile.ToFileContent(), request.GetOperationSymbol(mainType));
    }
}

public class ExcelImportRequest
{
    public ExcelImportModel ImportModel { get; set; }
    public QueryRequestTS QueryRequests { get; set; }
    public required string OperationKey { get; set; }

    public OperationSymbol GetOperationSymbol(Type entityType) => ParseOperationAssert(this.OperationKey, entityType);

    public static OperationSymbol ParseOperationAssert(string operationKey, Type entityType)
    {
        var symbol = SymbolLogic<OperationSymbol>.ToSymbol(operationKey);

        OperationLogic.AssertOperationAllowed(symbol, entityType, inUserInterface: true);

        return symbol;
    }
}
