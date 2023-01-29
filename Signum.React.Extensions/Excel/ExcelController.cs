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
    public async Task<FileStreamResult> ToPlainExcel([Required, FromBody]QueryRequestTS request, [FromQuery]bool forImport, CancellationToken token)
    {
        ExcelPermission.PlainExcel.AssertAuthorized();

        var queryRequest = request.ToQueryRequest(SignumServer.JsonSerializerOptions, this.HttpContext.Request.Headers.Referer);

        ResultTable queryResult = await QueryLogic.Queries.ExecuteQueryAsync(queryRequest, token);
        byte[] binaryFile = PlainExcelGenerator.WritePlainExcel(queryResult, QueryUtils.GetNiceName(queryRequest.QueryName), forImport: forImport);

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
    public QueryTokenTS? ValidateForImport([Required, FromBody] QueryRequestTS queryRequest)
    {
        ExcelPermission.ImportFromExcel.AssertAuthorized();

        var result = ImporterFromExcel.ParseQueryRequest(queryRequest.ToQueryRequest(SignumServer.JsonSerializerOptions, this.HttpContext.Request.Headers.Referer));

        return result.ElementTopToken == null ? null : new QueryTokenTS(result.ElementTopToken, true);
    }

    [HttpPost("api/excel/import")]
    public IAsyncEnumerable<ImportResult> ImportFromExcel([Required, FromBody] ImportFromExcelRequest request)
    {
        ExcelPermission.ImportFromExcel.AssertAuthorized();

        var qr = request.QueryRequest.ToQueryRequest(SignumServer.JsonSerializerOptions, this.HttpContext.Request.Headers.Referer);

        Type mainType = TypeLogic.GetType(request.ImportModel.TypeName);

        return ImporterFromExcel.ImportExcel(qr, request.ImportModel,  request.GetOperationSymbol(mainType));
    }
}

public class ImportFromExcelRequest
{
    public ImportExcelModel ImportModel { get; set; }
    public QueryRequestTS QueryRequest { get; set; }

    public OperationSymbol GetOperationSymbol(Type entityType) => ParseOperationAssert(this.ImportModel.OperationKey, entityType);

    public static OperationSymbol ParseOperationAssert(string operationKey, Type entityType)
    {
        var symbol = SymbolLogic<OperationSymbol>.ToSymbol(operationKey);

        OperationLogic.AssertOperationAllowed(symbol, entityType, inUserInterface: true);

        return symbol;
    }
}
